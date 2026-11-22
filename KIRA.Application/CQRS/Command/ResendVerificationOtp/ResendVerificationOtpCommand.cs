
namespace KIRA.Application.CQRS.Command.ResendVerificationOtp
{
    /// <summary>
    /// Represents a request to resend an account verification OTP.
    /// </summary>
    /// <param name="EmailOrPhoneNumber">
    /// The registered email address or phone number.
    /// </param>
    public sealed record ResendVerificationOtpCommand(
        string EmailOrPhoneNumber
    ) : IRequest<ResultT<bool>>;
}
