using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace SmartBIST.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Demo amaçlı - gerçek email gönderimi yapılmıyor
            _logger.LogInformation("Email gönderimi simüle edildi - Alıcı: {Email}, Konu: {Subject}", email, subject);
            return Task.CompletedTask;
        }
    }
} 