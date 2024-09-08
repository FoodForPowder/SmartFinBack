using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Expense;
using SmartFin.Entities;
using SmartFin.Services;

namespace Smartfin.Controllers
{
    [ApiController]
    [Authorize]
    public class ExpenseController(ExpenseService service) : ControllerBase
    {

        private readonly ExpenseService _service = service;

        [HttpGet("{expenseId}")]
        public async Task<ActionResult<ExpenseDto>> GetUserExpenseById(int expenseId, [FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this expenses's information. ");
            }
            var expense = await _service.GetExpenseById(expenseId);
            if (expense == null)
            {
                return NotFound();

            }
            else
            {
                return Ok(expense.asDto());
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetUserGoals([FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this expenses's information. ");
            }
            return Ok((await _service.GetUsersExpenses(int.Parse(userId))).Select(x => x.asDto()));

        }
        [HttpPost]
        public async Task<ActionResult> CreateUserExpense([FromBody] CreateExpenseDto createExpenseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            }
            try
            {
                await _service.CreateUserExpense(createExpenseDto);
                return Ok("Succes");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUserGoal(int id, [FromQuery] string userId, [FromBody] UpdateExpenseDto updateExpenseDto)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to update this expense's information. ");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));

            }
            var res = await _service.UpdateExpense(id, updateExpenseDto);
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
            var res = await _service.DeleteExpense(id);
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