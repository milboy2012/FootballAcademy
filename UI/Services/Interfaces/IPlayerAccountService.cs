using UI.Services.Model;

namespace UI.Services.Interfaces
{
    public interface IPlayerAccountService
    {
        Task<(PlayerAccountInfo? Info, string? Error)> CreateAsync(Guid playerId, string? password, CancellationToken ct);
        Task<(string? NewPassword, string? Error)> ResetPasswordAsync(Guid playerId, CancellationToken ct);
        Task<string?> SetActiveAsync(Guid playerId, bool active, CancellationToken ct);
    }
}
