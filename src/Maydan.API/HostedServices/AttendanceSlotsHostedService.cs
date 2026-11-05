using Maydan.Application.Interfaces;

namespace Maydan.API.HostedServices;

public class AttendanceSlotsHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceSlotsHostedService> _logger;

    public AttendanceSlotsHostedService(IServiceScopeFactory scopeFactory, ILogger<AttendanceSlotsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IWorkerAttendanceService>();
                await service.ProcessClosedSlotsAsync(stoppingToken);
                await service.SendUnresolvedRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance slots job failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}