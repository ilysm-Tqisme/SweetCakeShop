using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Constants;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.Chat;

namespace SweetCakeShop.Controllers
{
    /// <summary>
    /// Chatbot tư vấn sản phẩm (flow video Laravel + Gemini): DB history, ChatToken, catalog từ SQL.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ICustomerProductChatService _customerChat;
        private readonly IAiChatService _adminChat;
        private readonly IConversationMemoryService _adminMemory;

        public ChatController(
            ICustomerProductChatService customerChat,
            IAiChatService adminChat,
            IConversationMemoryService adminMemory)
        {
            _customerChat = customerChat;
            _adminChat = adminChat;
            _adminMemory = adminMemory;
        }

        /// <summary>Lịch sử chat — UserId hoặc Cookie ChatToken.</summary>
        [HttpGet("GetChatHistory")]
        [AllowAnonymous]
        public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(CancellationToken ct) =>
            Ok(await _customerChat.GetChatHistoryAsync(ct));

        /// <summary>Gửi tin nhắn → lưu DB → Gemini/OpenAI + dữ liệu sản phẩm.</summary>
        [HttpPost("SendMessage")]
        [AllowAnonymous]
        public async Task<ActionResult<SendChatMessageResponse>> SendMessage(
            [FromBody] SendChatMessageRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new SendChatMessageResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            return Ok(await _customerChat.SendMessageAsync(request, ct));
        }

        [HttpGet("customer/suggestions")]
        [AllowAnonymous]
        public ActionResult<object> CustomerSuggestions([FromQuery] int? productId)
        {
            var replies = productId.HasValue
                ? new[] { "Giá món này?", "Bánh tương tự?", "Giao hàng?", "Muốn đặt hàng" }
                : new[] { "Bánh sinh nhật gợi ý?", "Bánh rẻ nhất?", "Giao hàng mấy ngày?", "Muốn đặt hàng" };
            return Ok(new { quickReplies = replies });
        }

        [HttpPost("admin")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<ChatApiResponse>> Admin(
            [FromBody] ChatApiRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ChatApiResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            var reply = await _adminChat.GetAdminReplyAsync(request.Message, cancellationToken);
            return Ok(new ChatApiResponse { Success = true, Reply = reply });
        }

        [HttpPost("admin/clear")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public IActionResult ClearAdminSession()
        {
            _adminMemory.Clear(AiChatMode.Admin);
            return Ok(new { success = true });
        }
    }
}
