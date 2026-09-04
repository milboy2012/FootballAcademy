using UI.Models.ViewModels.Parent;

namespace UI.Services.Interfaces
{
    public interface IParentService
    {
        Task<List<ChildBriefDto>> GetChildrenAsync(Guid parentId, CancellationToken ct);
        Task<bool> OwnsAsync(Guid parentId, Guid playerId, CancellationToken ct);
        Task<AttendanceHistoryDto> GetAttendanceAsync(Guid playerId, DateOnly? from, DateOnly? to, CancellationToken ct);
        Task<string?> NoticeAbsenceAsync(Guid parentId, AbsenceNoticeDto dto, CancellationToken ct);
        Task<string?> WithdrawNoticeAsync(Guid parentId, Guid playerId, Guid trainingId, CancellationToken ct);
        Task<List<Guid>> GetNoticedTrainingIdsAsync(Guid playerId, CancellationToken ct);
        Task<ProgressDto> GetProgressAsync(Guid playerId, string? season, CancellationToken ct);
    }
}
