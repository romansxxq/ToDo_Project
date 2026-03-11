using Models.Domain.Entities;
using Models.Domain.Observers;
using Services;

namespace Models.Domain.Patterns.Observers;

public class LoggerObserver : ITaskObserver
{
    private readonly ILogService _logService;

    public LoggerObserver(ILogService logService)
    {
        _logService = logService;
    }

    public void OnTaskCompleted(TaskItem task)
    {
        _logService.LogInfo($"Task {task.Id} completed at {DateTime.UtcNow}");
    }

    public void OnTaskReminder(TaskItem task, Reminder reminder)
    {
        _logService.LogInfo($"Reminder sent for Task {task.Id} at {DateTime.UtcNow}");
    }
}
