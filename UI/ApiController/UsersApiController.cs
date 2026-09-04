using Azure.Core;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Users;
using UI.Services;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin,Manager")]
    [IgnoreAntiforgeryToken]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserAdminService _svc;
        public UsersApiController(IUserAdminService svc) => _svc = svc;

        private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] UsersQuery q, CancellationToken ct)
        {
            q.SortField ??= Request.Query["sort[0][field]"].FirstOrDefault();
            q.SortDir ??= Request.Query["sort[0][dir]"].FirstOrDefault();
            return Ok(await _svc.GetPageAsync(q, ct));
        }

        [HttpGet("roles")]
        public IActionResult Roles() => Ok(UserAdminService.AssignableRoles);

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await _svc.GetAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

        [HttpPost("managers")]
        public async Task<IActionResult> CreateManager(CreateManagerDto dto, CancellationToken ct)
        {
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email))
                return BadRequest(new { error = "Некорректный email" });
            var (password, error) = await _svc.CreateManagerAsync(dto, ct);
            return error is null ? Ok(new { email = dto.Email, password }) : BadRequest(new { error });
        }

        [HttpPut("{id:guid}/profile")]
        public async Task<IActionResult> UpdateProfile(Guid id, UpdateProfileDto dto, CancellationToken ct)
            => Result(await _svc.UpdateProfileAsync(id, dto, ct));

        [HttpPatch("{id:guid}/block")]
        public async Task<IActionResult> Block(Guid id, BlockDto dto, CancellationToken ct)
            => Result(await _svc.SetBlockedAsync(id, dto.Blocked, dto.Reason, ActorId, ct));

        [HttpPatch("{id:guid}/role")]
        public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleDto dto, CancellationToken ct)
            => Result(await _svc.ChangeRoleAsync(id, dto.Role, ActorId, ct));

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> Reset(Guid id, CancellationToken ct)
        {
            var (password, error) = await _svc.ResetPasswordAsync(id, ct);
            return error is null ? Ok(new { password }) : BadRequest(new { error });
        }

        private IActionResult Result(string? error) => error is null ? NoContent() : BadRequest(new { error });
    }
}
