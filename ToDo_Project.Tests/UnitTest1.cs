using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Data.Repositories;
using Models.Domain.Entities;
using Models.Domain.Enums;
using Models.Domain.Observers;
using DomainTaskFactory = Models.Domain.Patterns.Factories.TaskFactory;
using Models.Domain.Patterns.Observers;
using Models.Domain.Patterns.Strategies;
using Models.Domain.Patterns.Strategies.Notifications;
using Services;
using ToDo_Project.Controllers;
using ToDo_Project.Data;
using ToDo_Project.Models.ViewModels;
using DomainTaskStatus = Models.Domain.Enums.TaskStatus;

namespace ToDo_Project.Tests;

public class RepetitionStrategyTests
{
    [Fact]
    public void DailyRepetitionStrategy_GetNextExecutionDate_AddsOneDay()
    {
        var strategy = new DailyRepetitionStrategy();
        var current = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc);

        var next = strategy.GetNextExecutionDate(current);

        Assert.Equal(current.AddDays(1), next);
    }

    [Fact]
    public void WeeklyRepetitionStrategy_GetNextExecutionDate_AddsSevenDays()
    {
        var strategy = new WeeklyRepetitionStrategy();
        var current = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc);

        var next = strategy.GetNextExecutionDate(current);

        Assert.Equal(current.AddDays(7), next);
    }

    [Fact]
    public void MonthlyRepetitionStrategy_GetNextExecutionDate_AddsOneMonth()
    {
        var strategy = new MonthlyRepetitionStrategy();
        var current = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc);

        var next = strategy.GetNextExecutionDate(current);

        Assert.Equal(current.AddMonths(1), next);
    }

    [Fact]
    public void NoRepetitionStrategy_GetNextExecutionDate_ReturnsNull()
    {
        var strategy = new NoRepetitionStrategy();
        var current = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc);

        var next = strategy.GetNextExecutionDate(current);

        Assert.Null(next);
    }
}

public class TaskFactoryTests
{
    [Fact]
    public void CreateTask_SetsFieldsAndInitialStatusPending()
    {
        var factory = CreateFactory();
        var dueDate = new DateTime(2026, 3, 14, 9, 0, 0, DateTimeKind.Utc);

        var task = factory.CreateTask("Title", "Desc", dueDate, TypePriority.High, RepetitionType.Daily, 42);

        Assert.Equal("Title", task.Title);
        Assert.Equal("Desc", task.Description);
        Assert.Equal(dueDate, task.DueDate);
        Assert.Equal(TypePriority.High, task.Priority);
        Assert.Equal(RepetitionType.Daily, task.RepetitionType);
        Assert.Equal(42, task.TelegramChatId);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    [Fact]
    public void RestoreBehaviors_AssignsNotificationStrategies()
    {
        var strategy = new Mock<INotificationStrategy>().Object;
        var factory = CreateFactory(notificationStrategies: new[] { strategy });
        var task = new TaskItem { RepetitionType = RepetitionType.None };

        factory.RestoreBehaviors(task);

        Assert.Single(task.NotificationStrategies);
        Assert.Same(strategy, task.NotificationStrategies[0]);
    }

    [Fact]
    public void RestoreBehaviors_SubscribesObservers()
    {
        var observer = new Mock<ITaskObserver>();
        var factory = CreateFactory(observers: new[] { observer.Object });
        var task = new TaskItem { RepetitionType = RepetitionType.None, DueDate = DateTime.UtcNow };

        factory.RestoreBehaviors(task);
        task.Complete();

        observer.Verify(o => o.OnTaskCompleted(task), Times.Once);
    }

    [Theory]
    [InlineData(RepetitionType.None, typeof(NoRepetitionStrategy))]
    [InlineData(RepetitionType.Daily, typeof(DailyRepetitionStrategy))]
    [InlineData(RepetitionType.Weekly, typeof(WeeklyRepetitionStrategy))]
    [InlineData(RepetitionType.Monthly, typeof(MonthlyRepetitionStrategy))]
    public void RestoreBehaviors_AssignsRepetitionStrategyPerType(RepetitionType type, Type expected)
    {
        var factory = CreateFactory();
        var task = new TaskItem { RepetitionType = type };

        factory.RestoreBehaviors(task);

        Assert.NotNull(task.RepetitionStrategy);
        Assert.IsType(expected, task.RepetitionStrategy);
    }

    [Fact]
    public void RestoreBehaviors_NullTask_DoesNotThrow()
    {
        var factory = CreateFactory();
        TaskItem? task = null;

        var ex = Record.Exception(() => factory.RestoreBehaviors(task!));

        Assert.Null(ex);
    }

    private static DomainTaskFactory CreateFactory(
        IEnumerable<ITaskObserver>? observers = null,
        IEnumerable<INotificationStrategy>? notificationStrategies = null)
    {
        return new DomainTaskFactory(
            observers ?? Array.Empty<ITaskObserver>(),
            notificationStrategies ?? Array.Empty<INotificationStrategy>());
    }
}

public class TaskItemTests
{
    [Fact]
    public void Subscribe_DuplicateObserver_DoesNotNotifyTwice()
    {
        var observer = new Mock<ITaskObserver>();
        var task = new TaskItem { DueDate = DateTime.UtcNow };

        task.Subscribe(observer.Object);
        task.Subscribe(observer.Object);
        task.Complete();

        observer.Verify(o => o.OnTaskCompleted(task), Times.Once);
    }

    [Fact]
    public void Complete_NoRepetition_MarksCompletedAndNotifiesObservers()
    {
        var observer = new Mock<ITaskObserver>();
        var task = new TaskItem { DueDate = DateTime.UtcNow };
        task.Subscribe(observer.Object);

        task.Complete();

        Assert.Equal(DomainTaskStatus.Completed, task.Status);
        observer.Verify(o => o.OnTaskCompleted(task), Times.Once);
    }

    [Fact]
    public void Complete_AlreadyCompleted_DoesNotNotifyAgain()
    {
        var observer = new Mock<ITaskObserver>();
        var task = new TaskItem { DueDate = DateTime.UtcNow };
        task.Subscribe(observer.Object);

        task.Complete();
        task.Complete();

        observer.Verify(o => o.OnTaskCompleted(task), Times.Once);
    }

    [Fact]
    public void Complete_WithRepetitionStrategy_ReschedulesAndKeepsPending()
    {
        var dueDate = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc);
        var task = new TaskItem
        {
            DueDate = dueDate,
            RepetitionType = RepetitionType.Daily,
            RepetitionStrategy = new DailyRepetitionStrategy()
        };

        task.Complete();

        Assert.Equal(DomainTaskStatus.Pending, task.Status);
        Assert.Equal(dueDate.AddDays(1), task.DueDate);
    }

    [Fact]
    public void Complete_RepetitionStrategyReturnsNull_MarksCompleted()
    {
        var task = new TaskItem
        {
            DueDate = DateTime.UtcNow,
            RepetitionType = RepetitionType.None,
            RepetitionStrategy = new NoRepetitionStrategy()
        };

        task.Complete();

        Assert.Equal(DomainTaskStatus.Completed, task.Status);
    }

    [Fact]
    public void NotifyReminder_CallsObservers()
    {
        var observer = new Mock<ITaskObserver>();
        var task = new TaskItem { DueDate = DateTime.UtcNow };
        var reminder = new Reminder { RemindAt = DateTime.UtcNow, IsSent = false };
        task.Subscribe(observer.Object);

        task.NotifyReminder(reminder);

        observer.Verify(o => o.OnTaskReminder(task, reminder), Times.Once);
    }
}

public class TaskServiceTests
{
    [Theory]
    [MemberData(nameof(NonUtcDates))]
    public async Task CreateTaskAsync_NonUtcDate_NormalizesToUtc(DateTime input, DateTime expectedUtc)
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());

        var created = await service.CreateTaskAsync("Title", "Desc", input, TypePriority.Low, RepetitionType.None, 1);

        Assert.Equal(DateTimeKind.Utc, created.DueDate.Kind);
        Assert.Equal(expectedUtc, created.DueDate);
    }

    [Fact]
    public async Task CreateTaskAsync_UtcDate_LeavesUnchanged()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var utc = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc);

        var created = await service.CreateTaskAsync("Title", "Desc", utc, TypePriority.Low, RepetitionType.None, 1);

        Assert.Equal(utc, created.DueDate);
    }

    [Fact]
    public async Task CreateTaskAsync_SchedulesReminderOneHourBeforeDueDate()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var due = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);

        var created = await service.CreateTaskAsync("Title", "Desc", due, TypePriority.Low, RepetitionType.None, 1);

        Assert.Single(created.Reminders);
        Assert.Equal(due.AddHours(-1), created.Reminders[0].RemindAt);
        repository.Verify(r => r.AddTaskAsync(created), Times.Once);
    }

    [Fact]
    public async Task GetAllTasksAsync_RestoresBehaviors()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), RepetitionType = RepetitionType.Daily };
        repository.Setup(r => r.GetAllTasksAsync())
            .ReturnsAsync(new[] { task });
        var service = new TaskService(repository.Object, CreateFactory());

        var tasks = (await service.GetAllTasksAsync()).ToList();

        Assert.Single(tasks);
        Assert.IsType<DailyRepetitionStrategy>(tasks[0].RepetitionStrategy);
    }

    [Fact]
    public async Task GetTaskByIdAsync_MissingTask_ReturnsNull()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TaskItem?)null);
        var service = new TaskService(repository.Object, CreateFactory());

        var result = await service.GetTaskByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_FoundTask_RestoresBehaviors()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), RepetitionType = RepetitionType.Weekly };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id))
            .ReturnsAsync(task);
        var service = new TaskService(repository.Object, CreateFactory());

        var result = await service.GetTaskByIdAsync(task.Id);

        Assert.NotNull(result);
        Assert.IsType<WeeklyRepetitionStrategy>(result!.RepetitionStrategy);
    }

    [Fact]
    public async Task UpdateTaskAsync_UnspecifiedDate_NormalizesToUtc()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DueDate = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Unspecified)
        };

        await service.UpdateTaskAsync(task);

        Assert.Equal(DateTimeKind.Utc, task.DueDate.Kind);
        Assert.Equal(new DateTime(2026, 3, 14, 8, 0, 0, DateTimeKind.Utc), task.DueDate);
    }

    [Fact]
    public async Task UpdateTaskAsync_MissingReminder_AddsUpcomingReminder()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DueDate = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc)
        };

        await service.UpdateTaskAsync(task);

        Assert.Single(task.Reminders);
    }

    [Fact]
    public async Task UpdateTaskAsync_ExistingReminder_DoesNotDuplicate()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var due = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DueDate = due,
            Reminders = new List<Reminder>
            {
                new Reminder { RemindAt = due.AddHours(-1), IsSent = false }
            }
        };

        await service.UpdateTaskAsync(task);

        Assert.Single(task.Reminders);
    }

    [Fact]
    public async Task CompleteTaskAsync_MissingTask_DoesNotUpdateRepository()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TaskItem?)null);
        var service = new TaskService(repository.Object, CreateFactory());

        await service.CompleteTaskAsync(Guid.NewGuid());

        repository.Verify(r => r.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTaskAsync_RepetitionTask_ReschedulesAndAddsReminder()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DueDate = new DateTime(2026, 3, 14, 9, 0, 0, DateTimeKind.Utc),
            RepetitionType = RepetitionType.Daily
        };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        repository.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());

        await service.CompleteTaskAsync(task.Id);

        Assert.Equal(DomainTaskStatus.Pending, task.Status);
        Assert.Equal(new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc), task.DueDate);
        Assert.Single(task.Reminders);
        Assert.Equal(task.DueDate.AddHours(-1), task.Reminders[0].RemindAt);
        repository.Verify(r => r.UpdateTaskAsync(task), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_NoRepetition_MarksCompletedWithoutReminder()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DueDate = new DateTime(2026, 3, 14, 9, 0, 0, DateTimeKind.Utc),
            RepetitionType = RepetitionType.None
        };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        repository.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());

        await service.CompleteTaskAsync(task.Id);

        Assert.Equal(DomainTaskStatus.Completed, task.Status);
        Assert.Empty(task.Reminders);
        repository.Verify(r => r.UpdateTaskAsync(task), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_CallsRepository()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var service = new TaskService(repository.Object, CreateFactory());
        var id = Guid.NewGuid();

        await service.DeleteTaskAsync(id);

        repository.Verify(r => r.DeleteTaskAsync(id), Times.Once);
    }

    public static IEnumerable<object[]> NonUtcDates()
    {
        yield return new object[]
        {
            new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2026, 3, 14, 8, 0, 0, DateTimeKind.Utc)
        };
        yield return new object[]
        {
            new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Local),
            new DateTime(2026, 3, 14, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    private static DomainTaskFactory CreateFactory()
    {
        return new DomainTaskFactory(Array.Empty<ITaskObserver>(), Array.Empty<INotificationStrategy>());
    }
}

public class NotificationServiceTests
{
    [Fact]
    public async Task SendAsync_InvalidChatId_LogsWarningAndSkipsNotifications()
    {
        var logService = new Mock<ILogService>();
        var strategy = new Mock<INotificationStrategy>();
        var service = new NotificationService(new[] { strategy.Object }, logService.Object);

        await service.SendAsync(Guid.NewGuid(), "message", 0);

        logService.Verify(l => l.LogWarning(It.Is<string>(m => m.Contains("chat id", StringComparison.OrdinalIgnoreCase))), Times.Once);
        strategy.Verify(s => s.NotifyAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_NoStrategies_LogsWarning()
    {
        var logService = new Mock<ILogService>();
        var service = new NotificationService(Array.Empty<INotificationStrategy>(), logService.Object);

        await service.SendAsync(Guid.NewGuid(), "message", 10);

        logService.Verify(l => l.LogWarning(It.Is<string>(m => m.Contains("No notification strategies", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ValidChatId_CallsAllStrategies()
    {
        var logService = new Mock<ILogService>();
        var strategyA = new Mock<INotificationStrategy>();
        var strategyB = new Mock<INotificationStrategy>();
        var service = new NotificationService(new[] { strategyA.Object, strategyB.Object }, logService.Object);

        await service.SendAsync(Guid.NewGuid(), "message", 10);

        strategyA.Verify(s => s.NotifyAsync("message", 10), Times.Once);
        strategyB.Verify(s => s.NotifyAsync("message", 10), Times.Once);
    }

    [Fact]
    public async Task SendAsync_StrategyThrows_LogsErrorAndContinues()
    {
        var logService = new Mock<ILogService>();
        var failing = new Mock<INotificationStrategy>();
        var succeeding = new Mock<INotificationStrategy>();
        failing.Setup(s => s.NotifyAsync(It.IsAny<string>(), It.IsAny<long>()))
            .ThrowsAsync(new InvalidOperationException("fail"));
        var service = new NotificationService(new[] { failing.Object, succeeding.Object }, logService.Object);

        await service.SendAsync(Guid.NewGuid(), "message", 10);

        logService.Verify(l => l.LogError(It.Is<string>(m => m.Contains("Failed to send notification", StringComparison.OrdinalIgnoreCase)), It.IsAny<Exception>()), Times.Once);
        succeeding.Verify(s => s.NotifyAsync("message", 10), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ValidChatId_LogsStartInfo()
    {
        var logService = new Mock<ILogService>();
        var strategy = new Mock<INotificationStrategy>();
        var service = new NotificationService(new[] { strategy.Object }, logService.Object);

        await service.SendAsync(Guid.NewGuid(), "message", 10);

        logService.Verify(l => l.LogInfo(It.Is<string>(m => m.Contains("Triggering notifications", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }
}

public class AnalyticsServiceTests
{
    [Fact]
    public void IncrementCompletedTasks_IncrementsCountAndLogs()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var service = new AnalyticsService(logService.Object, context);

        service.IncrementCompletedTasks(Guid.NewGuid());

        logService.Verify(l => l.LogInfo("[Analytics] Completed Tasks: 1"), Times.Once);
    }

    [Fact]
    public void IncrementOverdueTasks_IncrementsCountAndLogs()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var service = new AnalyticsService(logService.Object, context);

        service.IncrementOverdueTasks(Guid.NewGuid());

        logService.Verify(l => l.LogInfo("[Analytics] Overdue Tasks: 1"), Times.Once);
    }

    [Fact]
    public void IncrementReminderCount_IncrementsCountAndLogs()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var service = new AnalyticsService(logService.Object, context);

        service.IncrementReminderCount(Guid.NewGuid());

        logService.Verify(l => l.LogInfo("[Analytics] Reminders Sent: 1"), Times.Once);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsAccurateCounts()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;

        var completed = new TaskItem { DueDate = now.AddDays(-1), Priority = TypePriority.Low };
        completed.Complete();
        var overdue = new TaskItem { DueDate = now.AddDays(-1), Priority = TypePriority.Low };
        var pending = new TaskItem { DueDate = now.AddDays(2), Priority = TypePriority.Low };

        context.Tasks.AddRange(completed, overdue, pending);
        await context.SaveChangesAsync();

        var service = new AnalyticsService(logService.Object, context);

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(3, snapshot.Total);
        Assert.Equal(1, snapshot.Completed);
        Assert.Equal(1, snapshot.Overdue);
        Assert.Equal(1, snapshot.Pending);
    }

    [Fact]
    public async Task GetSnapshotAsync_NoTasks_ReturnsZeros()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var service = new AnalyticsService(logService.Object, context);

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(0, snapshot.Total);
        Assert.Equal(0, snapshot.Completed);
        Assert.Equal(0, snapshot.Overdue);
        Assert.Equal(0, snapshot.Pending);
    }
}

public class LogServiceTests
{
    [Fact]
    public void LogInfo_ForwardsMessageToLogger()
    {
        var logger = new Mock<ILogger<LogService>>();
        var service = new LogService(logger.Object);

        service.LogInfo("info-message");

        VerifyLog(logger, LogLevel.Information, "info-message", null);
    }

    [Fact]
    public void LogWarning_ForwardsMessageToLogger()
    {
        var logger = new Mock<ILogger<LogService>>();
        var service = new LogService(logger.Object);

        service.LogWarning("warn-message");

        VerifyLog(logger, LogLevel.Warning, "warn-message", null);
    }

    [Fact]
    public void LogError_WithException_ForwardsException()
    {
        var logger = new Mock<ILogger<LogService>>();
        var service = new LogService(logger.Object);
        var ex = new InvalidOperationException("boom");

        service.LogError("error-message", ex);

        VerifyLog(logger, LogLevel.Error, "error-message", ex);
    }

    [Fact]
    public void LogError_WithoutException_ForwardsMessage()
    {
        var logger = new Mock<ILogger<LogService>>();
        var service = new LogService(logger.Object);

        service.LogError("error-message");

        VerifyLog(logger, LogLevel.Error, "error-message", null);
    }

    private static void VerifyLog(Mock<ILogger<LogService>> logger, LogLevel level, string message, Exception? exception)
    {
        logger.Verify(l => l.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}

public class ObserverTests
{
    [Fact]
    public void AnalyticsObserver_OnTaskCompleted_LogsIncrement()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var analyticsService = new AnalyticsService(logService.Object, context);
        var observer = new AnalyticsObserver(analyticsService);
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };

        observer.OnTaskCompleted(task);

        logService.Verify(l => l.LogInfo("[Analytics] Completed Tasks: 1"), Times.Once);
    }

    [Fact]
    public void AnalyticsObserver_OnTaskReminder_LogsIncrement()
    {
        var logService = new Mock<ILogService>();
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var analyticsService = new AnalyticsService(logService.Object, context);
        var observer = new AnalyticsObserver(analyticsService);
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };
        var reminder = new Reminder { RemindAt = DateTime.UtcNow, IsSent = false };

        observer.OnTaskReminder(task, reminder);

        logService.Verify(l => l.LogInfo("[Analytics] Reminders Sent: 1"), Times.Once);
    }

    [Fact]
    public void LoggerObserver_OnTaskCompleted_LogsInfo()
    {
        var logService = new Mock<ILogService>();
        var observer = new LoggerObserver(logService.Object);
        var task = new TaskItem { Id = Guid.NewGuid() };

        observer.OnTaskCompleted(task);

        logService.Verify(l => l.LogInfo(It.Is<string>(m => m.Contains("completed", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    public void LoggerObserver_OnTaskReminder_LogsInfo()
    {
        var logService = new Mock<ILogService>();
        var observer = new LoggerObserver(logService.Object);
        var task = new TaskItem { Id = Guid.NewGuid() };
        var reminder = new Reminder { RemindAt = DateTime.UtcNow, IsSent = false };

        observer.OnTaskReminder(task, reminder);

        logService.Verify(l => l.LogInfo(It.Is<string>(m => m.Contains("Reminder", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    public async Task NotificationObserver_OnTaskReminder_SendsNotification()
    {
        var logService = new Mock<ILogService>();
        var notification = new Mock<INotificationStrategy>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        notification.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<long>()))
            .Callback(() => tcs.TrySetResult(true))
            .Returns(Task.CompletedTask);

        var notificationService = new NotificationService(new[] { notification.Object }, logService.Object);
        var observer = new NotificationObserver(notificationService);
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Desc",
            Priority = TypePriority.High,
            TelegramChatId = 123
        };
        var reminder = new Reminder { RemindAt = DateTime.UtcNow, IsSent = false };

        observer.OnTaskReminder(task, reminder);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(200));
        Assert.Same(tcs.Task, completed);
        notification.Verify(n => n.NotifyAsync(It.Is<string>(m => m.Contains("Title")), 123), Times.Once);
    }

    [Fact]
    public void NotificationObserver_OnTaskCompleted_LogsWarningBecauseChatIdMissing()
    {
        var logService = new Mock<ILogService>();
        var notification = new Mock<INotificationStrategy>();
        var notificationService = new NotificationService(new[] { notification.Object }, logService.Object);
        var observer = new NotificationObserver(notificationService);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title" };

        observer.OnTaskCompleted(task);

        logService.Verify(l => l.LogWarning(It.Is<string>(m => m.Contains("chat id", StringComparison.OrdinalIgnoreCase))), Times.Once);
        notification.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }
}

public class TaskControllerTests
{
    [Fact]
    public async Task Index_ReturnsOkWithTasks()
    {
        var repository = new Mock<ITaskRepository>();
        var tasks = new[] { new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow } };
        repository.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var controller = new TaskController(CreateService(repository));

        var result = await controller.Index();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsAssignableFrom<IEnumerable<TaskItem>>(ok.Value);
    }

    [Fact]
    public async Task Details_MissingTask_ReturnsNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
        var controller = new TaskController(CreateService(repository));

        var result = await controller.Details(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_FoundTask_ReturnsOk()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        var controller = new TaskController(CreateService(repository));

        var result = await controller.Details(task.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(task, ok.Value);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var controller = new TaskController(CreateService(repository));
        var request = new TaskController.CreateTaskRequest
        {
            Title = "Title",
            Description = "Desc",
            DueDate = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc),
            Priority = TypePriority.Medium,
            RepetitionType = RepetitionType.None,
            TelegramChatId = 22
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TaskController.Details), created.ActionName);
        Assert.IsType<TaskItem>(created.Value);
    }

    [Fact]
    public async Task Complete_ReturnsNoContent()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        repository.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
        var controller = new TaskController(CreateService(repository));

        var result = await controller.Complete(task.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var controller = new TaskController(CreateService(repository));
        var id = Guid.NewGuid();

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        repository.Verify(r => r.DeleteTaskAsync(id), Times.Once);
    }

    private static TaskService CreateService(Mock<ITaskRepository> repository)
    {
        var factory = new DomainTaskFactory(Array.Empty<ITaskObserver>(), Array.Empty<INotificationStrategy>());
        return new TaskService(repository.Object, factory);
    }
}

public class TasksControllerTests
{
    [Fact]
    public async Task Index_ReturnsOrderedTasksByDueDate()
    {
        var repository = new Mock<ITaskRepository>();
        var tasks = new[]
        {
            new TaskItem { Id = Guid.NewGuid(), DueDate = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { Id = Guid.NewGuid(), DueDate = new DateTime(2026, 3, 18, 10, 0, 0, DateTimeKind.Utc) }
        };
        repository.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var controller = CreateController(repository);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(view.Model).ToList();
        Assert.True(model[0].DueDate <= model[1].DueDate);
    }

    [Fact]
    public async Task Details_MissingTask_ReturnsNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
        var controller = CreateController(repository);

        var result = await controller.Details(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_FoundTask_ReturnsView()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        var controller = CreateController(repository);

        var result = await controller.Details(task.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(task, view.Model);
    }

    [Fact]
    public void Create_Get_ReturnsViewWithOptions()
    {
        var controller = CreateController(new Mock<ITaskRepository>());

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TaskFormViewModel>(view.Model);
        Assert.NotEmpty(model.PriorityOptions);
        Assert.NotEmpty(model.RepetitionOptions);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsViewWithOptions()
    {
        var controller = CreateController(new Mock<ITaskRepository>());
        controller.ModelState.AddModelError("Title", "Required");
        var model = new TaskFormViewModel();

        var result = await controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<TaskFormViewModel>(view.Model);
        Assert.NotEmpty(viewModel.PriorityOptions);
        Assert.NotEmpty(viewModel.RepetitionOptions);
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsAndSetsTempData()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
        var controller = CreateController(repository);
        var model = new TaskFormViewModel
        {
            Title = "Title",
            Description = "Desc",
            DueDate = new DateTime(2026, 3, 14, 10, 0, 0, DateTimeKind.Utc),
            Priority = TypePriority.High,
            RepetitionType = RepetitionType.None,
            TelegramChatId = 55
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TasksController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["AlertMessage"]);
    }

    [Fact]
    public async Task Edit_Get_MissingTask_ReturnsNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
        var controller = CreateController(repository);

        var result = await controller.Edit(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_FoundTask_ReturnsViewModel()
    {
        var repository = new Mock<ITaskRepository>();
        var task = new TaskItem { Id = Guid.NewGuid(), DueDate = DateTime.UtcNow };
        repository.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        var controller = CreateController(repository);

        var result = await controller.Edit(task.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<TaskFormViewModel>(view.Model);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsViewWithOptions()
    {
        var controller = CreateController(new Mock<ITaskRepository>());
        controller.ModelState.AddModelError("Title", "Required");
        var model = new TaskFormViewModel();

        var result = await controller.Edit(Guid.NewGuid(), model);

        var view = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<TaskFormViewModel>(view.Model);
        Assert.NotEmpty(viewModel.PriorityOptions);
        Assert.NotEmpty(viewModel.RepetitionOptions);
    }

    [Fact]
    public async Task Edit_Post_MissingTask_ReturnsNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
        var controller = CreateController(repository);
        var model = new TaskFormViewModel
        {
            Title = "NewTitle",
            Description = "NewDesc",
            DueDate = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var result = await controller.Edit(Guid.NewGuid(), model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidModel_RedirectsToDetails()
    {
        var repository = new Mock<ITaskRepository>();
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, DueDate = DateTime.UtcNow };
        repository.Setup(r => r.GetTaskByIdAsync(taskId)).ReturnsAsync(task);
        repository.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
        var controller = CreateController(repository);
        var model = new TaskFormViewModel
        {
            Title = "NewTitle",
            Description = "NewDesc",
            DueDate = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            Priority = TypePriority.Medium,
            RepetitionType = RepetitionType.None,
            TelegramChatId = 99
        };

        var result = await controller.Edit(taskId, model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TasksController.Details), redirect.ActionName);
        repository.Verify(r => r.UpdateTaskAsync(task), Times.Once);
    }

    private static TasksController CreateController(Mock<ITaskRepository> repository)
    {
        var factory = new DomainTaskFactory(Array.Empty<ITaskObserver>(), Array.Empty<INotificationStrategy>());
        var service = new TaskService(repository.Object, factory);
        var controller = new TasksController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }
}

public class TaskRepositoryTests
{
    [Fact]
    public async Task AddTaskAsync_PersistsTask()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title", DueDate = DateTime.UtcNow };

        await repository.AddTaskAsync(task);

        Assert.Single(context.Tasks.Where(t => t.Id == task.Id));
    }

    [Fact]
    public async Task GetAllTasksAsync_IncludesReminders()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title", DueDate = DateTime.UtcNow };
        task.Reminders.Add(new Reminder { RemindAt = DateTime.UtcNow.AddHours(-1) });
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var tasks = (await repository.GetAllTasksAsync()).ToList();

        Assert.Single(tasks);
        Assert.Single(tasks[0].Reminders);
    }

    [Fact]
    public async Task GetTaskByIdAsync_IncludesReminders()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title", DueDate = DateTime.UtcNow };
        task.Reminders.Add(new Reminder { RemindAt = DateTime.UtcNow.AddHours(-1) });
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var loaded = await repository.GetTaskByIdAsync(task.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Reminders);
    }

    [Fact]
    public async Task GetByPriorityAsync_FiltersTasks()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var high = new TaskItem { Id = Guid.NewGuid(), Title = "High", Priority = TypePriority.High, DueDate = DateTime.UtcNow };
        var low = new TaskItem { Id = Guid.NewGuid(), Title = "Low", Priority = TypePriority.Low, DueDate = DateTime.UtcNow };
        context.Tasks.AddRange(high, low);
        await context.SaveChangesAsync();

        var result = (await repository.GetByPriorityAsync(TypePriority.High)).ToList();

        Assert.Single(result);
        Assert.Equal(high.Id, result[0].Id);
    }

    [Fact]
    public async Task UpdateTaskAsync_DetachedEntity_UpdatesRecord()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Old", DueDate = DateTime.UtcNow };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var detached = new TaskItem { Id = task.Id, Title = "New", DueDate = task.DueDate };
        await repository.UpdateTaskAsync(detached);

        Assert.Single(context.Tasks.Where(t => t.Id == task.Id && t.Title == "New"));
    }

    [Fact]
    public async Task DeleteTaskAsync_MissingTask_DoesNotChangeCount()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title", DueDate = DateTime.UtcNow };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        await repository.DeleteTaskAsync(Guid.NewGuid());

        Assert.Single(context.Tasks);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_RemovesRecord()
    {
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Title", DueDate = DateTime.UtcNow };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        await repository.DeleteTaskAsync(task.Id);

        Assert.Empty(context.Tasks);
    }
}

internal static class TestDbContextFactory
{
    public static TodoDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TodoDbContext(options);
    }
}
