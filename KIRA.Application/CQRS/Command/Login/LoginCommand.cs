
namespace KIRA.Application.CQRS.Command.Login
{
    /// <summary>
    /// Represents a user authentication request.
    /// </summary>
    /// <param name="EmailOrPhoneNumber">
    /// The user's registered email or phone number.
    /// </param>
    /// <param name="Password">The user's account password.</param>
    /// <param name="RememberMe">
    /// Determines whether the user wants an extended login session.
    /// </param>
    public record LoginCommand(
        string EmailOrPhoneNumber,
        string Password,
        bool RememberMe
    ) : IRequest<ResultT<LoginDTO>>;
}
