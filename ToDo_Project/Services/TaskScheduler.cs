using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DomainTaskFactory = Models.Domain.Patterns.Factories.TaskFactory;
using ToDo_Project.Data;

namespace Services;

public class TaskScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogService _logService;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public TaskScheduler(IServiceProvider serviceProvider, ILogService logService)
    {
        _serviceProvider = serviceProvider;
        _logService = logService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logService.LogInfo("TaskScheduler is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteRemindersAsync();
            }
            catch (Exception ex)
            {
                _logService.LogError("Error during reminder execution.", ex);
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logService.LogInfo("TaskScheduler is stopping.");
    }

    private async Task ExecuteRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var taskFactory = scope.ServiceProvider.GetRequiredService<DomainTaskFactory>();

        var now = DateTime.UtcNow;
        var reminders = await context.Reminders
            .Include(r => r.TaskItem)
            .Where(r => !r.IsSent && r.RemindAt <= now)
            .ToListAsync();

        foreach (var reminder in reminders)
        {
            taskFactory.RestoreBehaviors(reminder.TaskItem);
            reminder.TaskItem.NotifyReminder(reminder);
            reminder.IsSent = true;
        }

        if (reminders.Count > 0)
        {
            await context.SaveChangesAsync();
            _logService.LogInfo($"Processed {reminders.Count} reminder(s).");
        }
    }
}