using UI.Models.ViewModels.Player;

namespace UI.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<TabulatorPage<PlayerListItemDto>> GetPageAsync(TabulatorQuery q, Guid? parentOnly, CancellationToken ct);
        Task<PlayerListItemDto?> GetAsync(Guid id, CancellationToken ct);
        Task<Guid> CreateAsync(PlayerEditDto dto, CancellationToken ct);
        Task<bool> UpdateAsync(Guid id, PlayerEditDto dto, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
