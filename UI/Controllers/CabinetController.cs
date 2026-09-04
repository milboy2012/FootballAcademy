using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UI.Models.ViewModels.Cabinet;
using System.Security.Claims;
using Core.Enums;
using Microsoft.EntityFrameworkCore;


namespace UI.Controllers
{
    [Authorize(Roles = "Parent")]
    public class CabinetController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUoW _data;
        public CabinetController(UserManager<AppUser> userManager, IUoW data)
        {
            _data = data;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(bool welcome = false, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();



            var today = DateOnly.FromDateTime(DateTime.UtcNow);            

            var children = _data.Players.Query()
                .Where(p => p.ParentId == user.Id).AsNoTracking()
                .OrderBy(p => p.BirthDate)
                .Select(p => new ChildCardVm
                {
                    Id = p.Id,
                    FullName = p.LastName + " " + p.FirstName,
                    BirthDate = p.BirthDate,
                    GroupName = p.Group != null ? p.Group.Name : null,
                    CoachName = p.Group != null ? p.Group.Coach.User.LastName + " " + p.Group.Coach.User.FirstName : null,
                    MedicalUntil = p.MedicalCertificateUntil,
                    IsActive = p.IsActive,
                    Login = p.User != null ? p.User.UserName : null,
                    AccountActive = p.User != null ? p.User.IsActive : null,
                    ActiveSubscriptionUntil = p.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active && s.To >= today)
                        .OrderByDescending(s => s.To).Select(s => (DateOnly?)s.To).FirstOrDefault(),
                    NextTraining = p.Group != null
                        ? p.Group.Trainings
                            .Where(t => t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                            .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt).FirstOrDefault()
                        : null
                })
                .ToList();

            return View(new CabinetVm
            {
                ParentName = $"{user.FirstName} {user.LastName}",
                Email = user.Email!,
                Phone = user.PhoneNumber,
                Children = children,
                ShowWelcome = welcome && children.Count == 0
            });
        }
    }
}
