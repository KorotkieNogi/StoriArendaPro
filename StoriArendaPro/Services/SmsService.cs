// Services/SmsService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace StoriArendaPro.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly TwilioSettings _twilio;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IOptions<TwilioSettings> twilio, ILogger<SmsService> logger)
        {
            _twilio = twilio.Value;
            _logger = logger;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            try
            {
                _logger.LogInformation("Попытка отправки SMS на номер: {PhoneNumber}", toPhoneNumber);
                _logger.LogInformation("Текст сообщения: {Message}", message);
                _logger.LogInformation("Используется номер Twilio: {TwilioNumber}", _twilio.PhoneNumber);

                // Проверяем настройки
                if (string.IsNullOrEmpty(_twilio.AccountSid) ||
                    string.IsNullOrEmpty(_twilio.AuthToken) ||
                    string.IsNullOrEmpty(_twilio.PhoneNumber))
                {
                    _logger.LogError("Не настроены параметры Twilio");
                    throw new Exception("Не настроены параметры Twilio");
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_twilio.PhoneNumber),
                    to: new PhoneNumber(toPhoneNumber)
                );

                _logger.LogInformation("SMS отправлено успешно. SID: {MessageSid}", messageResource.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке SMS на номер {PhoneNumber}", toPhoneNumber);
                throw;
            }
        }
    }

    public class TwilioSettings
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string PhoneNumber { get; set; }
    }
}