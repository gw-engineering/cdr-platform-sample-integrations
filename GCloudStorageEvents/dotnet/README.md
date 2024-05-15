# Google Storage Event Notification Cloud Run Service

This starter project responds to events on a Google Cloud Pub/Sub queue containing Google Cloud Storage Event Notifications.

The notifications are used to trigger the following workflow:

* Identify a file in the configured input Google Storage bucket.
* Download the file using the Google Service Account.
* Request the file is processed by the CDR Platform
* Handle 429 status codes with retry logic.
* Store the protected file from the response into a configured "protected" bucket.

The sample has basic exception handling and any exceptions will simply result in the message being completed and no file written to the destination, a production implementation of the function would need to have
appropriate error handling according to your requirements.

It is important to not write the protected file to the source S3 bucket in order to avoid an event loop.

The Cloud Run service has been written to use the Halo API configured with the AuthenticationScheme set to Basic and therefore requires 3 environment variables to function:

* HALO_URL
* HALO_USERNAME
* HALO_PASSWORD

This allows the application to be able to operate against a specific instance of the Halo RESTApi with specified credentials.

These environment variables can be configured at deployment time with the 'gcloud run deploy' command or in the management console.

For more infomation consult the [Google Cloud Documentation](https://cloud.google.com/run/docs/configuring/services/environment-variables).

To configure your instance of CDR Platform to use basic authentication please consult the [Glasswall Installation Documentation](https://docs.glasswall.com/docs/cdr-platform-deployment-overview)

The Cloud Run service is configured to output to a specific bucket with the following environment variable:

* OutputBucket