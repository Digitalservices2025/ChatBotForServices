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

    public static async Task<string> AskChatGPT(string userMessage)
    {
        var apiKey = GetEnvVar("AZURE_OPENAI_KEY");
        var endpoint = GetEnvVar("AZURE_OPENAI_ENDPOINT");
        var deployment = GetEnvVar("AZURE_OPENAI_DEPLOYMENT");

        var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        // Add user's message to history
        chatHistory.Add(new ChatMessage(ChatRole.User, userMessage));

        var chatOptions = new ChatCompletionsOptions
        {
            DeploymentName = deployment
        };

        // Include the full conversation history
        foreach (var message in chatHistory)
        {
            chatOptions.Messages.Add(message);
        }

        var response = await client.GetChatCompletionsAsync(chatOptions);

        var reply = response.Value.Choices[0].Message.Content.Trim();

        // Add assistant's reply to history
        chatHistory.Add(new ChatMessage(ChatRole.Assistant, reply));

        return reply;
    }

    private static string GetEnvVar(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Environment variable '{key}' is missing.");
        return value;
    }

    // Optional: Method to clear chat history if needed
    public static void ResetChatHistory()
    {
        chatHistory.Clear();
        chatHistory.Add(new ChatMessage(ChatRole.System, "You are a helpful assistant."));
    }
}
