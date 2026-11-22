using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace KIRA.Infratructure.BackgroundJobs
{
    public class HangfireEmailJobScheduler : IEmailJobScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IDataProtector _dataProtector;

        public HangfireEmailJobScheduler(
            IBackgroundJobClient backgroundJobClient,
            IDataProtector dataProtector)
        {
            _backgroundJobClient = backgroundJobClient;
            _dataProtector = dataProtector;
        }

        public string EnqueueEmail(string recipient, string subject, string body)
        {
            var payload = new EmailJobPayload(
              recipient,
              subject,
              body);

            var jsonPayload = JsonSerializer.Serialize(payload);

            var encryptedPayload = _dataProtector.Protect(jsonPayload);

            return _backgroundJobClient.Enqueue<SendEmailBackgroundJob>(
                job => job.ExecuteAsync(
                    encryptedPayload,
                    CancellationToken.None));
        }
    }
}
