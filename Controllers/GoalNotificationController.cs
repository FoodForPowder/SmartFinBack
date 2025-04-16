using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFin.Services;

namespace SmartFin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   //
   //  [Authorize(Roles = "Admin")] // Ограничение доступа только для администраторов
    public class GoalNotificationController : ControllerBase
    {
        private readonly NotificationService _goalNotificationService;

        public GoalNotificationController(NotificationService goalNotificationService)
        {
            _goalNotificationService = goalNotificationService;
        }

        /// <summary>
        /// Запускает проверку целей и создание уведомлений вручную (для тестирования)
        /// </summary>
        [HttpPost("check-all")]
        public async Task<IActionResult> CheckAllGoals()
        {
            await _goalNotificationService.CheckAllGoalsAndNotifyUsersAsync();
            return Ok("Проверка целей выполнена успешно");
        }
    }
}