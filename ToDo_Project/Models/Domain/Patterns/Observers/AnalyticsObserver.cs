using Models.Domain.Entities;
using Models.Domain.Observers;
using Services;

namespace Models.Domain.Patterns.Observers;

public class AnalyticsObserver : ITaskObserver
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsObserver(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public void OnTaskCompleted(TaskItem task)
    {
        _analyticsService.IncrementCompletedTasks(task.Id);
    }

    public void OnTaskReminder(TaskItem task, Reminder reminder)
    {
        _analyticsService.IncrementReminderCount(task.Id);
    }
}