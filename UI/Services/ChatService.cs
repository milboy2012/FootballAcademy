using Core.Entity;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Models.DataModels;
using UI.Models.ViewModels.Chat;
using UI.Services.Interfaces;
using UI.Services.Model;

namespace UI.Services
{
    public class ChatService : IChatService
    {
        private readonly IUoW _data;
        public ChatService(IUoW data) => _data = data;
        private static Guid Uid(ClaimsPrincipal u) => Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<List<ChatGroupDto>> MyGroupsAsync(ClaimsPrincipal user, CancellationToken ct)
        {
            var uid = Uid(user);
            var q = _data.Groups.Query().AsNoTracking().Where(g => !g.IsArchived);
            if (!(user.IsInRole("Admin") || user.IsInRole("Manager"))) { var ids = await GroupAccess.MyGroupIdsAsync(_data, uid, ct); q = q.Where(g => ids.Contains(g.Id)); }

            var groups = await q.OrderBy(g => g.Name).Select(g => new
            {
                g.Id,
                g.Name,
                g.Color,
                Coach = g.Coach.User.FirstName + " " + g.Coach.User.LastName,
                Members = g.Players.Count(p => p.IsActive) + g.Players.Count(p => p.IsActive && p.UserId != null) + 1,                
                Last = _data.ChatMessages.Query().Where(m => m.GroupId == g.Id).OrderByDescending(m => m.CreatedAt).Select(m => new { m.Text, m.CreatedAt, m.SenderId }).FirstOrDefault(),
                ReadUpTo = _data.ChatReadMarks.Query().Where(r => r.GroupId == g.Id && r.UserId == uid).Select(r => (DateTime?)r.ReadUpTo).FirstOrDefault()
            }).ToListAsync(ct);

            var result = new List<ChatGroupDto>();
            foreach (var g in groups)
            {
                var unread = await _data.ChatMessages.Query().CountAsync(m => m.GroupId == g.Id && m.SenderId != uid && (g.ReadUpTo == null || m.CreatedAt > g.ReadUpTo), ct);
                result.Add(new(g.Id, g.Name, g.Color, g.Coach, g.Members, unread, g.Last?.Text, g.Last?.CreatedAt));
            }
            return result.OrderByDescending(r => r.LastAt ?? DateTime.MinValue).ToList();
        }

        public async Task<List<ChatMessageDto>> HistoryAsync(ClaimsPrincipal user, Guid groupId, DateTime? before, int take, CancellationToken ct)
        {
            if (!await GroupAccess.CanAccessAsync(_data, user, groupId, ct)) return [];
            var q = _data.ChatMessages.Query().AsNoTracking().Where(m => m.GroupId == groupId);
            if (before is not null) q = q.Where(m => m.CreatedAt < before);
            var list = await Project(q.OrderByDescending(m => m.CreatedAt).Take(take)).ToListAsync(ct);
            list.Reverse(); return list;
        }

        public async Task<(ChatMessageDto?, string?)> SendAsync(ClaimsPrincipal user, Guid groupId, string text, Guid? replyToId, CancellationToken ct)
        {
            text = text?.Trim() ?? "";
            if (text.Length is 0 or > 2000) return (null, "Сообщение от 1 до 2000 символов");
            if (!await GroupAccess.CanAccessAsync(_data, user, groupId, ct)) return (null, "Нет доступа к этой группе");
            if (replyToId is not null && !await _data.ChatMessages.AnyAsync(m => m.Id == replyToId && m.GroupId == groupId, ct)) replyToId = null;

            var m = new ChatMessage { GroupId = groupId, SenderId = Uid(user), Text = text, ReplyToId = replyToId };
            await _data.ChatMessages.AddAsync(m); await _data.SaveChangesAsync(ct);
            await MarkReadAsync(m.SenderId, groupId, ct);
            return (await Project(_data.ChatMessages.Query().Where(x => x.Id == m.Id)).FirstAsync(ct), null);
        }

        public async Task<(ChatMessageDto?, string?)> EditAsync(Guid userId, Guid id, string text, CancellationToken ct)
        {
            text = text?.Trim() ?? ""; if (text.Length is 0 or > 2000) return (null, "Сообщение от 1 до 2000 символов");
            var m = await _data.ChatMessages.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (m is null || m.SenderId != userId) return (null, "Можно редактировать только свои сообщения");
            if (m.CreatedAt < DateTime.UtcNow.AddHours(-24)) return (null, "Сообщение старше суток не редактируется");
            m.Text = text; m.IsEdited = true; await _data.SaveChangesAsync(ct);
            return (await Project(_data.ChatMessages.Query().Where(x => x.Id == id)).FirstAsync(ct), null);
        }

        public async Task<(Guid, string?)> DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
        {
            var m = await _data.ChatMessages.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (m is null) return (Guid.Empty, "Не найдено");
            var isModerator = user.IsInRole("Admin") || user.IsInRole("Manager") || (user.IsInRole("Coach") && await _data.Groups.AnyAsync(g => g.Id == m.GroupId && g.Coach.UserId == Uid(user), ct));
            if (m.SenderId != Uid(user) && !isModerator) return (Guid.Empty, "Нет прав на удаление");
            _data.ChatMessages.Delete(m); await _data.SaveChangesAsync(ct);
            return (m.GroupId, null);
        }

        public async Task MarkReadAsync(Guid userId, Guid groupId, CancellationToken ct)
        {
            var r = await _data.ChatReadMarks.Query().FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId, ct)
                    ?? await _data.ChatReadMarks.AddAsync(new ChatReadMark { GroupId = groupId, UserId = userId });
            r.ReadUpTo = DateTime.UtcNow; await _data.SaveChangesAsync(ct);
        }

        public async Task<List<object>> MembersAsync(ClaimsPrincipal user, Guid groupId, CancellationToken ct)
        {
            if (!await GroupAccess.CanAccessAsync(_data, user, groupId, ct)) return [];
            var coach = await _data.Groups.Query().Where(g => g.Id == groupId).Select(g => new { Name = g.Coach.User.FirstName + " " + g.Coach.User.LastName, Role = "Тренер", g.Coach.User.AvatarPath }).FirstAsync(ct);
            var players = await _data.Players.Query().Where(p => p.GroupId == groupId && p.IsActive).Select(p => new { Name = p.FirstName + " " + p.LastName, Role = p.UserId != null ? "Игрок" : "Игрок (без аккаунта)", AvatarPath = p.User != null ? p.User.AvatarPath : null, Parent = p.Parent.FirstName + " " + p.Parent.LastName, ParentAvatar = p.Parent.AvatarPath }).ToListAsync(ct);
            var list = new List<object> { coach };
            list.AddRange(players.Select(p => new { p.Name, p.Role, p.AvatarPath }));
            list.AddRange(players.Select(p => new { Name = p.Parent, Role = "Родитель", AvatarPath = p.ParentAvatar }).DistinctBy(x => x.Name));
            return list;
        }

        private IQueryable<ChatMessageDto> Project(IQueryable<ChatMessage> q) => q.Select(m => new ChatMessageDto(m.Id, m.GroupId, m.SenderId,
        m.Sender.FirstName + " " + m.Sender.LastName,
        //_data.Roles.Query().Where(ur => ur.Id== m.SenderId).Join(_data.Roles, ur => ur.Id , r => r.Id, (ur, r) => r.Name!).FirstOrDefault() ?? "",
        "" ?? "",
        m.Sender.AvatarPath, m.Text, m.CreatedAt, m.IsEdited, m.ReplyToId,
        m.ReplyTo != null ? m.ReplyTo.Text : null, m.ReplyTo != null ? m.ReplyTo.Sender.FirstName + " " + m.ReplyTo.Sender.LastName : null));
    }

}
