using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;
using UI.Models.ViewModels.Cabinet;
using UI.Models.ViewModels.Subscription;
using UI.Services.Interfaces;


namespace UI.Controllers
{
    [Authorize(Roles = "Parent")]
    public class CabinetController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ISubscriptionService _subServ;
        private readonly IUoW _data;
        public CabinetController(UserManager<AppUser> userManager, IUoW data, ISubscriptionService subServ)
        {
            _data = data;
            _userManager = userManager;
            _subServ = subServ;
        }
        public async Task<IActionResult> Index(bool welcome = false, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var us = await _data.Players.Query().Include(s=>s.User).Where(s=>s.ParentId == user.Id).ToListAsync();            
            var uss = await _data.Users.Query().Where(s => s.Id == user.Id).FirstOrDefaultAsync();
            
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var children = new List<ChildCardVm>();

            var allChildren = await _data.Players.Query()
                .Where(p => p.ParentId == user.Id).AsNoTracking()
                .OrderBy(p => p.BirthDate).ToListAsync();

            foreach (var p in allChildren)
            {
                var subscriptionStatus = await _subServ.GetStatusAsync(p.Id, ct);
                DateTime? dateTime = new DateTime();
                if(p.GroupId != null)
                {
                    //var data = p.Group.Trainings
                    //    .Where(t => t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                    //    .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt).FirstOrDefault();
                    DateTime? data = _data.Trainings.Query()
                        .Where(t => t.GroupId == p.GroupId && t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                        .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt)
                        .FirstOrDefault();


                }
                TrainingGroup group = new TrainingGroup();
                Coach c = new Coach();
                AppUser coach = new AppUser();
                if (p.GroupId != null)
                {
                    group = await _data.Groups.Query().Include(s => s.Coach).FirstOrDefaultAsync(s=>s.Id == p.GroupId, ct);
                    
                    if(group != null)
                    {
                        c = await _data.Coaches.Query().Include(s=>s.User).FirstOrDefaultAsync(s=>s.Id == group.CoachId);
                        //p.Group = group;
                        //coach = await _data.Users.Query().FirstOrDefaultAsync(s => s.Id == c.UserId);
                    }
                        
                }

                DateTime? dt = null; 
                if(p.GroupId != null)
                {
                    var trng = await _data.Trainings.Query()
                        .Where(t => t.GroupId == p.GroupId && t.StartsAt >= DateTime.UtcNow  && t.Status == TrainingStatus.Planned)
                        .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt)
                        .FirstOrDefaultAsync();

                    var actTrng = await _data.Trainings.Query()
                        .Where(t => t.GroupId == p.GroupId && (t.EndsAt > DateTime.UtcNow && t.StartsAt < DateTime.UtcNow))
                        .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt)
                        .FirstOrDefaultAsync();

                    if (trng != null)
                        dt = trng;

                    if (actTrng != null)
                        dt = actTrng;
                }

                children.Add(new ChildCardVm
                {
                    Id = p.Id,
                    FullName = p.LastName + " " + p.FirstName,
                    BirthDate = p.BirthDate,
                    //GroupName = p.Group != null ? p.Group.Name : null,                    
                    //CoachName = p.Group != null ? p.Group.Coach.User.LastName + " " + p.Group.Coach.User.FirstName : null,
                    GroupName = group != null ?  group.Name : null,
                    CoachName = c.User != null ? c.User.LastName + " " + c.User.FirstName : null,
                    MedicalUntil = p.MedicalCertificateUntil,
                    IsActive = p.IsActive,
                    Login = p.User != null ? p.User.UserName : null,
                    AccountActive = p.User != null ? p.User.IsActive : null,
                    //ActiveSubscriptionUntil = p.Subscriptions
                    //    .Where(s => s.Status == SubscriptionStatus.Active && s.To >= today)
                    //    .OrderByDescending(s => s.To).Select(s => (DateOnly?)s.To).FirstOrDefault(),
                    Subscription = new SubscriptionStatusDto(subscriptionStatus.IsValid, subscriptionStatus.Text, subscriptionStatus.Status, subscriptionStatus.To, subscriptionStatus.TrainingsLeft, subscriptionStatus.HasPending),
                    //Subscription = new SubscriptionStatusDto(false, "нет подписки", SubscriptionStatus.Expired, null,null,false),
                    //NextTraining = p.GroupId != null
                    //? _data.Trainings.Query()
                    //    .Where(t => t.GroupId == p.GroupId && t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                    //    .OrderBy(t => t.StartsAt).Select(t => (DateTime?)t.StartsAt)
                    //    .FirstOrDefault()
                    //: null

                    NextTraining = dt,
                });
            }
                

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
