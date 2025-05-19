using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
namespace ChatBotForServices.Services;

public static class OpenAIService
{
    static OpenAIService() => Env.Load();

    private static readonly List<ChatMessage> chatHistory = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.System, "You are a helpful assistant.")
    };

    public static async Task<string> CallOpenAIAsync(string prompt)
    {
        var apiKey = GetEnvVar("AZURE_OPENAI_KEY");
        var endpoint = GetEnvVar("AZURE_OPENAI_ENDPOINT");
        var deployment = GetEnvVar("AZURE_OPENAI_DEPLOYMENT");

        var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var chatOptions = new ChatCompletionsOptions
        {
            DeploymentName = deployment,
            Messages =
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant. Answer using only the provided context."),
            new ChatMessage(ChatRole.User, prompt)
        }
        };

        var response = await client.GetChatCompletionsAsync(chatOptions);
        return response.Value.Choices[0].Message.Content.Trim();
    }


    public static async Task<string> AskQuestionWithContextAsync(string question, List<string> contextChunks)
    {
        string prompt = $"Answer this question using the context below:\n\nContext:\n";
        foreach (var chunk in contextChunks)
            prompt += $"- {chunk}\n";
        prompt += $"\nQuestion: {question}";

        return await CallOpenAIAsync(prompt);
    }


    public static string GetEnvVar(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Environment variable '{key}' is missing.");
        return value;
    }

    public static void ResetChatHistory()
    {
        chatHistory.Clear();
        chatHistory.Add(new ChatMessage(ChatRole.System, "You are a helpful assistant."));
    }
}
