# Pipeline-ManualIntervention-Slack
CodePipeline Manual Intervention SNS Message Handler to post messages to Slack

Publish the project, zip it up, push to S3, and use the `deploy.yaml` CloudFormation template to deploy.

`dotnet publish -c Release -o ./publish`

`zip -r pipeline-manualintervention-slack.zip ./publish`

`aws s3 cp ./pipeline-manualintervention-slack.zip [S3Uri]`