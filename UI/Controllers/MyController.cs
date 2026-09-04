using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Models.ViewModels.My;

namespace UI.Controllers
{
    [Authorize(Roles = "Player")]
    public class MyController : Controller
    {
        private readonly IUoW _data;
        public MyController(IUoW data) => _data = data;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var me = await _data.Players.Query().AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new PlayerHomeVm
                {
                    Name = p.FirstName,
                    GroupName = p.Group != null ? p.Group.Name : null,
                    CoachName = p.Group != null ? p.Group.Coach.User.FirstName + " " + p.Group.Coach.User.LastName : null,
                    Upcoming = p.Group != null
                        ? p.Group.Trainings.Where(t => t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                            .OrderBy(t => t.StartsAt).Take(5)
                            .Select(t => new UpcomingVm(t.StartsAt, t.Venue.Name)).ToList()
                        : new(),
                    TotalTrainings = p.Attendances.Count,
                    Visited = p.Attendances.Count(a => a.Present)
                })
                .FirstOrDefaultAsync(ct);

            return me is null ? View("NotLinked") : View(me);
        }

        public IActionResult PlayerCab() => View();
        public IActionResult Schedule() => View();
    }
}
