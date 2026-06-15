using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Services;
using System.Security.Claims;

namespace SweetCakeShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            string? userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            string? sessionId = HttpContext.Session.Id;

            var notifications = await _notificationService.GetUnreadAsync(userId, userId == null ? sessionId : null);
            return Ok(notifications);
        }

        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            string? userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            string? sessionId = HttpContext.Session.Id;

            await _notificationService.MarkAllReadAsync(userId, userId == null ? sessionId : null);
            return Ok(new { success = true });
        }
    }
}
