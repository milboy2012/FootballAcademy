using UI.Models.ViewModels.Group;
using UI.Models.ViewModels.Player;

namespace UI.Services.Interfaces
{
    public interface IGroupService
    {
        Task<TabulatorPage<GroupListItemDto>> GetPageAsync(GroupsQuery q, CancellationToken ct);
        Task<GroupListItemDto?> GetAsync(Guid id, CancellationToken ct);
        Task<(Guid? Id, string? Error)> CreateAsync(GroupEditDto dto, CancellationToken ct);
        Task<string?> UpdateAsync(Guid id, GroupEditDto dto, CancellationToken ct);
        Task<string?> AssignCoachAsync(Guid id, Guid coachId, CancellationToken ct);
        Task<List<GroupPlayerDto>> GetPlayersAsync(Guid id, CancellationToken ct);
        Task<string?> MovePlayersAsync(Guid id, MovePlayersDto dto, CancellationToken ct);
        Task<string?> ArchiveAsync(Guid id, bool archive, Guid? moveTo, CancellationToken ct);
        Task<List<CoachLookupDto>> GetCoachesAsync(CancellationToken ct);
    }
}
