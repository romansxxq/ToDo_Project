using Models.Domain.Entities;

namespace Models.Domain.Observers;

public interface ITaskObserver
{
    void OnTaskReminder(TaskItem task, Reminder reminder);
    void OnTaskCompleted(TaskItem task);
}