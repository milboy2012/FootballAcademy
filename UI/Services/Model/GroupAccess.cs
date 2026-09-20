using Core.Entity;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace UI.Services.Model
{
    public class GroupAccess
    {
        public static async Task<List<Guid>> MyGroupIdsAsync(IUoW data, Guid userId, CancellationToken ct)
        => await data.Players.Query().Where(p => (p.ParentId == userId || p.UserId == userId) && p.GroupId != null && p.IsActive).Select(p => p.GroupId!.Value)
            .Union(data.Groups.Query().Where(g => g.Coach.UserId == userId).Select(g => g.Id))
            .Distinct().ToListAsync(ct);

        public static async Task<bool> CanAccessAsync(IUoW data, ClaimsPrincipal user, Guid groupId, CancellationToken ct)
        {
            if (user.IsInRole("Admin") || user.IsInRole("Manager")) return true;
            var uid = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return (await MyGroupIdsAsync(data, uid, ct)).Contains(groupId);
        }
    }
}
