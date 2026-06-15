using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Event-driven notification service for in-app toasts and notification history.
    /// </summary>
    public interface INotificationService
    {
        Task CreateAsync(string? userId, string? sessionId, string eventType, string title, string message, string? data = null);
        Task<List<Notification>> GetUnreadAsync(string? userId, string? sessionId, int max = 20);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllReadAsync(string? userId, string? sessionId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string? userId, string? sessionId, string eventType, string title, string message, string? data = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                SessionId = sessionId,
                EventType = eventType,
                Title = title,
                Message = message,
                Data = data,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadAsync(string? userId, string? sessionId, int max = 20)
        {
            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => !n.IsRead);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(n => n.SessionId == sessionId);
            }
            else
            {
                return new List<Notification>();
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(max)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadAsync(string? userId, string? sessionId)
        {
            var query = _context.Notifications.Where(n => !n.IsRead);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(n => n.SessionId == sessionId);
            }
            else
            {
                return;
            }

            var notifications = await query.ToListAsync();
            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}
