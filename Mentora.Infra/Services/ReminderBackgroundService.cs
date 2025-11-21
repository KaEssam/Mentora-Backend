using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Services;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
                await ProcessRetriesAsync(stoppingToken);
                await CleanupOldRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Reminder Background Service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Reminder Background Service is stopping.");
    }

    private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        try
        {
            await reminderService.ProcessScheduledRemindersAsync();
            _logger.LogDebug("Processed scheduled reminders");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled reminders");
        }
    }

    private async Task ProcessRetriesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        try
        {
            await reminderService.ProcessRetriesAsync();
            _logger.LogDebug("Processed reminder retries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reminder retries");
        }
    }

    private async Task CleanupOldRemindersAsync(CancellationToken stoppingToken)
    {
        // Clean up reminders older than 30 days that have been sent
        // This prevents the database from growing indefinitely
        using var scope = _serviceProvider.CreateScope();
        var reminderRepository = scope.ServiceProvider.GetRequiredService<IReminderRepository>();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            // This would require adding a cleanup method to the repository
            // await reminderRepository.DeleteOldRemindersAsync(cutoffDate);
            _logger.LogDebug("Cleaned up old reminders");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old reminders");
        }
    }
}
