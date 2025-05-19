using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using System.Text;

public static class S3Service
{
    private static readonly string bucketName = "s3.chatbot.backup.com";
    private static readonly RegionEndpoint bucketRegion = RegionEndpoint.GetBySystemName("ap-southeast-1");
    private static readonly IAmazonS3 s3Client;

    static S3Service()
    {
        Env.Load();

        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("AWS credentials are missing!");
        }

        s3Client = new AmazonS3Client(accessKey, secretKey, bucketRegion);
    }

    public static async Task<string> SearchFilesAsync(string keyword)
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

            // Find all matches
            var matches = response.S3Objects
                .Where(o => o.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!matches.Any())
                return $"❌ No matching files found in S3 for '{keyword}'.";

            var sb = new StringBuilder();
            sb.AppendLine("<div style='font-family:Segoe UI, sans-serif;'>");
            sb.AppendLine($"✅ Found {matches.Count} matching file(s):<br/><br/>");

            foreach (var match in matches)
            {
                var urlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = match.Key,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                };

                var url = s3Client.GetPreSignedURL(urlRequest);
                var fileName = Path.GetFileName(match.Key);

                sb.AppendLine($@"
<div style='margin-bottom: 14px; border: 1px solid #444; padding: 12px; border-radius: 8px; background-color: #2d2d44;'>
  📄 <strong style='color:#fff;'>{fileName}</strong><br/>
  <a href='{url}' target='_blank' style='color:#4ea1ff;text-decoration:underline;'>📥 Download</a>
</div>");

            }

            sb.AppendLine("</div>");
            return sb.ToString();

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
