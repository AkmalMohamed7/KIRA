
namespace KIRA.Application.CQRS.Command.ForgetPassword
{
    /// <summary>
    /// Represents a request to initiate password recovery.
    /// </summary>
    /// <param name="EmailOrPhoneNumber">
    /// The registered email address or phone number.
    /// </param>
    public record ForgetPasswordCommand(string EmailOrPhoneNumber)
        : IRequest<ResultT<bool>>;
}
