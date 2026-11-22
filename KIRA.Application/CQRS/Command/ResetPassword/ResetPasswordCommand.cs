
namespace KIRA.Application.CQRS.Command.ResetPassword
{
    /// <summary>
    /// Represents a request to reset the user's password.
    /// </summary>
    /// <param name="EmailOrPhoneNumber">
    /// The registered email address or phone number.
    /// </param>
    /// <param name="ResetToken">
    /// The token returned after successful OTP validation.
    /// </param>
    /// <param name="NewPassword">The new account password.</param>
    /// <param name="ConfirmPassword">Confirmation of the new password.</param>
    public record ResetPasswordCommand(
        string EmailOrPhoneNumber,
        string ResetToken,
        string NewPassword,
        string ConfirmPassword
    ) : IRequest<ResultT<bool>>;
}
