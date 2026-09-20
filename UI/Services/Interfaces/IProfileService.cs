using UI.Models.ViewModels.Profile;

namespace UI.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetAsync(Guid userId, CancellationToken ct);
        Task<string?> UpdateAsync(Guid userId, UpdateProfileRequest req, CancellationToken ct);
        Task<(string? Url, string? Error)> SetAvatarAsync(Guid userId, IFormFile file, CancellationToken ct);
        Task<string?> RemoveAvatarAsync(Guid userId, CancellationToken ct);
        Task<string?> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct);
    }
}
