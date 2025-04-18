using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartFin.Models.AouthResponse;
using System.Globalization;
using SmartFin.Models.LoginRequest;
using SmartFin.Models.RegisterRequest;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using SmartFin.Models.RevokeRequest;
using Microsoft.IdentityModel.Tokens;
using SmartFin.Models.RefreshRequest;
using SmartFin.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartFin.DTOs.User;
using Smartfin.Extensions;




namespace SmartFin.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {


        public IConfiguration _configuration;
        private UserManager<User> _userManager;
        public AuthController(IConfiguration config, UserManager<User> userManager)
        {
            _configuration = config;
            _userManager = userManager;
        }
        [HttpPost("login")]

        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessages();
            }
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            var isAuthorized = user != null && await _userManager.CheckPasswordAsync(user, loginRequest.Password);

            if (isAuthorized)
            {
                var AuthResponse = await GetTokens(user);
                AuthResponse.userId = user.Id; // Теперь это int
                user.RefreshToken = AuthResponse.RefreshToken;
                await _userManager.UpdateAsync(user);

                return Ok(AuthResponse);
            }
            else
            {
                return Unauthorized("Invalid credentials");
            }
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest refreshRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessages();
            }
            var principal = GetPrincipalFromExpiredToken(refreshRequest.AccessToken);
            var userEmail = principal.FindFirstValue("Email");

            var user = !string.IsNullOrEmpty(userEmail) ? await _userManager.FindByEmailAsync(userEmail) : null;

            if (user == null || user.RefreshToken != refreshRequest.RefreshToken)
            {
                return BadRequest(" Invalid refresh token");
            }
            var response = await GetTokens(user);
            user.RefreshToken = response.RefreshToken;
            await _userManager.UpdateAsync(user);
            return Ok(response);
        }

        private async Task<AuthResponse> GetTokens(User user)
        {
            var claims = new[]{
                        new Claim(JwtRegisteredClaimNames.Sub, _configuration["token:subject"]),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                        new Claim("UserId",user.Id.ToString()),
                        new Claim("UserName", user.Name),
                        new Claim("Email", user.Email)
                    };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["token:key"]));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["token:issuer"],
                _configuration["token:audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["token:accessTokenExpiryMinutes"])),
                signingCredentials: signIn
            );

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            var RefreshToken = GetRefreshToken();
            user.RefreshToken = RefreshToken;


            var AuthResponse = new AuthResponse { AccessToken = tokenStr, RefreshToken = RefreshToken };
            return await Task.FromResult(AuthResponse);
        }

        private string GetRefreshToken()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var tokenIsUniq = !_userManager.Users.Any(u => u.RefreshToken == token);

            if (!tokenIsUniq)
            {
                return GetRefreshToken();
            }
            return token;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessages();
            }

            var isEmailAlreadyRegistered = await _userManager.FindByEmailAsync(registerRequest.EmailAddress) != null;
            var PhoneNumberAlreadyRegistered = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == registerRequest.PhoneNumber) != null;

            if (isEmailAlreadyRegistered)
            {
                return Conflict($"Email {registerRequest.EmailAddress}  was already registered.");
            }
            if (PhoneNumberAlreadyRegistered)
            {
                return Conflict($"Phone number {registerRequest.PhoneNumber} is already registered");
            }
            var newUser = new User
            {
                UserName = registerRequest.UserName,
                Email = registerRequest.EmailAddress,
                Name = registerRequest.Name,
                PhoneNumber = registerRequest.PhoneNumber,

            };
            var result = await _userManager.CreateAsync(newUser, registerRequest.Password);

            if (result.Succeeded)
            {

                return Ok("Succes!");
            }
            else
            {
                return StatusCode(500, result.Errors.Select(e => new { msg = e.Code, desc = e.Description }).ToList());
            }

        }


        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var TokenValidationParameters = new TokenValidationParameters
            {

                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["token:key"])),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, TokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }



        private IActionResult BadRequestErrorMessages()

        {
            var errMsgs = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(errMsgs);
        }
        [Authorize]
        [HttpGet("tokenValidate")]
        public async Task<IActionResult> TokenValidate()
        {

            return Ok("Token is valid");
        }
        [Authorize]
        [HttpPut("revoke")]

        public async Task<IActionResult> Revoke(RevokeRequest revokeRequest)
        {

            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessages();
            }


            var userEmail = this.HttpContext.User.FindFirstValue("Email");


            var user = !string.IsNullOrEmpty(userEmail) ? await _userManager.FindByEmailAsync(userEmail) : null;
            if (user == null || user.RefreshToken != revokeRequest.RefreshToken)
            {
                return BadRequest("Invalid refresh token");
            }


            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);
            return Ok("Refresh token is revoked");


        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();
            return Ok(user.asDto());
        }
    }
}