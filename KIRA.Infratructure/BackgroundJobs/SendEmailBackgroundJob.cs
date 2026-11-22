using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KIRA.Infratructure.BackgroundJobs
{
    public class SendEmailBackgroundJob
    {
        private readonly ISendEmail _sendEmail;
        private readonly IDataProtector _dataProtector;
        private readonly ILogger<SendEmailBackgroundJob> _logger;

        public SendEmailBackgroundJob(
            ISendEmail sendEmail,
            IDataProtector dataProtector,
            ILogger<SendEmailBackgroundJob> logger)
        {
            _sendEmail = sendEmail;
            _dataProtector = dataProtector;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 5)]
        public async Task ExecuteAsync(string encryptedPayload, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
               "Starting email background job.");

            var jsonPayload = _dataProtector.Unprotect(
             encryptedPayload);

            var payload = JsonSerializer.Deserialize<EmailJobPayload>(
              jsonPayload);

            if (payload is null ||
                string.IsNullOrWhiteSpace(payload.Recipient) ||
                string.IsNullOrWhiteSpace(payload.Subject))
            {
                throw new InvalidOperationException(
                    "Invalid email job payload.");
            }

            await _sendEmail.SendEmailAsync(
               payload.Recipient,
               payload.Subject,
               payload.Body,
               cancellationToken);

            _logger.LogInformation(
               "Email sent successfully to {Recipient}",
                payload.Recipient);
        }
    }
}
