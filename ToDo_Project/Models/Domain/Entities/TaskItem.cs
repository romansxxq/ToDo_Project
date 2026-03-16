
using System.ComponentModel.DataAnnotations.Schema;
using Models.Domain.Enums;
using Models.Domain.Observers;
using Models.Domain.Patterns.Strategies;
using Models.Domain.Patterns.Strategies.Notifications;
using TaskStatus = Models.Domain.Enums.TaskStatus;

namespace Models.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public TypePriority Priority { get; set; }
    public TaskStatus Status { get; private set; } = TaskStatus.Pending; 
    public RepetitionType RepetitionType { get; set; } = RepetitionType.None;

    public long TelegramChatId { get; set; }

    public List<Reminder> Reminders { get; set; } = new();
    
    [NotMapped]
    public IRepetitionStrategy? RepetitionStrategy { get; set; }
    
    [NotMapped]
    public List<INotificationStrategy> NotificationStrategies { get; set; } = new();

    [NotMapped]
    private readonly List<ITaskObserver> _observers = new();

    public void Subscribe(ITaskObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Complete()
    {
        if (Status == TaskStatus.Completed) return;

        foreach (var observer in _observers)
        {
            observer.OnTaskCompleted(this);
        }

        if (RepetitionStrategy != null)
        {
            var nextDate = RepetitionStrategy.GetNextExecutionDate(DueDate);
            if (nextDate.HasValue)
            {
                DueDate = nextDate.Value;
                Status = TaskStatus.Pending; 
                return;
            }
        }
        Status = TaskStatus.Completed;
    }

    public void NotifyReminder(Reminder reminder)
    {
        foreach (var observer in _observers)
        {
            observer.OnTaskReminder(this, reminder);
        }
    }
}