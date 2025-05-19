using ChatBotForServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBotForServices.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            var chunks = await AzureSearchService.GetTopChunksAsync(question);
            var answer = await OpenAIService.AskQuestionWithContextAsync(question, chunks);
            return Ok(new { answer });
        }
    }
}
