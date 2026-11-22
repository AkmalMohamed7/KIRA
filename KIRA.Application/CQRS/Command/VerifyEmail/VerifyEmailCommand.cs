
namespace KIRA.Application.CQRS.Command.VerifyEmail
{
    /// <summary>
    /// Represents an email verification request.
    /// </summary>
    /// <param name="Email">The registered email address.</param>
    /// <param name="Code">The OTP verification code.</param>
    public record VerifyEmailCommand(
        string Email,
        string Code
    ) : IRequest<ResultT<bool>>;
}
