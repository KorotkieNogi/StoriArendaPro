// Services/EmailSmsService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace StoriArendaPro.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }

    public class EmailSmsService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailSmsService> _logger;

        public EmailSmsService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailSmsService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogError("Email адрес получателя не указан");
                    throw new ArgumentException("Email адрес получателя не может быть пустым");
                }

                using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000 // 30 секунд таймаут
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromAddress, _smtpSettings.FromName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(toEmail));

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email отправлен на: {Email}", toEmail);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Ошибка валидации email адреса");
                throw;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP ошибка: {Status}", ex.StatusCode);
                throw new Exception($"Ошибка SMTP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email на {Email}", toEmail);
                throw new Exception($"Не удалось отправить email: {ex.Message}", ex);
            }
        }
    }

    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FromName { get; set; }
        public string FromAddress { get; set; }
    }
}