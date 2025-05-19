using System.Net.Http.Headers;
using System.Text.Json;

namespace ChatBotForServices.Services;

public static class AzureSearchService
{
    private static readonly string SearchEndpoint = OpenAIService.GetEnvVar("AZURE_SEARCH_ENDPOINT");
    private static readonly string ApiKey = OpenAIService.GetEnvVar("AZURE_SEARCH_API_KEY");
    private static readonly string IndexName = OpenAIService.GetEnvVar("AZURE_SEARCH_INDEX");

    public static async Task<List<string>> GetTopChunksAsync(string query)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("api-key", ApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            search = query,
            top = 3,
            select = "merged_content"
        };

        var response = await httpClient.PostAsync(
            $"{SearchEndpoint}/indexes/{IndexName}/docs/search?api-version=2023-07-01-Preview",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();
        var chunks = new List<string>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("value", out JsonElement results))
        {
            foreach (var result in results.EnumerateArray())
            {
                if (result.TryGetProperty("content", out var content))
                    chunks.Add(content.GetString());
                else if (result.TryGetProperty("merged_content", out var merged))
                    chunks.Add(merged.GetString());
            }
        }

        return chunks;
    }

}
