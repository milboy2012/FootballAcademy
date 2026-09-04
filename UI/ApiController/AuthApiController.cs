using Core.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Services.Options;

namespace UI.ApiController
{
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string AccessToken, string RefreshToken);

    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokens;

        public AuthApiController(UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, ITokenService tokens)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokens = tokens;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user is null || !user.IsActive) return Unauthorized();

            var check = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
            if (check.IsLockedOut) return StatusCode(423, "Аккаунт временно заблокирован");
            if (!check.Succeeded) return Unauthorized();

            return Ok(await _tokens.IssueAsync(user));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(RefreshRequest req)
        {
            var principal = _tokens.GetPrincipalFromExpiredToken(req.AccessToken);
            var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(id);
            if (user is null || !user.IsActive ||
                user.RefreshToken != req.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized();

            return Ok(await _tokens.IssueAsync(user)); // ротация refresh-токена
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me() => Ok(new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.FindFirstValue("fullName"),
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        });

    }
}
