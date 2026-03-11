namespace Models.Domain.Patterns.Strategies.Notifications;

public interface INotificationStrategy
{
    Task NotifyAsync(string message, long chatId);
}