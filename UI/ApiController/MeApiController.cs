using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Player;
using UI.Services;

namespace UI.ApiController
{
    [ApiController, Route("api/me"), Authorize(Roles = "Player"), IgnoreAntiforgeryToken]
    public class MeApiController : ControllerBase
    {
        private readonly IPlayerCabinetService _svc;
        public MeApiController(IPlayerCabinetService svc) => _svc = svc;
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<IActionResult> WithPlayer(Func<Guid, Task<IActionResult>> action, CancellationToken ct)
            => await _svc.GetPlayerIdAsync(UserId, ct) is Guid pid ? await action(pid) : NotFound(new { error = "Учётная запись не привязана к ученику" });

        [HttpGet]
        public Task<IActionResult> Home(CancellationToken ct) => WithPlayer(async pid => Ok(await _svc.GetHomeAsync(pid, ct)), ct);

        [HttpGet("upcoming")]
        public Task<IActionResult> Upcoming([FromQuery] int days = 14, CancellationToken ct = default)
            => WithPlayer(async pid => Ok(await _svc.GetUpcomingAsync(pid, Math.Clamp(days, 1, 60), ct)), ct);

        [HttpPost("absence")]
        public Task<IActionResult> Notice(PlayerNoticeDto dto, CancellationToken ct)
            => WithPlayer(async pid => { var e = await _svc.NoticeAsync(pid, UserId, dto, ct); return e is null ? NoContent() : BadRequest(new { error = e }); }, ct);

        [HttpDelete("absence/{trainingId:guid}")]
        public Task<IActionResult> Withdraw(Guid trainingId, CancellationToken ct)
            => WithPlayer(async pid => { var e = await _svc.WithdrawAsync(pid, UserId, trainingId, ct); return e is null ? NoContent() : BadRequest(new { error = e }); }, ct);
    }
}
