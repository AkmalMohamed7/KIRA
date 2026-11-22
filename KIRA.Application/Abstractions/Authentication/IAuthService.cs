namespace KIRA.Application.Abstractions.Authentication
{
    public interface IAuthService
    {
        Task<ResultT<Guid>> RegisterAsync(
           RegisterIdentityRequest request,
           CancellationToken cancellationToken = default);

        Task<ResultT<LoginDTO>> LoginAsync(
          LoginIdentityRequest request,
          CancellationToken cancellationToken = default);

        Task<ResultT<bool>> VerifyEmailAsync(
          string email,
          string code,
          CancellationToken cancellationToken = default);

        Task<ResultT<RefreshTokenResponse>> RefreshTokenAsync(
           string RefreshToken,
           CancellationToken cancellationToken);
        Task<ResultT<bool>> ForgetPasswordAsync(
            string EmailOrPhoneNumber,
            CancellationToken cancellationToken);

        Task<ResultT<string>> ValidateOtpAsync(
            string emailOrPhoneNumber,
            string code,
            CancellationToken cancellationToken);

        Task<ResultT<bool>> ResetPasswordAsync(
            string emailOrPhoneNumber,
            string resetToken,
            string newPassword,
            string confirmPassword,
            CancellationToken cancellationToken = default);

        Task<ResultT<bool>> ResendVerificationOtpAsync(
            string emailOrPhoneNumber,
            CancellationToken cancellationToken = default);

        public sealed record RegisterIdentityRequest(
          string FullName,
          string Email,
          string Password,
          string PhoneNumber,
          DateOnly BirthDate,
          List<string> Interests,
          string Industry,
          string BusinessDomain,
          string JobTitle,
          string ComapnyName);

        public sealed record LoginIdentityRequest(
          string EmailOrPhoneNumber,
          string Password,
          bool RememberMe);


    }
}
