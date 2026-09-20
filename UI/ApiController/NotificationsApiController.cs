using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Models.ViewModels.Parent;

namespace UI.ApiController
{
    [ApiController, Route("api/notifications"), Authorize, IgnoreAntiforgeryToken]
    public class NotificationsApiController : ControllerBase
    {
        private readonly IUoW _data;
        public NotificationsApiController(IUoW data) => _data = data;
        private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            List<Notification> list = await _data.Notifications.Query().AsNoTracking()
            .Where(n => n.UserId == Me && n.ReadAt == null).OrderByDescending(n => n.CreatedAt).Take(30).ToListAsync(ct);

            var res = list.Select(n => new { n.Id, n.Title, n.Message, n.Link, n.CreatedAt, IsRead = n.ReadAt != null }).ToList();

            return Ok(res);
            
        }
        [HttpPost("read")]
        public async Task<IActionResult> Read(Guid id, CancellationToken ct)
        {
            var notif = await _data.Notifications.GetByIdAsync(id);
            notif.ReadAt = DateTime.UtcNow;
            _data.Notifications.Update(notif);
            return notif is null ? NoContent() : BadRequest(new { error = notif }); 
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> Unread(CancellationToken ct) => Ok(await _data.Notifications.Query().CountAsync(n => n.UserId == Me && n.ReadAt == null, ct));

        [HttpPost("read-all")]
        public async Task<IActionResult> ReadAll(CancellationToken ct)
        {
            await _data.Notifications.Query().Where(n => n.UserId == Me && n.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
            return NoContent();
        }
    }
}
