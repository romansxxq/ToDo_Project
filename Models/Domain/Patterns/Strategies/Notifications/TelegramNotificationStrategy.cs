using Services;

namespace Models.Domain.Patterns.Strategies.Notifications;

public class TelegramNotificationStrategy : INotificationStrategy
{
    private readonly TelegramBotService _botService;

    public TelegramNotificationStrategy(TelegramBotService botService)
    {
        _botService = botService;
    }

    public async Task NotifyAsync(string message, long chatId)
    {
        await _botService.SendMessageAsync(chatId, message);
    }
}