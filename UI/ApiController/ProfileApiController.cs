using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Profile;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController, Route("api/profile"), Authorize, IgnoreAntiforgeryToken]
    public class ProfileApiController : ControllerBase
    {
        private readonly IProfileService _svc; 
        private readonly SignInManager<AppUser> _signIn; 
        private readonly UserManager<AppUser> _users;
        public ProfileApiController(IProfileService svc, SignInManager<AppUser> signIn, UserManager<AppUser> users) { 
            _svc = svc; 
            _signIn = signIn; 
            _users = users; 
        }
        private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => await _svc.GetAsync(Me, ct) is { } p ? Ok(p) : NotFound();

        [HttpPut] public async Task<IActionResult> Update(UpdateProfileRequest req, CancellationToken ct) => R(await _svc.UpdateAsync(Me, req, ct));

        [HttpPost("avatar"), RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> Avatar(IFormFile file, CancellationToken ct)
        { 
            var (url, e) = await _svc.SetAvatarAsync(Me, file, ct); 
            return e is null ? Ok(new { url }) : BadRequest(new { error = e }); }

        [HttpDelete("avatar")] public async Task<IActionResult> RemoveAvatar(CancellationToken ct) => R(await _svc.RemoveAvatarAsync(Me, ct));

        [HttpPost("password")]
        public async Task<IActionResult> Password(ChangePasswordRequest req, CancellationToken ct)
        {
            var e = await _svc.ChangePasswordAsync(Me, req, ct);
            if (e is not null) return BadRequest(new { error = e });
            await _signIn.RefreshSignInAsync((await _users.GetUserAsync(User))!);   // cookie не слетит после смены SecurityStamp
            return NoContent();
        }

        private IActionResult R(string? e) => e is null ? NoContent() : BadRequest(new { error = e });
    }
}
