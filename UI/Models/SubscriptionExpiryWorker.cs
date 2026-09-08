using UI.Services.Interfaces;

namespace UI.Models
{
    public class SubscriptionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        public SubscriptionExpiryWorker(IServiceScopeFactory scopes) => _scopes = scopes;
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using (var scope = _scopes.CreateScope())
                    await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().ExpireOutdatedAsync(ct);
                await Task.Delay(TimeSpan.FromHours(1), ct);
            }
        }
    }
}
