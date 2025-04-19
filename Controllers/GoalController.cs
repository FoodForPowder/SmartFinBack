using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Goal;
using SmartFin.Entities;
using SmartFin.Services;

namespace SmartFin.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class GoalController : ControllerBase
    {

        private readonly GoalService _goalService;

        public GoalController(GoalService goalService)
        {
            _goalService = goalService;

        }

        [HttpGet("{goalId}")]
        public async Task<ActionResult<Goal>> GetUsersGoalById(int goalId)
        {
            var userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Необходимо войти в систему.");
            }

            var goal = await _goalService.GetGoalByIdAsync(goalId);
            if (goal == null)
            {
                return NotFound("Цель не найдена");
            }

            if (!goal.Users.Any(u => u.Id == int.Parse(userId)))
            {
                return Unauthorized("У вас нет доступа к этой цели");
            }

            return Ok(goal.AsDto());
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Goal>>> GetUsersGoals()
        {
            var userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Необходимо войти в систему.");
            }
            return Ok(await _goalService.GetUserGoalsAsync(int.Parse(userId)));

        }
        [HttpPost]
        public async Task<ActionResult> CreateUserGoal([FromBody] CreateGoalDto createGoalDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            }
            var (isSuccess, message) = await _goalService.CreateGoalAsync(createGoalDto);

            if (isSuccess)
            {
                return Ok(message);
            }
            else
            {
                return BadRequest(message);
            }
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUserGoal(int id, [FromBody] UpdateGoalDto updateGoal)
        {
            var userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Необходимо войти в систему");
            }

            var goalToUpdate = await _goalService.GetGoalByIdAsync(id);
            if (goalToUpdate == null)
            {
                return NotFound("Цель не найдена");
            }

            if (!goalToUpdate.Users.Any(u => u.Id == int.Parse(userId)))
            {
                return Unauthorized("У вас нет доступа к этой цели");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));

            }
            goalToUpdate.dateOfStart = updateGoal.dateOfStart.ToUniversalTime();
            goalToUpdate.dateOfEnd = updateGoal.dateOfEnd.ToUniversalTime();
            goalToUpdate.payment = updateGoal.payment;
            goalToUpdate.name = updateGoal.name;
            goalToUpdate.description = updateGoal.description;
            goalToUpdate.plannedSum = updateGoal.plannedSum;
            goalToUpdate.currentSum = updateGoal.currentSum;
            goalToUpdate.status = updateGoal.status;

            var res = await _goalService.UpdateGoalAsync(goalToUpdate);
            if (res)
            {
                return Ok("Success");
            }
            else
            {
                return BadRequest();
            }

        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGoal(int id)
        {
            var userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Необходимо войти в систему");
            }

            var result = await _goalService.DeleteGoalAsync(id, int.Parse(userId));
            if (result)
            {
                return Ok("Цель успешно удалена");
            }

            return BadRequest("Не удалось удалить цель");
        }
        [HttpPost("recalculate")]
        public async Task<ActionResult> RecalculateGoal([FromBody] GoalDto goalDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            }

            var (isAchievable, recalculatedGoal) = await _goalService.RecalculateGoal(goalDto);

            if (isAchievable)
            {
                return Ok(new { message = "Цель успешно пересчитана", goal = recalculatedGoal });
            }
            else
            {
                return BadRequest("Невозможно пересчитать цель. Даже после перерасчета сумма ежемесячных отчислений превышает 20% дохода пользователя.");
            }
        }
        [HttpPut("{id}/contribute")]
        public async Task<ActionResult> ContributeToGoal(int id, [FromQuery] decimal amount)
        {
            var userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Необходимо войти в систему");
            }

            if (amount <= 0)
            {
                return BadRequest("Сумма должна быть больше нуля");
            }

            var goal = await _goalService.GetGoalByIdAsync(id);
            if (goal == null)
            {
                return NotFound("Цель не найдена");
            }

            if (!goal.Users.Any(u => u.Id == int.Parse(userId)))
            {
                return Unauthorized("У вас нет доступа к этой цели");
            }

            try
            {
                await _goalService.ContributeToGoalAsync(id, amount, int.Parse(userId));
                return Ok("Платеж успешно выполнен");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{goalId}/invite")]
        public async Task<ActionResult<string>> GenerateInviteLink(int goalId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            try
            {
                // Проверяем, существует ли цель и является ли пользователь участником
                var goal = await _goalService.GetGoalByIdAsync(goalId);
                if (goal == null)
                    return NotFound("Цель не найдена");

                if (!goal.Users.Any(u => u.Id == userId))
                    return Unauthorized("Только участники цели могут создавать приглашения");

                // Генерируем зашифрованный токен
                var token = _goalService.GenerateSecureInviteLink(goalId, userId);

                // Формируем полную ссылку
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var inviteUrl = $"/join?token={token}";

                return Ok(new { inviteUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Присоединение к цели по ссылке-приглашению
        [HttpPost("join")]
        public async Task<ActionResult> JoinGoal([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Не указан токен приглашения");

            var userId = int.Parse(User.FindFirstValue("UserId"));

            try
            {
                await _goalService.JoinGoalBySecureLink(token, userId);
                return Ok("Вы успешно присоединились к цели");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}