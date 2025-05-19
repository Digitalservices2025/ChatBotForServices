using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using ChatBotForServices.Data;
using ChatBotForServices.Services;
using Tesseract;
using System.Text;


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

// Route: Handle chat messages
app.MapPost("/chat", async (HttpRequest request) =>
{
    var data = await request.ReadFromJsonAsync<ChatRequest>();
    if (data?.Messages == null || !data.Messages.Any())
        return Results.BadRequest(new { reply = "❌ Invalid message format." });

    var userMessage = data.Messages.Last().Content.Trim();

    // Handle S3-based "find file"
    if (userMessage.StartsWith("find file:", StringComparison.OrdinalIgnoreCase))
    {
        var keyword = userMessage["find file:".Length..].Trim();

        var s3Result = await S3Service.SearchFilesAsync(keyword);
        return Results.Json(new { reply = s3Result });
    }

    // First check if it's an FAQ match
    var faqResponse = await GoogleSheetFAQService.TryMatchAsync(userMessage);
    if (!string.IsNullOrEmpty(faqResponse))
    {
        return Results.Json(new { reply = faqResponse });
    }

    // Otherwise, fallback to GPT-4o
    var response = await OpenAIService.CallOpenAIAsync(userMessage);
    return Results.Json(new { reply = response });

});




// app.MapPost("/upload-image", async (HttpRequest req) =>
// {
//     var file = req.Form.Files.FirstOrDefault();
//     if (file == null || file.Length == 0)
//         return Results.BadRequest(new { text = "❌ No image found." });

//     using var ms = new MemoryStream();
//     await file.CopyToAsync(ms);
//     ms.Position = 0;

//     using var img = Pix.LoadFromMemory(ms.ToArray());
//     using var engine = new TesseractEngine(@"./tessdata", "eng+tha", EngineMode.Default);
//     engine.SetVariable("user_defined_dpi", "300");

//     using var page = engine.Process(img);
//     string extractedText = page.GetText();

//     var autoPrompt = $"I found this problem in a file:\n\n{extractedText}\n\nPlease suggest how to fix this.";
//     var fixSuggestion = await OpenAIService.CallOpenAIAsync(autoPrompt);

//     return Results.Json(new { text = $"📸 OCR Result:\n{extractedText}\n\n🛠️ Chatty Suggests:\n{fixSuggestion}" });
// });

// app.MapPost("/upload-read", async (HttpRequest req) =>
// {
//     var file = req.Form.Files.FirstOrDefault();
//     if (file == null || file.Length == 0)
//         return Results.BadRequest(new { summary = "❌ No file found." });

//     using var ms = new MemoryStream();
//     await file.CopyToAsync(ms);
//     var bytes = ms.ToArray();

//     var extracted = FileReaderService.ExtractText(file.FileName, bytes);
//     if (extracted.StartsWith("[❌"))
//         return Results.Json(new { summary = extracted });

//     var prompt = $"Summarize or answer based on this content:\n\n{extracted[..Math.Min(4000, extracted.Length)]}";
//     var aiReply = await OpenAIService.CallOpenAIAsync(prompt);

//     return Results.Json(new { summary = aiReply });
// });


app.Run();

// Record types for deserialization
public record ChatRequest(List<Message> Messages);
public record Message(string Role, string Content);
