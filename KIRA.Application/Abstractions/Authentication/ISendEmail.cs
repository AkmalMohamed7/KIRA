namespace KIRA.Application.Abstractions.Authentication
{
    public interface ISendEmail
    {
        Task SendEmailAsync(
         string email,
         string subject,
         string htmlMessage,
         CancellationToken cancellationToken = default);
    }
}
