using UI.Models.ViewModels.Player;
using UI.Models.ViewModels.Users;

namespace UI.Services.Interfaces
{
    public interface IUserAdminService
    {
        Task<TabulatorPage<UserListItemDto>> GetPageAsync(UsersQuery q, CancellationToken ct);
        Task<UserDetailsDto?> GetAsync(Guid id, CancellationToken ct);
        Task<(string? Password, string? Error)> CreateManagerAsync(CreateManagerDto dto, CancellationToken ct);
        Task<string?> UpdateProfileAsync(Guid id, UpdateProfileDto dto, CancellationToken ct);
        Task<string?> SetBlockedAsync(Guid id, bool blocked, string? reason, Guid actorId, CancellationToken ct);
        Task<string?> ChangeRoleAsync(Guid id, string newRole, Guid actorId, CancellationToken ct);
        Task<(string? Password, string? Error)> ResetPasswordAsync(Guid id, CancellationToken ct);
    }
}
