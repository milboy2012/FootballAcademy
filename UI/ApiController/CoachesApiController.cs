using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    public record CreateCoachRequest(string Email, string? FirstName, string? LastName);

    [ApiController]
    [Route("api/coaches")]
    [Authorize(Roles = "Admin,Manager")]
    [IgnoreAntiforgeryToken]
    public class CoachesApiController : ControllerBase
    {
        private readonly IUoW _data;
        private readonly ICoachOnboardingService _onboarding;

        public CoachesApiController(IUoW data, ICoachOnboardingService onboarding)
        {
            _data = data;
            _onboarding = onboarding;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var data = await _data.Coaches.Query().AsNoTracking()
                .OrderBy(c => c.User.LastName)
                .Select(c => new
                {
                    c.Id,
                    c.User.Email,
                    FullName = c.User.LastName + " " + c.User.FirstName,
                    c.HiredAt,
                    c.Qualification,
                    c.User.IsActive,
                    c.User.MustChangePassword,
                    GroupsCount = c.Groups.Count
                })
                .ToListAsync(ct);
            //return Ok(new { data });
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCoachRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(req.Email))
                return BadRequest(new { error = "Некорректный email" });

            var (result, error) = await _onboarding.CreateAsync(req.Email, req.FirstName, req.LastName, ct);
            return error is null ? Ok(result) : BadRequest(new { error });
        }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> Reset(Guid id, CancellationToken ct)
        {
            var (password, error) = await _onboarding.ResetTemporaryPasswordAsync(id, ct);
            return error is null ? Ok(new { password }) : BadRequest(new { error });
        }
    }
}
