using UI.Models.ViewModels.Player;

namespace UI.Services
{
    public interface IPlayerCabinetService
    {
        Task<Guid?> GetPlayerIdAsync(Guid userId, CancellationToken ct);
        Task<PlayerHomeDto?> GetHomeAsync(Guid playerId, CancellationToken ct);
        Task<List<PlayerUpcomingDto>> GetUpcomingAsync(Guid playerId, int days, CancellationToken ct);
        Task<string?> NoticeAsync(Guid playerId, Guid userId, PlayerNoticeDto dto, CancellationToken ct);
        Task<string?> WithdrawAsync(Guid playerId, Guid userId, Guid trainingId, CancellationToken ct);
    }
}
