using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Models.ViewModels.Coach;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/coach")]
    [Authorize(Roles = "Coach,Admin,Manager")]
    [IgnoreAntiforgeryToken]
    public class CoachApiController : ControllerBase
    {
        private readonly ICoachTrainingService _svc;
        private readonly IUoW _data;
        public CoachApiController(ICoachTrainingService svc, IUoW data)
        {
            _svc = svc;
            _data = data;
        }

        //рекорды для для отправки оценок навыков родителям
        public record ScoreItem(Guid SkillId, int Value);
        public record AssessDto(DateOnly Date, string? Comment, List<ScoreItem> Scores);

        private async Task<Guid?> CoachIdAsync(CancellationToken ct)
            => await _svc.GetCoachIdAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);

        [HttpGet("groups")]
        public async Task<IActionResult> Groups(CancellationToken ct)
            => await CoachIdAsync(ct) is Guid c ? Ok(await _svc.GetGroupsAsync(c, ct)) : NotFound(new { error = "Профиль тренера не найден" });

        [HttpGet("upcoming")]
        public async Task<IActionResult> Upcoming([FromQuery] int days = 7, CancellationToken ct = default)
            => await CoachIdAsync(ct) is Guid c ? Ok(await _svc.GetUpcomingAsync(c, Math.Clamp(days, 1, 60), ct)) : NotFound();

        [HttpGet("trainings/{id:guid}")]
        public async Task<IActionResult> Training(Guid id, CancellationToken ct)
        {
            if (await CoachIdAsync(ct) is not Guid c) return NotFound();
            var (dto, error) = await _svc.GetTrainingAsync(id, c, ct);
            return error is null ? Ok(dto) : NotFound(new { error });
        }

        [HttpPut("trainings/{id:guid}/conduct")]
        public async Task<IActionResult> Conduct(Guid id, ConductDto dto, CancellationToken ct)
        {
            if (await CoachIdAsync(ct) is not Guid c) return NotFound();
            var error = await _svc.ConductAsync(id, c, dto, ct);
            return error is null ? NoContent() : BadRequest(new { error });
        }

        //--методы для отправки оценок навыков родителям
        [HttpGet("skills")]
        public async Task<IActionResult> Skills([FromServices] ContextAuth ctx, CancellationToken ct) 
            => Ok(await _data.Skills.Query().Where(s => s.IsActive).OrderBy(s => s.SortOrder).Select(s => new { s.Id, s.Name }).ToListAsync(ct));

        [HttpGet("players/{playerId:guid}/assessments")]
        public async Task<IActionResult> Assessments(Guid playerId, [FromServices] IParentService ps, CancellationToken ct)
            => Ok(await ps.GetProgressAsync(playerId, null, ct)); // тот же DTO, что видит родитель

        [HttpPost("players/{playerId:guid}/assessments")]
        public async Task<IActionResult> Assess(Guid playerId, AssessDto dto, [FromServices] ContextAuth ctx, CancellationToken ct)
        {
            if (await CoachIdAsync(ct) is not Guid coachId) return NotFound();
            var player = await ctx.Players.Include(p => p.Group).FirstOrDefaultAsync(p => p.Id == playerId && p.Group!.CoachId == coachId, ct);
            if (player is null) return Forbid();
            if (dto.Scores.Count == 0 || dto.Scores.Any(s => s.Value is < 1 or > 10)) return BadRequest(new { error = "Оценки от 1 до 10" });
            if (await ctx.SkillAssessments.AnyAsync(a => a.PlayerId == playerId && a.Date == dto.Date, ct)) return BadRequest(new { error = "На эту дату оценка уже есть" });

            var a = new SkillAssessment
            {
                PlayerId = playerId,
                CoachId = coachId,
                Date = dto.Date,
                Season = player.Group!.Season,
                Comment = dto.Comment?.Trim(),
                Scores = dto.Scores.Select(s => new SkillScore { SkillId = s.SkillId, Value = s.Value }).ToList()
            };
            ctx.SkillAssessments.Add(a);
            ctx.Notifications.Add(new Notification { UserId = player.ParentId, Title = "Новая оценка навыков", Message = $"Тренер оценил навыки {player.FirstName} ({dto.Date:dd.MM.yyyy})", Link = $"/Parent/Progress/{playerId}" });
            await ctx.SaveChangesAsync(ct);
            return Ok(new { a.Id });
        }
    }
}
