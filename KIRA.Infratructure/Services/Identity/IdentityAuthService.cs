using Microsoft.Extensions.Options;

namespace KIRA.Infratructure.Services.Identity
{
    public class IdentityAuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpHasher _otpHasher;
        private readonly IOtpStorageService _cacheOtp;
        private readonly IEmailJobScheduler _emailJobScheduler;
        private readonly ILogger<IdentityAuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRotationService _refreshToken;
        private readonly JwtSettings _options;


        public IdentityAuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork,
            IOtpHasher otpHasher,
            IOtpStorageService redisOtp,
            IEmailJobScheduler emailJobScheduler,
            ILogger<IdentityAuthService> logger,
            ITokenService tokenService,
            IRefreshTokenRotationService refreshToken,
            IOptions<JwtSettings> options
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _otpHasher = otpHasher;
            _cacheOtp = redisOtp;
            _emailJobScheduler = emailJobScheduler;
            _logger = logger;
            _tokenService = tokenService;
            _refreshToken = refreshToken;
            _options = options.Value;

        }

        private async Task<ApplicationUser?> FindUserAsync(
           string emailOrPhoneNumber,
           CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(emailOrPhoneNumber))
                return null;

            emailOrPhoneNumber = emailOrPhoneNumber.Trim();

            if (emailOrPhoneNumber.Contains('@'))
            {
                return await _userManager.FindByEmailAsync(
                    emailOrPhoneNumber);
            }

            return await _userManager.Users.FirstOrDefaultAsync(
                u => u.PhoneNumber == emailOrPhoneNumber,
                cancellationToken);
        }

        private static ResultT<bool> InvalidResetRequest()
        {
            return ResultT<bool>.Failure(
                new Error(
                    "ResetPassword.InvalidRequest",
                    "Invalid or expired password reset request.",
                    ErrorType.BadRequest));
        }
        public async Task<ResultT<Guid>> RegisterAsync(IAuthService.RegisterIdentityRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                string.IsNullOrWhiteSpace(request.Industry) ||
                string.IsNullOrWhiteSpace(request.BusinessDomain) ||
                string.IsNullOrWhiteSpace(request.JobTitle) ||
                string.IsNullOrWhiteSpace(request.ComapnyName)
                )
            {
                return ResultT<Guid>.Failure(
                    new Error(
                        "Register.InvalidRequest",
                        "Required fields are missing.",
                        ErrorType.BadRequest));
            }

            var email = request.Email.Trim();
            var phoneNumber = request.PhoneNumber.Trim();

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                return ResultT<Guid>.Failure(
                    new Error(
                        "Register.Conflict",
                        "Email already exists.",
                        ErrorType.Conflict));
            }

            var existingPhone = await _userManager.Users.AnyAsync(
                u => u.PhoneNumber == phoneNumber,
                cancellationToken);

            if (existingPhone)
            {
                return ResultT<Guid>.Failure(
                    new Error(
                        "Register.PhoneConflict",
                        "Phone number already exists.",
                        ErrorType.Conflict));
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                PhoneNumber = phoneNumber,
                FullName = request.FullName.Trim(),
                Industry = request.Industry.Trim(),
                BusniessDomain = request.BusinessDomain.Trim(),
                JobTitle = request.JobTitle.Trim(),
                CompanyName = request.ComapnyName.Trim(),
                Interests = request.Interests,
                BirthDay = request.BirthDate
            };

            var result = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                return ResultT<Guid>.Failure(
                    new Error(
                        "Register.CreateAccount",
                        errors,
                        ErrorType.BadRequest));
            }

            try
            {
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    Roles.Client);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        roleResult.Errors.Select(x => x.Description));

                    await _userManager.DeleteAsync(user);

                    return ResultT<Guid>.Failure(
                        new Error(
                            "Register.AssignRole",
                            errors,
                            ErrorType.Failure));
                }

                var otpCode = RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

                var otpHash = _otpHasher.Hash(user.Id, otpCode);

                var canSend = await _cacheOtp.TryStoreOtpAsync(
                    user.Id,
                    "EmailConfirmation",
                    otpHash,
                    TimeSpan.FromMinutes(5),
                    maxRequests: 3,
                    requestWindow: TimeSpan.FromMinutes(15),
                    cooldown: TimeSpan.FromSeconds(60),
                    cancellationToken);

                if (!canSend)
                {
                    await _userManager.DeleteAsync(user);

                    return ResultT<Guid>.Failure(
                        new Error(
                            "OTP.RateLimit",
                            "Please wait before requesting another verification code.",
                            ErrorType.BadRequest));
                }

                _emailJobScheduler.EnqueueEmail(
                   user.Email!,
                   "KIRA - Email Verification",
                   $"Your verification code is: <h2>{otpCode}</h2>" +
                   "<p>This code expires in 5 minutes.</p>");

                return ResultT<Guid>.Success(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Registration failed after creating user {UserId}.",
                    user.Id);

                try
                {
                    await _cacheOtp.DeleteOtpAsync(
                        user.Id,
                        "EmailConfirmation",
                        cancellationToken);

                    await _userManager.DeleteAsync(user);
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Failed to cleanup registration for user {UserId}.",
                        user.Id);
                }

                return ResultT<Guid>.Failure(
                    new Error(
                        "Register.Failed",
                        "Registration could not be completed. Please try again.",
                        ErrorType.Failure));
            }
        }


        public async Task<ResultT<bool>> VerifyEmailAsync(
          string email,
          string code,
          CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code) ||
                code.Length != 6 ||
                !code.All(char.IsAsciiDigit))
            {
                return ResultT<bool>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());

            if (user is null || user.EmailConfirmed)
            {
                return ResultT<bool>.Failure(
                    new Error(
                        "EmailVerification.Invalid",
                        "Invalid verification request.",
                        ErrorType.BadRequest));
            }

            var otpHash = _otpHasher.Hash(
                user.Id,
                code);

            var isValid = await _cacheOtp.VerifyAndConsumeOtpAsync(
                user.Id,
                "EmailConfirmation",
                otpHash,
                maxAttempts: 5,
                cancellationToken);

            if (!isValid)
            {
                return ResultT<bool>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError(
                    "Failed to confirm email for user {UserId} after OTP consumption.",
                    user.Id);

                return ResultT<bool>.Failure(
                    new Error(
                        "EmailVerification.Failed",
                        "Could not verify email. Please request a new verification code.",
                        ErrorType.Failure));
            }

            return ResultT<bool>.Success(true);
        }


        private async Task ReleaseReservationSafelyAsync(
          Guid userId,
          string secretHash,
          string operationId,
          CancellationToken cancellationToken)
        {
            try
            {
                await _cacheOtp.ReleaseSecretReservationAsync(
                    userId,
                    "PasswordResetToken",
                    secretHash,
                    operationId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to release password reset reservation for user {UserId}.",
                    userId);
            }
        }

        public async Task<ResultT<bool>> ResendVerificationOtpAsync(
            string emailOrPhoneNumber,
            CancellationToken cancellationToken = default)
        {

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var user = await FindUserAsync(
                    emailOrPhoneNumber,
                    cancellationToken);

                if (user is null || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
                    return ResultT<bool>.Success(true);

                var otp = RandomNumberGenerator
                    .GetInt32(0, 1_000_000)
                    .ToString("D6");

                var otpHash = _otpHasher.Hash(user.Id, otp);

                var stored = await _cacheOtp.TryStoreOtpAsync(
                    user.Id,
                    "EmailConfirmation",
                    otpHash,
                    expiration: TimeSpan.FromMinutes(5),
                    maxRequests: 3,
                    requestWindow: TimeSpan.FromMinutes(15),
                    cooldown: TimeSpan.FromSeconds(60),
                    cancellationToken);

                if (!stored)
                    return ResultT<bool>.Success(true);

                var emailBody = $"""
            <html>
            <body>
                <h2>Verify Your Email</h2>
                <p>Your verification code is:</p>
                <h1>{otp}</h1>
                <p>This code expires in 5 minutes.</p>
                <p>If you did not request this code, please ignore this email.</p>
            </body>
            </html>
            """;

                try
                {
                    _emailJobScheduler.EnqueueEmail(
                        user.Email,
                        "KIRA - Email Verification",
                        emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send verification OTP to user {UserId}",
                        user.Id);

                    await _cacheOtp.DeleteOtpAsync(
                        user.Id,
                        "EmailConfirmation",
                        cancellationToken);

                    return ResultT<bool>.Success(true);
                }

                return ResultT<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while resending verification OTP.");

                return ResultT<bool>.Failure(
                    new Error(
                        "ResendVerificationOtp.Failed",
                        "Unable to process the request right now.",
                        ErrorType.Failure));
            }
        }

        public async Task<ResultT<LoginDTO>> LoginAsync(
            IAuthService.LoginIdentityRequest request,
            CancellationToken cancellationToken = default)

        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request is null ||
                string.IsNullOrWhiteSpace(request.EmailOrPhoneNumber) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return ResultT<LoginDTO>.Failure(
                    new Error(
                        "Identity.InvalidCredentials",
                        "Invalid email/phone number or password.",
                        ErrorType.BadRequest));
            }

            var user = await FindUserAsync(
                request.EmailOrPhoneNumber,
                cancellationToken);

            if (user is null)
            {
                return ResultT<LoginDTO>.Failure(
                    new Error(
                        "Identity.InvalidCredentials",
                        "Invalid email/phone number or password.",
                        ErrorType.BadRequest));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return ResultT<LoginDTO>.Failure(
                    new Error(
                        "Account.IsLocked",
                        "Your account is temporarily locked. Please try again later.",
                        ErrorType.BadRequest));
            }

            var passwordValid = await _userManager.CheckPasswordAsync(
                user,
                request.Password);

            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);

                return ResultT<LoginDTO>.Failure(
                    new Error(
                        "Identity.InvalidCredentials",
                        "Invalid email/phone number or password.",
                        ErrorType.BadRequest));
            }

            if (!user.EmailConfirmed)
            {
                return ResultT<LoginDTO>.Failure(
                    new Error(
                        "Identity.EmailNotConfirmed",
                        "Please verify your email before logging in.",
                        ErrorType.BadRequest));
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateAccessTokenAsync(
                user.Id,
                user.FullName,
                user.Email!,
                user.PhoneNumber,
                roles);

            var refreshToken = _tokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                ApplicationUserId = user.Id,
                Token = refreshToken,
                ExpiresOnUtc = DateTimeOffset.UtcNow.AddDays(
                    _options.RefreshTokenExpirationDays)
            };

            await _unitOfWork.Repository<RefreshToken>()
                .CreateAsync(newRefreshToken, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return ResultT<LoginDTO>.Success(
                new LoginDTO
                {
                    UserId = user.Id,
                    AcccessToken = accessToken,
                    RefreshToken = refreshToken,
                    FullName = user.FullName,
                    phoneNumber = user.PhoneNumber!
                });
        }

        public async Task<ResultT<RefreshTokenResponse>> RefreshTokenAsync(
            string RefreshToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return ResultT<RefreshTokenResponse>.Failure(
                    new Error(
                        "RefreshToken.Invalid",
                        "Invalid or already used refresh token.",
                        ErrorType.Unauthorized));
            }

            var storedToken = await _unitOfWork
                .Repository<RefreshToken>()
                .GetOneAsync(
                    x => x.Token == RefreshToken,
                    cancellationToken: cancellationToken);

            if (storedToken is null ||
                storedToken.IsRevoked ||
                storedToken.ExpiresOnUtc <= DateTimeOffset.UtcNow)
            {
                return ResultT<RefreshTokenResponse>.Failure(
                    new Error(
                        "RefreshToken.Invalid",
                        "Invalid or already used refresh token.",
                        ErrorType.Unauthorized));
            }

            var user = await _userManager.FindByIdAsync(
                storedToken.ApplicationUserId.ToString());

            if (user is null)
            {
                return ResultT<RefreshTokenResponse>.Failure(
                    new Error(
                        "RefreshToken.Invalid",
                        "Invalid or already used refresh token.",
                        ErrorType.Unauthorized));
            }

            var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

            var rotated = await _refreshToken.RotateAsync(
                RefreshToken,
                user.Id,
                newRefreshTokenValue,
                DateTimeOffset.UtcNow.AddDays(
                    _options.RefreshTokenExpirationDays),
                cancellationToken);

            if (!rotated)
            {
                return ResultT<RefreshTokenResponse>.Failure(
                    new Error(
                        "RefreshToken.Invalid",
                        "Invalid or already used refresh token.",
                        ErrorType.Unauthorized));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(
                user.Id,
                user.FullName,
                user.Email!,
                user.PhoneNumber,
                roles);

            return ResultT<RefreshTokenResponse>.Success(
                new RefreshTokenResponse
                {
                    UserId = user.Id,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshTokenValue
                });
        }


        public async Task<ResultT<bool>> ForgetPasswordAsync(
          string EmailOrPhoneNumber,
          CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(EmailOrPhoneNumber))
            {
                return ResultT<bool>.Success(true);
            }

            var user = await FindUserAsync(
                EmailOrPhoneNumber,
                cancellationToken);

            if (user is null || !user.EmailConfirmed ||
                string.IsNullOrWhiteSpace(user.Email))
            {
                return ResultT<bool>.Success(true);
            }

            var otpCode = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

            var otpHash = _otpHasher.Hash(
                user.Id,
                otpCode);

            var canSend = await _cacheOtp.TryStoreOtpAsync(
                user.Id,
                "PasswordReset",
                otpHash,
                TimeSpan.FromMinutes(5),
                maxRequests: 3,
                requestWindow: TimeSpan.FromMinutes(15),
                cooldown: TimeSpan.FromSeconds(60),
                cancellationToken);

            if (!canSend)
            {
                return ResultT<bool>.Success(true);
            }

            try
            {
                _emailJobScheduler.EnqueueEmail(
                   user.Email,
                    "Kira - Password Reset",
                    $"Your password reset code is: <h2>{otpCode}</h2>" +
                    "<p>This code expires in 5 minutes.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password reset email for user {UserId}.",
                    user.Id);

                await _cacheOtp.DeleteOtpAsync(
                    user.Id,
                    "PasswordReset",
                    cancellationToken);

                return ResultT<bool>.Success(true);
            }

            return ResultT<bool>.Success(true);
        }
        public async Task<ResultT<string>> ValidateOtpAsync(
            string emailOrPhoneNumber,
            string code,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(emailOrPhoneNumber) ||
                string.IsNullOrWhiteSpace(code))
            {
                return ResultT<string>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            emailOrPhoneNumber = emailOrPhoneNumber.Trim();
            code = code.Trim();

            if (code.Length != 6 ||
                !code.All(char.IsAsciiDigit))
            {
                return ResultT<string>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            var user = await FindUserAsync(
                emailOrPhoneNumber,
                cancellationToken);

            if (user is null || !user.EmailConfirmed)
            {
                return ResultT<string>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            var otpHash = _otpHasher.Hash(
                user.Id,
                code);

            var isValid = await _cacheOtp.VerifyAndConsumeOtpAsync(
                user.Id,
                "PasswordReset",
                otpHash,
                maxAttempts: 5,
                cancellationToken);

            if (!isValid)
            {
                return ResultT<string>.Failure(
                    new Error(
                        "OTP.Invalid",
                        "Invalid or expired verification code.",
                        ErrorType.BadRequest));
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(
                user);

            var resetTokenHash = _otpHasher.Hash(
                user.Id,
                resetToken);

            await _cacheOtp.StoreTemporarySecretAsync(
                user.Id,
                "PasswordResetToken",
                resetTokenHash,
                TimeSpan.FromMinutes(10),
                cancellationToken);

            return ResultT<string>.Success(resetToken);
        }

        public async Task<ResultT<bool>> ResetPasswordAsync(
            string emailOrPhoneNumber,
            string resetToken,
            string newPassword,
            string confirmPassword,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(emailOrPhoneNumber) ||
                string.IsNullOrWhiteSpace(resetToken) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                return ResultT<bool>.Failure(
                    new Error(
                        "ResetPassword.BadRequest",
                        "All fields are required.",
                        ErrorType.BadRequest));
            }

            if (!string.Equals(
                newPassword,
                confirmPassword,
                StringComparison.Ordinal))
            {
                return ResultT<bool>.Failure(
                    new Error(
                        "ResetPassword.PasswordMismatch",
                        "Password and confirmation password do not match.",
                        ErrorType.BadRequest));
            }

            emailOrPhoneNumber = emailOrPhoneNumber.Trim();

            var user = await FindUserAsync(
                emailOrPhoneNumber,
                cancellationToken);

            if (user is null)
            {
                return InvalidResetRequest();
            }

            var resetTokenHash = _otpHasher.Hash(
                user.Id,
                resetToken);

            var operationId = Guid.NewGuid().ToString("N");

            var reserved = await _cacheOtp.TryReserveSecretAsync(
                user.Id,
                "PasswordResetToken",
                resetTokenHash,
                operationId,
                cancellationToken);

            if (!reserved)
            {
                return InvalidResetRequest();
            }

            IdentityResult identityResult;

            try
            {
                identityResult = await _userManager.ResetPasswordAsync(
                    user,
                    resetToken,
                    newPassword);
            }
            catch
            {
                await ReleaseReservationSafelyAsync(
                    user.Id,
                    resetTokenHash,
                    operationId,
                    cancellationToken);

                throw;
            }

            if (!identityResult.Succeeded)
            {
                await ReleaseReservationSafelyAsync(
                    user.Id,
                    resetTokenHash,
                    operationId,
                    cancellationToken);

                var errors = string.Join(
                    "; ",
                    identityResult.Errors.Select(x => x.Description));

                return ResultT<bool>.Failure(
                    new Error(
                        "ResetPassword.Failed",
                        errors,
                        ErrorType.BadRequest));
            }

            var completed = await _cacheOtp.CompleteSecretReservationAsync(
                user.Id,
                "PasswordResetToken",
                operationId,
                cancellationToken);

            if (!completed)
            {
                _logger.LogCritical(
                    "Password changed successfully for user {UserId}, " +
                    "but Redis reset token reservation could not be completed.",
                    user.Id);
            }

            return ResultT<bool>.Success(true);
        }

    }
}
