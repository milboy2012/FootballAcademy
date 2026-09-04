using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Venue;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/venues")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class VenuesApiController : ControllerBase
    {
        private readonly IUoW _data;
        public VenuesApiController(IUoW data) => _data = data;

        /// <summary>Список доступен всем авторизованным (нужен тренерам для расписания).</summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool? active, CancellationToken ct)
        {
            var q = _data.Venues.Query().AsNoTracking();
            if (active is not null) q = q.Where(v => v.IsActive == active);
            var now = DateTime.UtcNow;
            var data = await q.OrderBy(v => v.Name).Select(v => new
            {
                v.Id,
                v.Name,
                v.Address,
                v.IsIndoor,
                v.Capacity,
                v.Description,
                v.IsActive,
                UpcomingTrainings = v.Trainings.Count(t => t.StartsAt >= now && t.Status == TrainingStatus.Planned)
            }).ToListAsync(ct);
            return Ok(new { data });
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(VenueDto dto, CancellationToken ct)
        {
            if (Validate(dto) is { } e) return BadRequest(new { error = e });
            if (await _data.Venues.AnyAsync(v => v.Name == dto.Name.Trim(), ct)) return BadRequest(new { error = "Место с таким названием уже есть" });
            var v = new Venue(); Apply(v, dto);
            _data.Venues.AddAsync(v, ct); await _data.SaveChangesAsync(ct);
            return Ok(new { v.Id });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(Guid id, VenueDto dto, CancellationToken ct)
        {
            if (Validate(dto) is { } e) return BadRequest(new { error = e });
            var v = await _data.Venues.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (v is null) return NotFound();
            if (await _data.Venues.AnyAsync(x => x.Id != id && x.Name == dto.Name.Trim(), ct)) return BadRequest(new { error = "Место с таким названием уже есть" });
            Apply(v, dto); await _data.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var v = await _data.Venues.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (v is null) return NotFound();
            var upcoming = await _data.Trainings.Query().CountAsync(t => t.VenueId == id && t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned, ct);
            if (upcoming > 0) return BadRequest(new { error = $"На этом месте запланировано {upcoming} тренировок. Перенесите их или отключите место вместо удаления" });
            _data.Venues.Delete(v); await _data.SaveChangesAsync(ct);   // soft delete
            return NoContent();
        }

        private static string? Validate(VenueDto d)
        {
            if (string.IsNullOrWhiteSpace(d.Name)) return "Название обязательно";
            if (d.Capacity is <= 0) return "Вместимость должна быть положительной";
            return null;
        }

        private static void Apply(Venue v, VenueDto d)
        {
            v.Name = d.Name.Trim(); v.Address = d.Address?.Trim(); v.IsIndoor = d.IsIndoor;
            v.Capacity = d.Capacity; v.Description = d.Description?.Trim(); v.IsActive = d.IsActive;
        }
    }
}
