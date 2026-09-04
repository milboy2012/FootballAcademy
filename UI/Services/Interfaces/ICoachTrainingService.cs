using UI.Models.ViewModels.Coach;

namespace UI.Services.Interfaces
{
    public interface ICoachTrainingService
    {
        Task<Guid?> GetCoachIdAsync(Guid userId, CancellationToken ct);
        Task<List<CoachGroupDto>> GetGroupsAsync(Guid coachId, CancellationToken ct);
        Task<List<UpcomingDto>> GetUpcomingAsync(Guid coachId, int days, CancellationToken ct);
        Task<(TrainingDetailsDto? Dto, string? Error)> GetTrainingAsync(Guid trainingId, Guid coachId, CancellationToken ct);
        Task<string?> ConductAsync(Guid trainingId, Guid coachId, ConductDto dto, CancellationToken ct);
    }
}
