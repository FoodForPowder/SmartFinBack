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
    [Authorize]
    public class GoalController : ControllerBase
    {

        private readonly GoalService _goalService;

        public GoalController(GoalService goalService)
        {
            _goalService = goalService;

        }

        [HttpGet("{goalId}")]
        public async Task<ActionResult<Goal>> GetUserGoalById(int goalId, [FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this goal's information. ");
            }
            var goal = await _goalService.GetGoalByIdAsync(goalId, int.Parse(userId));
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
        public async Task<ActionResult<IEnumerable<Goal>>> GetUserGoals([FromQuery] string userId)
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
            try
            {
                await _goalService.CreateGoalAsync(createGoalDto);
                return Ok("Succes");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUserGoal(int id, [FromQuery] string userId, [FromBody] UpdateGoalDto updateGoalDto)
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
            var res = await _goalService.UpdateGoalAsync(id, int.Parse(userId), updateGoalDto);
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

    }
}