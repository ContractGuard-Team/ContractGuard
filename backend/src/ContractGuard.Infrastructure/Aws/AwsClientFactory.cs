using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.SQS;

namespace ContractGuard.Infrastructure.Aws;

// Builds AWS SDK clients. On AWS the SDK finds credentials itself (the ECS task role);
// against LocalStack any non-empty key works, so a fixed dummy pair is used.
internal static class AwsClientFactory
{
    private static readonly AWSCredentials LocalCredentials = new BasicAWSCredentials("test", "test");

    public static IAmazonS3 CreateS3(AwsOptions options, string? serviceUrl)
    {
        var config = new AmazonS3Config();
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            return new AmazonS3Client(config);
        }

        config.ServiceURL = serviceUrl;
        config.AuthenticationRegion = options.Region;

        // LocalStack serves buckets as http://host:4566/bucket/key (path style)
        // instead of http://bucket.host/key (virtual-hosted style, the AWS default).
        config.ForcePathStyle = true;
        return new AmazonS3Client(LocalCredentials, config);
    }

    // Always the real regional endpoint: local runs send email through Mailpit, not LocalStack.
    public static IAmazonSimpleEmailServiceV2 CreateSes(AwsOptions options) =>
        new AmazonSimpleEmailServiceV2Client(new AmazonSimpleEmailServiceV2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        });

    // Always the real regional endpoint: it reads the AI team's table in their account (docs/04 C8).
    public static IAmazonDynamoDB CreateDynamoDb(AwsOptions options) =>
        new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        });

    public static IAmazonSQS CreateSqs(AwsOptions options)
    {
        var config = new AmazonSQSConfig();
        if (!options.UsesLocalEndpoint)
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            return new AmazonSQSClient(config);
        }

        config.ServiceURL = options.ServiceUrl;
        config.AuthenticationRegion = options.Region;
        return new AmazonSQSClient(LocalCredentials, config);
    }
}
