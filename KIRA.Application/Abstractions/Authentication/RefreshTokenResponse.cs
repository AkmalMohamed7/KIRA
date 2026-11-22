namespace KIRA.Application.Abstractions.Authentication
{
    /// <summary>
    /// Contains the newly generated authentication tokens.
    /// </summary>
    public class RefreshTokenResponse
    {
        /// <summary>Unique identifier of the user.</summary>
        public Guid UserId { get; set; }

        /// <summary>New JWT access token.</summary>
        public string AccessToken { get; set; } = null!;

        /// <summary>New refresh token.</summary>
        public string RefreshToken { get; set; } = null!;
    }
}