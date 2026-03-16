using Microsoft.EntityFrameworkCore;
using Models.Domain.Entities;
using Models.Domain.Enums;
using ToDo_Project.Data;

namespace Data.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TodoDbContext _context;

    public TaskRepository(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .Include(t => t.Reminders)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Reminders)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        var tracked = _context.Tasks.Local.FirstOrDefault(t => t.Id == task.Id);
        if (tracked != null && tracked != task)
        {
            _context.Entry(tracked).State = EntityState.Detached;
        }
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var task = await GetTaskByIdAsync(id);
        if (task == null)
        {
            return;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByPriorityAsync(TypePriority priority)
    {
        return await _context.Tasks
            .Include(t => t.Reminders)
            .Where(t => t.Priority == priority)
            .ToListAsync();
    }
}