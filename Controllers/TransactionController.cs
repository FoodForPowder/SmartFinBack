using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartfin.Extensions;
using SmartFin.DTOs.Expense;
using SmartFin.DTOs.Transaction;
using SmartFin.Services;

namespace Smartfin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class TransactionController(TransactionService service) : ControllerBase
    {

        private readonly TransactionService _service = service;

        [HttpGet("{transactionId}")]
        public async Task<ActionResult<TransactionDto>> GetUserTransactionById(int transactionId, [FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this transactions's information. ");
            }
            var transaction = await _service.GetTransactionById(transactionId);
            if (transaction == null)
            {
                return NotFound();

            }
            else
            {
                return Ok(transaction.asDto());
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactions([FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to get this transactions's information. ");
            }
            return Ok((await _service.GetUsersTransactions(int.Parse(userId))).Select(x => x.asDto()));

        }
        [HttpPost]
        public async Task<ActionResult> CreateUserTransaction([FromBody] CreateTransactionDto createTransactionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
            }
            try
            {
                await _service.CreateUserTransaction(createTransactionDto);
                return Ok("Succes");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUserTransaction(int id, [FromQuery] string userId, [FromBody] UpdateTransactionDto updateTransactionDto)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to update this transaction's information. ");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));

            }
            var res = await _service.UpdateTransaction(id, updateTransactionDto);
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
        public async Task<ActionResult> DeleteTransaction(int id, [FromQuery] string userId)
        {
            var curUserId = User.FindFirstValue("UserId");
            if (userId != curUserId)
            {
                return Unauthorized("You are not authorized to delete this goal");
            }
            var res = await _service.DeleteTransaction(id);
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