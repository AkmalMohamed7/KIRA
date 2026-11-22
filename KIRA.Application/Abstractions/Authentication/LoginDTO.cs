
namespace KIRA.Application.Abstractions.Authentication
{
    /// <summary>
    /// Contains the authenticated user's information and tokens.
    /// </summary>
    public class LoginDTO
    {
        /// <summary>Unique identifier of the authenticated user.</summary>
        public Guid UserId { get; set; }

        /// <summary>JWT access token used to authorize API requests.</summary>
        public string AcccessToken { get; set; } = string.Empty;

        /// <summary>Refresh token used to generate a new access token.</summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>Authenticated user's phone number.</summary>
        public string phoneNumber { get; set; } = string.Empty;

        /// <summary>Authenticated user's full name.</summary>
        public string FullName { get; set; } = string.Empty;
    }
}
