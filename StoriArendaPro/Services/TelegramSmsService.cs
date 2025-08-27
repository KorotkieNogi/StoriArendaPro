// Services/TelegramSmsService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace StoriArendaPro.Services
{
    public interface ITelegramSmsService : ISmsService
    {
    }

    public class TelegramSmsService : ITelegramSmsService
    {
        private readonly TelegramSettings _telegramSettings;
        private readonly ILogger<TelegramSmsService> _logger;
        private readonly HttpClient _httpClient;

        public TelegramSmsService(IOptions<TelegramSettings> telegramSettings,
                                ILogger<TelegramSmsService> logger,
                                HttpClient httpClient)
        {
            _telegramSettings = telegramSettings.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            try
            {
                _logger.LogInformation("Отправка Telegram сообщения для номера: {PhoneNumber}", toPhoneNumber);

                if (string.IsNullOrEmpty(_telegramSettings.BotToken) ||
                    string.IsNullOrEmpty(_telegramSettings.ChatId))
                {
                    _logger.LogError("Не настроены параметры Telegram");
                    throw new Exception("Не настроены параметры Telegram");
                }

                // Извлекаем только код из сообщения
                var code = message.Replace("Ваш код подтверждения для СтройАренда+: ", "").Trim();

                var url = $"https://api.telegram.org/bot{_telegramSettings.BotToken}/sendMessage";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("chat_id", _telegramSettings.ChatId),
                    new KeyValuePair<string, string>("text",
                        $"🔐 *Код подтверждения СтройАренда+*\n\n" +
                        $"📞 Номер: `{toPhoneNumber}`\n" +
                        $"🔢 Код: *{code}*\n\n" +
                        $"⏰ Действует 10 минут\n" +
                        $"💡 Сообщение: {message}"),
                    new KeyValuePair<string, string>("parse_mode", "Markdown")
                });

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Telegram сообщение отправлено успешно: {Response}", responseContent);
                }
                else
                {
                    _logger.LogError("Ошибка отправки Telegram: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new Exception($"Ошибка Telegram API: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке Telegram сообщения");
                throw;
            }
        }
    }

    public class TelegramSettings
    {
        public string BotToken { get; set; }
        public string ChatId { get; set; }
    }
}