using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Parent;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController, Route("api/parent"), Authorize(Roles = "Parent"), IgnoreAntiforgeryToken]
    public class ParentApiController : ControllerBase
    {
        private readonly IParentService _svc;
        public ParentApiController(IParentService svc) => _svc = svc;
        private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("children")]
        public async Task<IActionResult> Children(CancellationToken ct) => Ok(await _svc.GetChildrenAsync(Me, ct));

        [HttpGet("children/{playerId:guid}/attendance")]
        public async Task<IActionResult> Attendance(Guid playerId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
            => await _svc.OwnsAsync(Me, playerId, ct) ? Ok(await _svc.GetAttendanceAsync(playerId, from, to, ct)) : Forbid();

        [HttpGet("children/{playerId:guid}/notices")]
        public async Task<IActionResult> Notices(Guid playerId, CancellationToken ct)
            => await _svc.OwnsAsync(Me, playerId, ct) ? Ok(await _svc.GetNoticedTrainingIdsAsync(playerId, ct)) : Forbid();

        [HttpPost("absence")]
        public async Task<IActionResult> Notice(AbsenceNoticeDto dto, CancellationToken ct)
        { var e = await _svc.NoticeAbsenceAsync(Me, dto, ct); return e is null ? NoContent() : BadRequest(new { error = e }); }

        [HttpDelete("absence/{playerId:guid}/{trainingId:guid}")]
        public async Task<IActionResult> Withdraw(Guid playerId, Guid trainingId, CancellationToken ct)
        { var e = await _svc.WithdrawNoticeAsync(Me, playerId, trainingId, ct); return e is null ? NoContent() : BadRequest(new { error = e }); }

        [HttpGet("children/{playerId:guid}/progress")]
        public async Task<IActionResult> Progress(Guid playerId, [FromQuery] string? season, CancellationToken ct)
            => await _svc.OwnsAsync(Me, playerId, ct) ? Ok(await _svc.GetProgressAsync(playerId, season, ct)) : Forbid();
    }
}
