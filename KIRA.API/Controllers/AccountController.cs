
namespace KIRA.API.Controllers
{
    /// <summary>
    /// Provides authentication and account management endpoints.
    /// </summary>
    /// <remarks>
    /// This controller handles user registration, email verification,
    /// login, token refresh, and password recovery operations.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseApiController
    {
        public AccountController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Registers a new KIRA user account.
        /// </summary>
        /// <remarks>
        /// Creates a new user account using the supplied personal
        /// and business information.
        /// 
        /// After successful registration, an OTP verification
        /// process may be required to activate the account.
        /// </remarks>
        /// <param name="register">
        /// User registration information, including personal details,
        /// contact information, interests, and business information.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the request.
        /// </param>
        /// <returns>
        /// The unique identifier (GUID) of the newly registered user.
        /// </returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">Invalid registration data.</response>
        /// <response code="409">An account with the provided information already exists.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<ActionResult<Guid>> Register(
            [FromBody] RegisterCommand register,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(register, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Verifies the user's email address using an OTP code.
        /// </summary>
        /// <param name="command">
        /// Contains the user's email address and verification code.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>
        /// True if the email verification succeeds.
        /// </returns>
        /// <response code="200">Email verified successfully.</response>
        /// <response code="400">Invalid or expired verification code.</response>
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [EnableRateLimiting("VerifyEmailPolicy")]
        public async Task<ActionResult<bool>> VerifyEmail(
            [FromBody] VerifyEmailCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Resends the email or phone verification OTP.
        /// </summary>
        /// <param name="command">
        /// Contains the user's email address or phone number.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>True if the OTP resend operation succeeds.</returns>
        /// <response code="200">Verification OTP sent successfully.</response>
        /// <response code="400">Invalid account information or request.</response>
        /// <response code="429">Too many requests.</response>
        [HttpPost("resend-verification-otp")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("ResendOtpPolicy")]
        public async Task<ActionResult<bool>> ResendVerificationOtp(
            [FromBody] ResendVerificationOtpCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Authenticates a user and generates access and refresh tokens.
        /// </summary>
        /// <param name="loginCommand">
        /// Contains the user's email or phone number, password,
        /// and Remember Me preference.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>
        /// User information together with the generated authentication tokens.
        /// </returns>
        /// <response code="200">Authentication successful.</response>
        /// <response code="400">Invalid login request.</response>
        /// <response code="401">Invalid credentials or account not authorized to log in.</response>
        /// <response code="423">User account is temporarily locked.</response>
        /// <response code="429">Too many login attempts.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<ActionResult<LoginDTO>> Login(
            [FromBody] LoginCommand loginCommand,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(loginCommand, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Generates a new access token using a valid refresh token.
        /// </summary>
        /// <param name="command">
        /// Contains the refresh token issued during login or a previous refresh operation.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>
        /// A new access token and refresh token.
        /// </returns>
        /// <response code="200">Tokens refreshed successfully.</response>
        /// <response code="400">Invalid refresh request.</response>
        /// <response code="401">Refresh token is invalid, expired, or revoked.</response>
        /// <response code="429">Too many refresh requests.</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("RefreshTokenPolicy")]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
            [FromBody] RefreshTokenCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Initiates the password recovery process.
        /// </summary>
        /// <remarks>
        /// Sends a password reset OTP to the user's registered
        /// email address or phone number.
        /// </remarks>
        /// <param name="command">
        /// Contains the user's email address or phone number.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>True if the recovery request is processed successfully.</returns>
        /// <response code="200">Password recovery request processed.</response>
        /// <response code="400">Invalid recovery request.</response>
        /// <response code="429">Too many recovery requests.</response>
        [HttpPost("forget-password")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("ForgetPasswordPolicy")]
        public async Task<ActionResult<bool>> ForgetPassword(
            [FromBody] ForgetPasswordCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Validates the password recovery OTP.
        /// </summary>
        /// <param name="command">
        /// Contains the user's email or phone number and the received OTP code.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>
        /// A password reset token that must be used in the reset-password endpoint.
        /// </returns>
        /// <response code="200">OTP validated successfully.</response>
        /// <response code="400">Invalid OTP request.</response>
        /// <response code="401">OTP is incorrect or expired.</response>
        /// <response code="429">Too many validation attempts.</response>
        [HttpPost("validate-otp")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("ValidateOtpPolicy")]
        public async Task<ActionResult<string>> ValidateOtp(
            [FromBody] ValidateOtpCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Resets the user's password using a valid password reset token.
        /// </summary>
        /// <param name="command">
        /// Contains the user's email or phone number, reset token,
        /// new password, and password confirmation.
        /// </param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>True if the password is reset successfully.</returns>
        /// <response code="200">Password reset successfully.</response>
        /// <response code="400">Invalid password or request data.</response>
        /// <response code="401">Reset token is invalid, expired, or unauthorized.</response>
        /// <response code="429">Too many reset attempts.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [EnableRateLimiting("ResetPasswordPolicy")]
        public async Task<ActionResult<bool>> ResetPassword(
            [FromBody] ResetPasswordCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return ToActionResult(result);
        }
    }
}
