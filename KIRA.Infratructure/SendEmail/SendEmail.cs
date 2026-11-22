using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace KIRA.Infratructure.SendEmail
{
    public sealed class SendEmail : ISendEmail
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendEmail> _logger;

        public SendEmail(
            IConfiguration configuration,
            ILogger<SendEmail> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var senderEmail = _configuration["EmailSettings:Email"];
            var password = _configuration["EmailSettings:Password"];
            var host = _configuration["EmailSettings:Host"];
            var portValue = _configuration["EmailSettings:Port"];

            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(host) ||
                !int.TryParse(portValue, out var port))
            {
                throw new InvalidOperationException(
                    "Email settings are missing or invalid.");
            }

            try
            {
                using var smtp = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        senderEmail,
                        password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, "KIRA"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(email));

                await smtp.SendMailAsync(
                    message,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Recipient}.",
                    email);

                throw;
            }
        }
    }
}
