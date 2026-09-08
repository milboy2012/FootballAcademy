using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    public record ExtendDto(DateOnly NewTo, string? Comment);
    public record RejectDto(string? Reason);

    [ApiController, Route("api/subscriptions"), Authorize, IgnoreAntiforgeryToken]
    public class SubscriptionsApiController : ControllerBase
    {
        private readonly ISubscriptionService _svc;
        private readonly IParentService _parents;
        private readonly IUoW _data;
        public SubscriptionsApiController(ISubscriptionService svc, IParentService parents, IUoW data)
        {
            _svc = svc;
            _parents = parents;
            _data = data;
        }
        private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("plans")]
        public async Task<IActionResult> Plans(CancellationToken ct) => Ok(await _svc.GetPlansAsync(onlyActive: !User.IsInRole("Manager") && !User.IsInRole("Admin"), ct));

        // --- родитель ---
        [HttpGet("player/{playerId:guid}"), Authorize(Roles = "Parent,Manager")]
        public async Task<IActionResult> ForPlayer(Guid playerId, CancellationToken ct)
        {
            if (User.IsInRole("Parent") && !await _parents.OwnsAsync(Me, playerId, ct)) return Forbid();
            return Ok(new { status = await _svc.GetStatusAsync(playerId, ct), history = await _svc.GetForPlayerAsync(playerId, ct) });
        }
        [HttpPost("request"), Authorize(Roles = "Parent")]
        public async Task<IActionResult> Request(RequestSubscriptionDto dto, CancellationToken ct)
        {
            var (id, e) = await _svc.RequestAsync(Me, dto, ct);
            return e is null ? Ok(new { id }) : BadRequest(new { error = e });
        }
        [HttpDelete("request/{id:guid}"), Authorize(Roles = "Parent")]
        public async Task<IActionResult> CancelRequest(Guid id, CancellationToken ct) => R(await _svc.CancelRequestAsync(Me, id, ct));

        // --- менеджер ---
        [HttpGet, Authorize(Roles = "Manager")]
        public async Task<IActionResult> List([FromQuery] SubscriptionStatus? status, [FromQuery] string? search, CancellationToken ct) => Ok(new { data = await _svc.GetForManagerAsync(status, search, ct) });
        [HttpPost("{id:guid}/confirm"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Confirm([FromRoute] Guid id, [FromBody] ConfirmDtoReq dt, CancellationToken ct)
        {
            ConfirmDto dto = new ConfirmDto()
            {
                Amount = dt.Amount,
                Comment = dt.Comment,
                Method = dt.Method switch
                {
                    0 => PaymentMethod.Cash,
                    1 => PaymentMethod.Card,
                    2 => PaymentMethod.Transfer
                }
            };

            return R(await _svc.ConfirmAsync(Me, id, dto, ct));
        }
        [HttpPost("{id:guid}/reject"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Reject(Guid id, RejectDto dto, CancellationToken ct) => R(await _svc.RejectAsync(Me, id, dto.Reason, ct));
        [HttpPatch("{id:guid}/status"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> SetStatus(Guid id, [FromQuery] SubscriptionStatus value, CancellationToken ct) => R(await _svc.SetStatusAsync(id, value, ct));

        [HttpPost("{id:guid}/extend"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Extend(Guid id, ExtendDto dto, CancellationToken ct) => R(await _svc.ExtendAsync(id, dto.NewTo, dto.Comment, ct));
        [HttpGet("summary"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Summary(CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return Ok(new
            {
                pending = await _data.Subscriptions.Query().CountAsync(s => s.Status == SubscriptionStatus.PendingPayment, ct),
                active = await _data.Subscriptions.Query().CountAsync(s => s.Status == SubscriptionStatus.Active, ct),
                expiringSoon = await _data.Subscriptions.Query().CountAsync(s => s.Status == SubscriptionStatus.Active && s.To >= today && s.To <= today.AddDays(7), ct),
                monthRevenue = await _data.Payments.Query().Where(p => p.PaidAt >= new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)).SumAsync(p => p.Amount, ct)
            });
        }
        private IActionResult R(string? e) => e is null ? NoContent() : BadRequest(new { error = e });
    }
}
