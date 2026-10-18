using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Maydan.Application.Interfaces;
using Maydan.Domain.Enums;
using Maydan.Domain.Entities;

namespace Maydan.API.HostedServices;

public class ServiceRequestReminderHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    public ServiceRequestReminderHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var threshold = DateTime.UtcNow.AddHours(-6);
                var toRemind = await uow.ServiceRequests.GetPendingOlderThanAsync(threshold, stoppingToken);

                foreach (var r in toRemind)
                {
                    var association = r.Association ?? await uow.Associations.GetByIdAsync(r.AssociationId, stoppingToken);
                    if (association == null) continue;

                    var assocUsers = await uow.Users.GetByEntityAsync(EntityType.Association, association.Id, null, stoppingToken);
                    var subject = "Reminder: service request still awaiting worker selection";
                    var body = $"Service request (Id: {r.Id}) is still pending worker selection after 6 hours.";


                    if (!string.IsNullOrWhiteSpace(association.ContactEmail))
                    {
                        await emailSender.SendAsync(association.ContactEmail, subject, body, stoppingToken);

                        r.ReminderSent = true;
                    }

                    foreach (var u in assocUsers)
                    {
                        if (!string.IsNullOrWhiteSpace(u.Email))
                        {
                            await emailSender.SendAsync(u.Email, subject, body, stoppingToken);
                        }
                    }

                }

                await uow.SaveChangesAsync(stoppingToken);
            }
            catch
            {
                // ignore
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException) { }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts == null) return;
        _cts.Cancel();
        if (_executingTask != null)
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
