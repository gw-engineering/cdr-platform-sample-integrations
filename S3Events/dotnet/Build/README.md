# Instructions for Lambda Zip Upload

* Navigate to the code tab of Lambda function
* Click "Upload from"
* Choose ".zip file"
* Upload the zip file.
* Ensure in the runtime settings the Handler is set to **CdrSampleLambda::CdrSampleLambda.Function::FunctionHandler**
* If not - edit the runtime settings and correct the handler.

The function has been written to use the CDR Platform configured with the AuthenticationScheme set to Basic and therefore requires 3 environment variables to function:

* CDR_URL
* CDR_USERNAME
* CDR_PASSWORD

This allows the function to be able to operate against a specific instance of the CDR Platform RESTApi with specified credentials.

These environment variables can either be configured in Visual Studio via the AWS Toolkit's configuration tab in the Function View window, or can be set via the AWS Console.
For more infomation consult the [AWS Documentation](https://docs.aws.amazon.com/lambda/latest/dg/configuration-envvars.html).

To configure your instance of CDR Platform to use basic authentication please consult the [Glasswall Installation Documentation](https://docs.glasswall.com/docs/cdr-platform-deployment-overview)