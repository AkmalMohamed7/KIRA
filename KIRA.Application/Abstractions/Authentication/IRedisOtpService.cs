using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Abstractions.Authentication
{
    public interface IRedisOtpService
    {
        Task<bool> TryStoreOtpAsync(
       Guid userId,
       string purpose,
       string otpHash,
       TimeSpan expiration,
       int maxRequests,
       TimeSpan requestWindow,
       TimeSpan cooldown,
       CancellationToken cancellationToken = default);

        Task<bool> VerifyAndConsumeOtpAsync(
            Guid userId,
            string purpose,
            string otpHash,
            int maxAttempts,
            CancellationToken cancellationToken = default);

        Task DeleteOtpAsync(
            Guid userId,
            string purpose,
            CancellationToken cancellationToken = default);

        Task StoreTemporarySecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            TimeSpan expiration,
            CancellationToken cancellationToken = default);

        Task<bool> TryReserveSecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default);

        Task<bool> CompleteSecretReservationAsync(
            Guid userId,
            string purpose,
            string operationId,
            CancellationToken cancellationToken = default);

        Task<bool> ReleaseSecretReservationAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default);
    }
}
