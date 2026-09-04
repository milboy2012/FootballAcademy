using Core.Enums;
using UI.Models.ViewModels.Schedule;

namespace UI.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<List<CalendarEventDto>> GetEventsAsync(DateTime from, DateTime to, Guid? groupId, Guid? venueId, Guid? forUserId, CancellationToken ct);
        Task<List<ConflictDto>> CheckAsync(Guid? selfId, EventKind kind, Guid groupId, Guid? opponentId, Guid venueId, DateTime start, DateTime end, CancellationToken ct);
        Task<(CreateResult? Result, string? Error, List<ConflictDto>? Conflicts)> CreateAsync(EventEditDto dto, CancellationToken ct);
        Task<(string? Error, List<ConflictDto>? Conflicts)> UpdateAsync(Guid id, EventEditDto dto, CancellationToken ct);
        Task<(string? Error, List<ConflictDto>? Conflicts)> MoveAsync(Guid id, MoveDto dto, CancellationToken ct);
        Task<string?> CancelAsync(Guid id, CancelDto dto, CancellationToken ct);
    }
}
