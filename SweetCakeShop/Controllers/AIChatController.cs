using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Constants;
using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    public class AIChatController : Controller
    {
        private readonly IAiChatService _aiChatService;

        public AIChatController(IAiChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminClearSession([FromServices] SweetCakeShop.Services.AI.IConversationMemoryService memory)
        {
            memory.Clear(AiChatMode.Admin);
            return Json(new { success = true });
        }

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminSend([FromForm] AIChatRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Json(new AIChatResponse { Success = false, Reply = "Vui lòng nhập câu hỏi hợp lệ." });
            }

            var reply = await _aiChatService.GetAdminReplyAsync(request.Message, cancellationToken);
            return Json(new AIChatResponse { Success = true, Reply = reply });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerClearSession([FromServices] SweetCakeShop.Services.AI.IConversationMemoryService memory)
        {
            memory.Clear(AiChatMode.Customer);
            return Json(new { success = true });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerSend([FromForm] AIChatRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Json(new AIChatResponse { Success = false, Reply = "Vui lòng nhập câu hỏi hợp lệ." });
            }

            var result = await _aiChatService.GetCustomerReplyAsync(request.Message, cancellationToken: cancellationToken);
            return Json(new AIChatResponse { Success = true, Reply = result.Reply });
        }
    }
}
