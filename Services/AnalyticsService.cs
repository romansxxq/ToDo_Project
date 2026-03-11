namespace Services;

public class AnalyticsService
{
    private readonly ILogService _logService;
    // Potentially you could have some database entity to save metrics here
    
    private int _completedTasks;
    private int _overdueTasks;
    private int _reminderCount;

    public AnalyticsService(ILogService logService)
    {
        _logService = logService;
    }

    public void IncrementCompletedTasks(Guid taskId)
    {
        _completedTasks++;
        _logService.LogInfo($"[Analytics] Completed Tasks: {_completedTasks}");
    }

    public void IncrementOverdueTasks(Guid taskId)
    {
        _overdueTasks++;
        _logService.LogInfo($"[Analytics] Overdue Tasks: {_overdueTasks}");
    }

    public void IncrementReminderCount(Guid taskId)
    {
        _reminderCount++;
        _logService.LogInfo($"[Analytics] Reminders Sent: {_reminderCount}");
    }
}