namespace KIRA.Application.CQRS.Command.RefreshToken
{
    /// <summary>
    /// Represents a request to refresh authentication tokens.
    /// </summary>
    /// <param name="RefreshToken">The currently valid refresh token.</param>
    public record RefreshTokenCommand(string RefreshToken)
        : IRequest<ResultT<RefreshTokenResponse>>;
}