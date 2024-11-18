using Microsoft.EntityFrameworkCore;
using SmartFin.DbContexts;
using SmartFin.Entities;

namespace SmartFin.Services
{
    public class NotificationService(SmartFinDbContext context)
    {
        private readonly SmartFinDbContext _context = context;



        public async Task CreateNotificationAsync(int goalId, string message)
        {
            var notification = new Notification
            {
                goalId = goalId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Goals
         .Where(g => g.UserId == userId)
         .SelectMany(g => g.notifications)
         .ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}