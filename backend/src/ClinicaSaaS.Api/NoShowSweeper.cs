using ClinicaSaaS.Services;

namespace ClinicaSaaS.Api;

// Every few minutes, records as absent the bookings nobody attended, so the agenda, the home board and the history agree.
public class NoShowSweeper(IServiceScopeFactory scopes, ILogger<NoShowSweeper> log) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var marked = await scope.ServiceProvider.GetRequiredService<ScheduleService>().MarkOverdueNoShowsAsync(stoppingToken);
                if (marked > 0)
                    log.LogInformation("Marked {Count} unattended appointments as absent.", marked);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogWarning(ex, "Could not mark unattended appointments as absent.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
