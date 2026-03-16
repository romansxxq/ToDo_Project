using Models.Domain.Patterns.Strategies.Notifications;

namespace Services;

public class NotificationService
{
    private readonly IEnumerable<INotificationStrategy> _notificationStrategies;
    private readonly ILogService _logService;

    // This service uses DI to get all registered notification strategies 
    // e.g. EmailStrategy, TelegramStrategy, PushStrategy etc.
    public NotificationService(
        IEnumerable<INotificationStrategy> notificationStrategies, 
        ILogService logService)
    {
        _notificationStrategies = notificationStrategies;
        _logService = logService;
    }

    public async Task SendAsync(Guid taskId, string message, long targetChatId = 0)
    {
        _logService.LogInfo($"Triggering notifications for Task {taskId}...");

        if (targetChatId <= 0)
        {
            _logService.LogWarning($"Telegram chat id is not configured for task {taskId}.");
            return;
        }

        if (!_notificationStrategies.Any())
        {
            _logService.LogWarning("No notification strategies configured.");
            return;
        }

        foreach (var strategy in _notificationStrategies)
        {
            try
            {
                await strategy.NotifyAsync(message, targetChatId);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to send notification via {strategy.GetType().Name}", ex);
            }
        }
    }
}