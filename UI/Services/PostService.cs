using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UI.Models.ViewModels.Post;
using UI.Services.Interfaces;
using UI.Services.Model;

namespace UI.Services
{
    public class PostService : IPostService
    {
        private readonly IUoW _data; 
        private readonly IWebHostEnvironment _env;
        public PostService(IUoW data, IWebHostEnvironment env) { 
            _data = data; 
            _env = env; 
        }

        private static IQueryable<PostDto> Project(IQueryable<Post> q) => q.Select(p => new PostDto(p.Id, p.Type, p.Title, p.Body, p.ImagePath, p.EventDate, p.Location,
            p.Audience, p.IsPinned, p.IsPublished, p.PublishedAt, p.Author.FirstName + " " + p.Author.LastName, p.GroupId, p.Group != null ? p.Group.Name : null, p.CreatedAt));

        /// <summary>Лента с учётом аудитории и групп пользователя.</summary>
        public async Task<List<PostDto>> GetFeedAsync(ClaimsPrincipal user, PostType? type, int take, CancellationToken ct)
        {
            var q = _data.Posts.Query().AsNoTracking().Where(p => p.IsPublished);
            if (type is not null) q = q.Where(p => p.Type == type);

            var auth = user.Identity?.IsAuthenticated == true;
            if (!auth) q = q.Where(p => p.Audience == PostAudience.Everyone && p.GroupId == null);
            else
            {
                var uid = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var staff = user.IsInRole("Admin") || user.IsInRole("Manager");
                var isParent = user.IsInRole("Parent"); var isCoach = user.IsInRole("Coach");
                var myGroups = staff ? null : await GroupAccess.MyGroupIdsAsync(_data, uid, ct);

                q = q.Where(p => p.Audience == PostAudience.Everyone || p.Audience == PostAudience.Authenticated
                              || (p.Audience == PostAudience.Parents && (isParent || staff)) || (p.Audience == PostAudience.Coaches && (isCoach || staff)));
                if (myGroups is not null) q = q.Where(p => p.GroupId == null || myGroups.Contains(p.GroupId.Value));
            }
            return await Project(q.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.PublishedAt).Take(take)).ToListAsync(ct);
        }

        public Task<List<PostDto>> GetAllAsync(CancellationToken ct) => Project(_data.Posts.Query().AsNoTracking().OrderByDescending(p => p.CreatedAt)).ToListAsync(ct);

        public async Task<PostDto?> GetAsync(Guid id, ClaimsPrincipal user, CancellationToken ct)
            => (await GetFeedAsync(user, null, int.MaxValue, ct)).FirstOrDefault(p => p.Id == id)
               ?? (user.IsInRole("Manager") || user.IsInRole("Admin") ? await Project(_data.Posts.Query().Where(p => p.Id == id)).FirstOrDefaultAsync(ct) : null);

        public async Task<Guid> CreateAsync(Guid authorId, PostEditDto dto, CancellationToken ct)
        {
            var p = new Post { AuthorId = authorId }; Apply(p, dto);
            await _data.Posts.AddAsync(p); 
            await _data.SaveChangesAsync(ct);
            if (dto.IsPublished && dto.Notify) await NotifyAsync(p, ct);
            return p.Id;
        }

        public async Task<string?> UpdateAsync(Guid id, PostEditDto dto, CancellationToken ct)
        {
            var p = await _data.Posts.Query().FirstOrDefaultAsync(x => x.Id == id, ct); if (p is null) return "Публикация не найдена";
            var wasPublished = p.IsPublished; Apply(p, dto); await _data.SaveChangesAsync(ct);
            if (!wasPublished && p.IsPublished && dto.Notify) await NotifyAsync(p, ct);
            return null;
        }

        public async Task<string?> DeleteAsync(Guid id, CancellationToken ct)
        {
            var p = await _data.Posts.Query().FirstOrDefaultAsync(x => x.Id == id, ct); if (p is null) return "Не найдено";
            _data.Posts.Delete(p); await _data.SaveChangesAsync(ct); return null;
        }

        public async Task<(string?, string?)> SetImageAsync(Guid id, IFormFile file, CancellationToken ct)
        {
            var p = await _data.Posts.Query().FirstOrDefaultAsync(x => x.Id == id, ct); if (p is null) return (null, "Не найдено");
            if (file.Length > 5 * 1024 * 1024 || !file.ContentType.StartsWith("image/")) return (null, "Изображение до 5 МБ");
            var dir = Path.Combine(_env.WebRootPath, "uploads", "posts"); Directory.CreateDirectory(dir);
            var name = $"{id}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using (var fs = File.Create(Path.Combine(dir, name))) await file.CopyToAsync(fs, ct);
            p.ImagePath = $"/uploads/posts/{name}?v={DateTime.UtcNow.Ticks}"; await _data.SaveChangesAsync(ct);
            return (p.ImagePath, null);
        }

        private static void Apply(Post p, PostEditDto d)
        {
            p.Type = d.Type; p.Title = d.Title.Trim(); p.Body = d.Body.Trim(); p.EventDate = d.EventDate?.ToUniversalTime(); p.Location = d.Location?.Trim();
            p.Audience = d.Audience; p.IsPinned = d.IsPinned; p.GroupId = d.GroupId;
            if (d.IsPublished && !p.IsPublished) p.PublishedAt = DateTime.UtcNow;
            p.IsPublished = d.IsPublished;
        }

        private async Task NotifyAsync(Post p, CancellationToken ct)
        {
            IQueryable<Guid> users = _data.Users.Query().Where(u => u.IsActive).Select(u => u.Id);
            if (p.GroupId is Guid g)
                users = _data.Players.Query().Where(x => x.GroupId == g && x.IsActive).Select(x => x.ParentId)
                    .Union(_data.Players.Query().Where(x => x.GroupId == g && x.UserId != null).Select(x => x.UserId!.Value))
                    .Union(_data.Groups.Query().Where(x => x.Id == g).Select(x => x.Coach.UserId));
            else if (p.Audience == PostAudience.Parents) users = _data.Players.Query().Where(x => x.IsActive).Select(x => x.ParentId).Distinct();
            else if (p.Audience == PostAudience.Coaches) users = _data.Coaches.Query().Select(c => c.UserId);

            var title = p.Type switch { PostType.Competition => "Соревнование", PostType.Holiday => "Праздник", PostType.News => "Новость", _ => "Объявление" };
            await _data.Notifications.AddRangeAsync((await users.ToListAsync(ct)).Select(u => new Notification { UserId = u, Title = title, Message = p.Title, Link = $"/News/{p.Id}" }));
            await _data.SaveChangesAsync(ct);
        }
    }
}
