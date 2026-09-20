using Core.Enums;
using System.Security.Claims;
using UI.Models.ViewModels.Post;

namespace UI.Services.Interfaces
{
    public interface IPostService
    {
        Task<List<PostDto>> GetFeedAsync(ClaimsPrincipal user, PostType? type, int take, CancellationToken ct);
        Task<List<PostDto>> GetAllAsync(CancellationToken ct);                        // для менеджера
        Task<PostDto?> GetAsync(Guid id, ClaimsPrincipal user, CancellationToken ct);
        Task<Guid> CreateAsync(Guid authorId, PostEditDto dto, CancellationToken ct);
        Task<string?> UpdateAsync(Guid id, PostEditDto dto, CancellationToken ct);
        Task<string?> DeleteAsync(Guid id, CancellationToken ct);
        Task<(string? Path, string? Error)> SetImageAsync(Guid id, IFormFile file, CancellationToken ct);
    }
}
