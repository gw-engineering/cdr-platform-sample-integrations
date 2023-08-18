# S3 Event Notification Lambda

This starter project responds to events on an Amazon SQS queue containing S3 Event Notifications.

The project targets the .NET6 runtime - which is currently the latest runtime supported by Lambda functions.

The notifications are used to trigger the following workflow:

* Identify a file in an S3 bucket.
* Download the file using the Lambda execution roles credentials.
* Request the file is processed by the CDR Platform
* Handle 429 status codes with retry logic.
* Store the protected file from the response into a "protected" bucket based on the original bucket name (creating the bucket if it doesn't exist).

The sample has basic exception handling and any exceptions will simply result in the message being completed and no file written to the destination, a production implementation of the function would need to have
appropriate error handling according to your requirements.

It is important to not write the protected file to the source S3 bucket in order to avoid an event loop - the sample code will create a new bucket if one doesn't exist
following the convention of {original-bucket-name}-protected.

The function has been written to use the CDR Platform configured with the AuthenticationScheme set to Basic and therefore requires 2 environment variables to function:

* CDR_USERNAME
* CDR_PASSWORD

These environment variables can either be configured in Visual Studio via the AWS Toolkit's configuration tab in the Function View window, or can be set via the AWS Console.
For more infomation consult the [AWS Documentation](https://docs.aws.amazon.com/lambda/latest/dg/configuration-envvars.html).

To configure your instance of CDR Platform to use basic authentication please consult the [Glasswall Installation Documentation](https://docs.glasswall.com/docs/cdr-platform-deployment-overview)

After deploying your function you must configure S3 to publish events to an Amazon SQS queue and configure the queue as an event source to trigger your Lambda function.
For a guide on how to achieve this please consult these [instructions](www.glasswall.com)

The easiest way to publish the function is via the [AWS Toolkit for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2022)

## Here are some steps to follow from Visual Studio to publish this code to a Lambda function:

Install the "AWS Toolkit for Visual Studio" extension.

Ensure you have configured your machine to have an [AWS Profile ](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/basic-use.html)

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window - this can be used to set the two environment variables.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.
