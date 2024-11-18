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
        public async Task<ActionResult<Goal>> GetUsersGoalById(int goalId, string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this goal's information. ");
            }
            var goal = await _goalService.GetGoalByIdAsync(goalId);
            if (goal == null)
            {
                return NotFound();

            }
            else
            {
                return Ok(goal.AsDto());
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Goal>>> GetUsersGoals([FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this goal's information. ");
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
        public async Task<ActionResult> UpdateUserGoal(int id, [FromQuery] string userId, [FromBody] UpdateGoalDto updateGoal)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to update this goal's information. ");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));

            }
            var goalToUpdate = await _goalService.GetGoalByIdAsync(id);
            if (goalToUpdate == null)
            {
                return NotFound("");
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
        public async Task<ActionResult> DeleteGoal(int id, [FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to delete this goal");
            }
            var res = await _goalService.DeleteGoalAsync(id, int.Parse(userId));
            if (res)
            {
                return Ok("Success");
            }
            else
            {
                return BadRequest();
            }
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
        public async Task<ActionResult> ContributeToGoal(int id, [FromQuery] string userId, [FromQuery] decimal amount)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to contribute this goal");
            }
            if (amount <= 0)
            {
                return BadRequest("Сумма должна быть больше нуля");
            }
            var goal = await _goalService.GetGoalByIdAsync(id);
            if (goal == null)
            {
                return NotFound();
            }
            try
            {
                await _goalService.ContributeToGoalAsync(id, amount);
                return Ok("Seccuess");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException.Message);
            }
        }


    }
}