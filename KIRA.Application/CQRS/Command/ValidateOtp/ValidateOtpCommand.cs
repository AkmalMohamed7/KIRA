
namespace KIRA.Application.CQRS.Command.ValidateOtp
{
    /// <summary>
    /// Represents a request to validate a password recovery OTP.
    /// </summary>
    /// <param name="emailOrPhoneNumber">
    /// The registered email address or phone number.
    /// </param>
    /// <param name="code">The received OTP code.</param>
    public record ValidateOtpCommand(
        string emailOrPhoneNumber,
        string code
    ) : IRequest<ResultT<string>>;
}
