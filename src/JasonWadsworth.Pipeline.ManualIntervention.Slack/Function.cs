using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Formatting.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace JasonWadsworth.Pipeline.ManualIntervention.Slack
{
    public class Function
    {
        private readonly Microsoft.Extensions.Logging.ILogger logger;

        private readonly string webhookUrl;

        private readonly HttpClient httpClient = new HttpClient();

        public Function()
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            System.Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(configuration["Pipeline:ManualIntervention:Slack:WebhookUrl"]));

            webhookUrl = configuration["Pipeline:ManualIntervention:Slack:WebhookUrl"];

            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var loggerConfig = new Serilog.LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "CodePipeline.ManualIntervention.Slack")
                .WriteTo.Console(new JsonFormatter());

            Serilog.Log.Logger = loggerConfig.CreateLogger();
            loggerFactory.AddSerilog();

            logger = serviceProvider.GetRequiredService<ILogger<Function>>();
        }


        public async Task FunctionHandler(Amazon.Lambda.SNSEvents.SNSEvent input, ILambdaContext context)
        {
            foreach (var record in input.Records)
            {
                var manualIntervention = Newtonsoft.Json.JsonConvert.DeserializeObject<ManualInterventionMessage>(record.Sns.Message);

                var message = new
                {
                    Blocks = new object[]
                    {
                    new { Type = "divider" },
                    new { Type = "section", Text = new { Type = "mrkdwn", Text = $"*{record.Sns.Subject}*\n\n<{manualIntervention.Approval.ApprovalReviewLink}|Click to Approve/Reject>" } },
                    new { Type = "section", Fields = new object[]
                    {
                        new { Type = "mrkdwn", Text = $"*Region*\n{manualIntervention.Region}" },
                        new { Type = "mrkdwn", Text = $"*Pipeline Name*\n{manualIntervention.Approval.PipelineName}" },
                        new { Type = "mrkdwn", Text = $"*Stage*\n{manualIntervention.Approval.StageName}" },
                        new { Type = "mrkdwn", Text = $"*Action*\n{manualIntervention.Approval.ActionName}" },
                        new { Type = "mrkdwn", Text = $"*Custom Data*\n{manualIntervention.Approval.CustomData}" },
                        new { Type = "mrkdwn", Text = $"*Time*\n{manualIntervention.Approval.Expires}" },
                    } },
                    }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(message, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                });

                logger.LogInformation(webhookUrl);
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    // TODO: add some retry logic
                    using (var response = await httpClient.SendAsync(requestMessage))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            logger.LogError((int)response.StatusCode, "Failed to send message to Slack. StatusCode was {StatusCode}", response.StatusCode);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(500, e, "Failed to send message to Slack");
                }
            }
        }
    }
}
