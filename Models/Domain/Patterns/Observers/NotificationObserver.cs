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
        _ = _notificationService.SendAsync(task.Id, $"Task '{task.Title}' was completed.", task.TelegramChatId);
    }

    public void OnTaskReminder(TaskItem task, Reminder reminder)
    {
        _ = _notificationService.SendAsync(
            task.Id,
            $"Reminder for task: {task.Title}. Time: {reminder.RemindAt:yyyy-MM-dd HH:mm}",
            task.TelegramChatId);
    }
}