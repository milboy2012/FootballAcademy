using UI.Services.Model;

namespace UI.Services.Interfaces
{
    public interface ICoachOnboardingService
    {
        Task<(CoachCreatedDto? Result, string? Error)> CreateAsync(string email, string? firstName, string? lastName, CancellationToken ct);
        Task<(string? Password, string? Error)> ResetTemporaryPasswordAsync(Guid coachId, CancellationToken ct);
    }
}
