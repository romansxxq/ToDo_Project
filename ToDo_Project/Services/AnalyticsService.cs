using Microsoft.EntityFrameworkCore;
using Models.Domain.Enums;
using ToDo_Project.Data;
using DomainTaskStatus = Models.Domain.Enums.TaskStatus;

namespace Services;

public class AnalyticsService
{
    private readonly ILogService _logService;
    private readonly TodoDbContext _context;
    // Potentially you could have some database entity to save metrics here

    private int _completedTasks;
    private int _overdueTasks;
    private int _reminderCount;

    public AnalyticsService(ILogService logService, TodoDbContext context)
    {
        _logService = logService;
        _context = context;
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

    public async Task<AnalyticsSnapshot> GetSnapshotAsync()
    {
        var now = DateTime.UtcNow;
        var total = await _context.Tasks.CountAsync();
        var completed = await _context.Tasks.CountAsync(t => t.Status == DomainTaskStatus.Completed);
        var overdue = await _context.Tasks.CountAsync(t => t.Status != DomainTaskStatus.Completed && t.DueDate < now);
        var pending = total - completed - overdue;

        return new AnalyticsSnapshot(total, completed, pending, overdue);
    }
}

public sealed record AnalyticsSnapshot(int Total, int Completed, int Pending, int Overdue);