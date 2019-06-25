using System;

namespace JasonWadsworth.Pipeline.ManualIntervention.Slack
{
    public class ManualInterventionMessage
    {
        public string Region { get; set; }

        public string ConsoleLink { get; set; }

        public Approval Approval { get; set; }
    }

    public class Approval
    {
        public string PipelineName { get; set; }

        public string StageName { get; set; }

        public string ActionName { get; set; }

        public DateTime Expires { get; set; }

        public string ApprovalReviewLink { get; set; }

        public string CustomData { get; set; }
    }
}