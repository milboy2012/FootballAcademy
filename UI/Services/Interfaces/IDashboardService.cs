using UI.Models.ViewModels.Dashboard;

namespace UI.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetAsync(CancellationToken ct);
    }
}
