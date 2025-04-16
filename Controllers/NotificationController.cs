using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartFin.Entities;
using SmartFin.Services;

namespace SmartFin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Получает список уведомлений пользователя
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(int userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId.ToString() != curUserId)
            {
                return Unauthorized("Вы не авторизованы для получения этих уведомлений");
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);      }

        /// <summary>
        /// Получает список непрочитанных уведомлений пользователя
        /// </summary>
        [HttpGet("user/{userId}/unread")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadUserNotifications(int userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId.ToString() != curUserId)
            {
                return Unauthorized("Вы не авторизованы для получения этих уведомлений");
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            return Ok(unreadNotifications);
        }
    }
}