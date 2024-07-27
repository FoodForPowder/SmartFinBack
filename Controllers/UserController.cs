using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFin.DTOs;
using SmartFin.DTOs.User;
using SmartFin.Entities;
using SmartFin.Models;

namespace SmartFin.Controllers.UserController
{


    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {



        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserController(ILogger<UserController> logger, UserManager<User> userManager, SignInManager<User> signInManager)
        {

            _userManager = userManager;
            _signInManager = signInManager;
        }
        [HttpGet]
        public async   Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return Ok(await _userManager.Users.ToListAsync());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(string id, [FromBody] UpdateUserDto user)
        {
            var curUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (curUserId != id)
            {
                return Unauthorized("You are not authorized to update this user's information. ");
            }
            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                return NotFound();
            }
            userToUpdate.PhoneNumber = user.PhoneNumber;
            userToUpdate.Email = user.Email;
            userToUpdate.Name = user.Name;

            var res = await _userManager.UpdateAsync(userToUpdate);
            if (res.Succeeded)
            {
                return Ok("User updated successfully");
            }
            else
            {
                return StatusCode(500, res.Errors.Select(e => new { msg = e.Code, desc = e.Description }).ToList());
            }

        }
        [HttpPost("changepass")]
        public async Task<IActionResult> ChangePassword(ChangePassRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.userId);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.oldPassword, request.newPassword);
            if (!result.Succeeded)
            {
                return StatusCode(500, result.Errors.Select(e => new { msg = e.Code, desc = e.Description }).ToList());
            }
            await _signInManager.RefreshSignInAsync(user);
            return Ok("Password changed successfully");
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            await _userManager.DeleteAsync(user);
            return NoContent();
        }
    }
}