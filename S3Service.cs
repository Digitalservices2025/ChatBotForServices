using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;  // Ensure this is installed and used

public static class S3Service
{
    private static readonly string bucketName = "s3.chatbot.backup.com";
    private static readonly RegionEndpoint bucketRegion = RegionEndpoint.GetBySystemName("ap-southeast-1");

    private static readonly IAmazonS3 s3Client;

    static S3Service()
    {
        // Load environment variables
        Env.Load();

        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("AWS credentials are missing!");
        }

        s3Client = new AmazonS3Client(accessKey, secretKey, bucketRegion);
    }

    public static async Task<string> SearchFileAsync(string keyword)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            var response = await s3Client.ListObjectsV2Async(request);

            if (response.S3Objects == null || !response.S3Objects.Any())
                return $"❌ No files found in the bucket.";

            var match = response.S3Objects.FirstOrDefault(o => o.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                return $"❌ No matching file found in S3 for '{keyword}'.";

            var urlRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = match.Key,
                Expires = DateTime.UtcNow.AddMinutes(15)
            };

            var url = s3Client.GetPreSignedURL(urlRequest);

            return $"✅ Found: {match.Key}\n🔗 Download Link: {url}";
        }
        catch (AmazonS3Exception ex)
        {
            return $"❌ AWS S3 Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ Unexpected Error: {ex.Message}";
        }
    }
}
