using Models.Domain.Entities;
using Models.Domain.Enums;
using Models.Domain.Observers;
using Models.Domain.Patterns.Strategies;
using Models.Domain.Patterns.Strategies.Notifications;

namespace Models.Domain.Patterns.Factories;

public class TaskFactory
{
    private readonly IEnumerable<ITaskObserver> _observers;
    private readonly IEnumerable<INotificationStrategy> _notificationStrategies;

    public TaskFactory(
        IEnumerable<ITaskObserver> observers,
        IEnumerable<INotificationStrategy> notificationStrategies)
    {
        _observers = observers;
        _notificationStrategies = notificationStrategies;
    }

    public TaskItem CreateTask(
        string title,
        string description,
        DateTime dueDate,
        TypePriority priority,
        RepetitionType repetitionType,
        long telegramChatId)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueDate = dueDate,
            Priority = priority,
            RepetitionType = repetitionType,
            TelegramChatId = telegramChatId
        };
        
        RestoreBehaviors(task);
        return task;
    }
    
    public void RestoreBehaviors(TaskItem task)
    {
        if (task == null) return;
        
        task.RepetitionStrategy = CreateRepetitionStrategy(task.RepetitionType);
        task.NotificationStrategies = _notificationStrategies.ToList();
        
        foreach (var observer in _observers)
        {
            task.Subscribe(observer);
        }
    }
    private IRepetitionStrategy CreateRepetitionStrategy(RepetitionType type)
    {
        return type switch
        {
            RepetitionType.None => new NoRepetitionStrategy(),
            RepetitionType.Daily => new DailyRepetitionStrategy(),
            RepetitionType.Weekly => new WeeklyRepetitionStrategy(),
            RepetitionType.Monthly => new MonthlyRepetitionStrategy(),
            _ => new NoRepetitionStrategy()
        };
    }
}