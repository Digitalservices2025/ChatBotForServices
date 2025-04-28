using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using ChatBotForServices.Data;
using ChatBotForServices.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddRouting();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Optional: Add Swagger for easy API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files (like index.html, CSS, JS)
app.UseStaticFiles();

// Route: Serve index.html by default
app.MapGet("/", () => Results.Redirect("/index.html"));

/// Route: Handle chat messages
app.MapPost("/chat", async (HttpRequest request) =>
{
    var data = await request.ReadFromJsonAsync<ChatRequest>();
    if (data?.Messages == null || !data.Messages.Any())
        return Results.BadRequest(new { reply = "❌ Invalid message format." });

    var userMessage = data.Messages.Last().Content.Trim();

    // 1️⃣ Check for S3 File Search Command
    if (userMessage.StartsWith("find file:", StringComparison.OrdinalIgnoreCase))
    {
        var keyword = userMessage.Replace("find file:", "").Trim();
        var s3Response = await S3Service.SearchFileAsync(keyword);
        return Results.Json(new { reply = s3Response });
    }

    // 2️⃣ Exact Match from FAQ
    var faqMatch = ChattyFAQ.Data.FirstOrDefault(item =>
        userMessage.Contains(item.Key, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrEmpty(faqMatch.Key))
        return Results.Json(new { reply = faqMatch.Value });

    // 3️⃣ Suggestion Logic
    var suggestions = ChattyFAQ.Data.Keys
        .Where(key => key.Contains(userMessage, StringComparison.OrdinalIgnoreCase)
                   || userMessage.Contains(key, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (suggestions.Any())
    {
        var suggested = suggestions.First();
        return Results.Json(new
        {
            reply = $"คุณหมายถึง \"{suggested}\" หรือไม่?\nคำตอบ: {ChattyFAQ.Data[suggested]}"
        });
    }

    // 4️⃣ Fallback to ChatGPT
    var chatGptResponse = await OpenAIService.AskChatGPT(userMessage);
    return Results.Json(new { reply = chatGptResponse });
});

/// Route: Handle file upload
app.MapPost("/upload", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();

    if (file == null || file.Length == 0)
        return Results.BadRequest(new { message = "No file uploaded." });

    return Results.Json(new
    {
        message = $"✅ Received file: {file.FileName}, size: {file.Length} bytes"
    });
});



app.Run();

// Record types for deserialization
public record ChatRequest(List<Message> Messages);
public record Message(string Role, string Content);
