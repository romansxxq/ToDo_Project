using Models.Domain.Entities;
using Models.Domain.Observers;
using Services;

namespace Models.Domain.Patterns.Observers;

public class NotificationObserver : ITaskObserver
{
    private readonly NotificationService _notificationService;

    public NotificationObserver(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void OnTaskCompleted(TaskItem task)
    {
        _ = _notificationService.SendAsync(task.Id, $"Task '{task.Title}' was completed.");
    }

    public void OnTaskReminder(TaskItem task, Reminder reminder)
    {
        var displayTime = reminder.RemindAt.ToUniversalTime().AddHours(2);
        _ = _notificationService.SendAsync(
            task.Id,
            $"Reminder for task:\nTitle: {task.Title}\nDescription: {task.Description}\nPriority: {task.Priority}\nTime (GMT+2): {displayTime:yyyy-MM-dd HH:mm}",
            task.TelegramChatId);
    }
}