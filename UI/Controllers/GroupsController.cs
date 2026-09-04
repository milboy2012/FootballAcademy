using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Models.ViewModels.Group;
using UI.Services.Interfaces;

namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class GroupsController : Controller
    {
        private readonly IGroupService _svc;
        public GroupsController(IGroupService svc) => _svc = svc;

        public IActionResult Index() => View();

        /// <summary>Печатная форма: ?mode=journal — журнал посещаемости, ?mode=parents — список для родителей.</summary>
        public async Task<IActionResult> Print(Guid id, string mode = "journal", int weeks = 4, CancellationToken ct = default)
        {
            var group = await _svc.GetAsync(id, ct);
            if (group is null) return NotFound();
            ViewBag.Mode = mode; ViewBag.Weeks = Math.Clamp(weeks, 1, 12);
            return View(new GroupPrintVm(group, await _svc.GetPlayersAsync(id, ct)));
        }
    }
}

public record GroupPrintVm(GroupListItemDto Group, List<GroupPlayerDto> Players);
