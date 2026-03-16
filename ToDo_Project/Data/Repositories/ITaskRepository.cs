using Models.Domain.Entities;
using Models.Domain.Enums;

namespace Data.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllTasksAsync();
    Task<TaskItem?> GetTaskByIdAsync(Guid id);
    Task AddTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(Guid id);
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(TypePriority priority);
}

