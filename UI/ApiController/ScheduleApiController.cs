using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Schedule;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/schedule")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ScheduleApiController : ControllerBase
    {
        private readonly IScheduleService _svc;
        public ScheduleApiController(IScheduleService svc) => _svc = svc;

        private bool IsManager => User.IsInRole("Admin") || User.IsInRole("Manager");

        /// <summary>FullCalendar: ?start=...&end=... (ISO). Менеджер видит всё, остальные — свои группы.</summary>
        [HttpGet]
        public async Task<IActionResult> Events([FromQuery] DateTimeOffset start, [FromQuery] DateTimeOffset end,
            [FromQuery] Guid? groupId, [FromQuery] Guid? venueId, CancellationToken ct)
        {
            Guid? forUser = IsManager ? null : Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _svc.GetEventsAsync(start.UtcDateTime, end.UtcDateTime, groupId, venueId, forUser, ct));
        }

        [HttpPost("check")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Check([FromQuery] Guid? selfId, EventEditDto dto, CancellationToken ct)
            => Ok(await _svc.CheckAsync(selfId, dto.Kind, dto.GroupId, dto.OpponentGroupId, dto.VenueId, dto.Start.UtcDateTime, dto.End.UtcDateTime, ct));

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(EventEditDto dto, CancellationToken ct)
        {
            var (result, error, conflicts) = await _svc.CreateAsync(dto, ct);
            if (error is not null) return BadRequest(new { error });
            if (conflicts is not null) return Conflict(new { error = "Есть пересечения", conflicts });
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(Guid id, EventEditDto dto, CancellationToken ct) => R(await _svc.UpdateAsync(id, dto, ct));

        [HttpPatch("{id:guid}/move")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Move(Guid id, MoveDto dto, CancellationToken ct) => R(await _svc.MoveAsync(id, dto, ct));

        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Cancel(Guid id, CancelDto dto, CancellationToken ct)
        {
            var e = await _svc.CancelAsync(id, dto, ct);
            return e is null ? NoContent() : BadRequest(new { error = e });
        }

        private IActionResult R((string? Error, List<ConflictDto>? Conflicts) r)
        {
            if (r.Error is not null) return BadRequest(new { error = r.Error });
            if (r.Conflicts is not null) return Conflict(new { error = "Есть пересечения", conflicts = r.Conflicts });
            return NoContent();
        }
    }
}
