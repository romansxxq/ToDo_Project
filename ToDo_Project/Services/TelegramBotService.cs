using Telegram.Bot;

namespace Services;

public class TelegramBotService
{
    private readonly TelegramBotClient _botClient;

    public TelegramBotService(string botToken)
    {
        _botClient = new TelegramBotClient(botToken);
    }

    public async Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        if (chatId <= 0 || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await _botClient.SendMessage(chatId, message, cancellationToken: cancellationToken);
    }
}