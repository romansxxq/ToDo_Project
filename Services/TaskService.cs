using Models.Domain.Entities;
using Models.Domain.Enums;
using Data.Repositories;
using DomainTaskFactory = Models.Domain.Patterns.Factories.TaskFactory;
using DomainTaskStatus = Models.Domain.Enums.TaskStatus;

namespace Services;

public class TaskService
{
    private readonly ITaskRepository _repository;
    private readonly DomainTaskFactory _taskFactory;

    public TaskService(ITaskRepository repository, DomainTaskFactory taskFactory)
    {
        _repository = repository;
        _taskFactory = taskFactory;
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
    {
        var tasks = await _repository.GetAllTasksAsync();

        foreach (var task in tasks)
        {
            _taskFactory.RestoreBehaviors(task);
        }

        return tasks;
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid taskId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);

        if (task != null)
        {
            _taskFactory.RestoreBehaviors(task);
        }

        return task;
    }

    public async Task<TaskItem> CreateTaskAsync(string title, string description, DateTime dueDate, TypePriority priority, RepetitionType repetition, long telegramChatId)
    {
        var task = _taskFactory.CreateTask(title, description, dueDate, priority, repetition, telegramChatId);

        ScheduleReminder(task);

        await _repository.AddTaskAsync(task);
        return task;
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        _taskFactory.RestoreBehaviors(task);
        EnsureUpcomingReminder(task);
        await _repository.UpdateTaskAsync(task);
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        await _repository.DeleteTaskAsync(taskId);
    }

    public async Task CompleteTaskAsync(Guid taskId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null) return;
        
        _taskFactory.RestoreBehaviors(task);

        task.Complete();
        EnsureUpcomingReminder(task);

        await _repository.UpdateTaskAsync(task);
    }

    private static void ScheduleReminder(TaskItem task)
    {
        task.Reminders.Add(new Reminder
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            RemindAt = task.DueDate.AddHours(-1),
            IsSent = false
        });
    }

    private static void EnsureUpcomingReminder(TaskItem task)
    {
        var reminderTime = task.DueDate.AddHours(-1);
        var hasReminder = task.Reminders.Any(r => !r.IsSent && r.RemindAt == reminderTime);

        if (!hasReminder && task.Status != DomainTaskStatus.Completed)
        {
            ScheduleReminder(task);
        }
    }
}
