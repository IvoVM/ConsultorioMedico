using ClinicaSaaS.Services;

namespace ClinicaSaaS.Api;

// Once a day, deletes the personal data that only served a request that is over (Ley 25.326, art. 4, inc. 7: data
// no longer needed for its purpose is destroyed): online requests that never became an appointment and closed
// waitlist entries, once PrivacyService.ClosedRequestDays have passed.
public class RetentionSweeper(IServiceScopeFactory scopes, ILogger<RetentionSweeper> log) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var (requests, waitlist) = await scope.ServiceProvider.GetRequiredService<PrivacyService>().PurgeExpiredAsync(stoppingToken);
                if (requests + waitlist > 0)
                    log.LogInformation("Deleted {Requests} expired requests and {Waitlist} closed waitlist entries.", requests, waitlist);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogWarning(ex, "Could not delete expired requests.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
