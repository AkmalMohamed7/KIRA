using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Infratructure.Caching
{
    public sealed class InMemoryOtpStorageService : IOtpStorageService
    {
        private readonly IMemoryCache _cache;

        // بنستخدم Lock واحد عشان نضمن إن العمليات الحساسة
        // متتنفذش في نفس الوقت وتسبب Race Conditions.
        private readonly SemaphoreSlim _lock = new(1, 1);

        public InMemoryOtpStorageService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string GetKey(Guid userId, string purpose)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid user ID.", nameof(userId));

            if (string.IsNullOrWhiteSpace(purpose) ||
                purpose.Length > 50 ||
                !purpose.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                throw new ArgumentException("Invalid OTP purpose.", nameof(purpose));
            }

            return $"otp:{userId:N}:{purpose}";
        }

        private static void ValidateHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash is required.", nameof(hash));
        }

        private static void ValidateExpiration(TimeSpan expiration)
        {
            if (expiration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(expiration));
        }

        private static void ValidateOperationId(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId) ||
                operationId.Length > 100)
            {
                throw new ArgumentException(
                    "Invalid operation ID.",
                    nameof(operationId));
            }
        }

        private sealed class OtpEntry
        {
            public required string Hash { get; init; }
            public int Attempts { get; set; }
            public required DateTimeOffset ExpiresAt { get; init; }
        }

        private sealed class SecretEntry
        {
            public required string Hash { get; set; }
            public string? ReservedBy { get; set; }
            public required DateTimeOffset ExpiresAt { get; init; }
        }

        private static MemoryCacheEntryOptions ExpirationOptions(
            DateTimeOffset expiresAt)
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            };
        }

        public async Task<bool> TryStoreOtpAsync(
            Guid userId,
            string purpose,
            string otpHash,
            TimeSpan expiration,
            int maxRequests,
            TimeSpan requestWindow,
            TimeSpan cooldown,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(otpHash);
            ValidateExpiration(expiration);
            ValidateExpiration(requestWindow);
            ValidateExpiration(cooldown);

            if (maxRequests <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRequests));

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                var now = DateTimeOffset.UtcNow;

                var cooldownKey = $"{key}:cooldown";
                var requestsKey = $"{key}:requests";

                // Check cooldown.
                if (_cache.TryGetValue(cooldownKey, out _))
                    return false;

                // Get current request count.
                var count = _cache.Get<int?>(requestsKey) ?? 0;

                count++;

                // First request starts the request window.
                if (count == 1)
                {
                    _cache.Set(
                        requestsKey,
                        count,
                        ExpirationOptions(now.Add(requestWindow)));
                }
                else
                {
                    // Preserve the original request window.
                    var requestsExpiry = _cache.Get<DateTimeOffset>(
                        $"{requestsKey}:expiry");

                    _cache.Set(requestsKey, count,
                        ExpirationOptions(requestsExpiry));
                }

                // Store request window expiration.
                if (count == 1)
                {
                    _cache.Set(
                        $"{requestsKey}:expiry",
                        now.Add(requestWindow),
                        ExpirationOptions(now.Add(requestWindow)));
                }

                if (count > maxRequests)
                    return false;

                // Store OTP.
                var otpEntry = new OtpEntry
                {
                    Hash = otpHash,
                    Attempts = 0,
                    ExpiresAt = now.Add(expiration)
                };

                _cache.Set(
                    key,
                    otpEntry,
                    ExpirationOptions(otpEntry.ExpiresAt));

                // Apply cooldown.
                _cache.Set(
                    cooldownKey,
                    true,
                    ExpirationOptions(now.Add(cooldown)));

                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> VerifyAndConsumeOtpAsync(
            Guid userId,
            string purpose,
            string otpHash,
            int maxAttempts,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(otpHash);

            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_cache.TryGetValue<OtpEntry>(key, out var entry) ||
                    entry is null)
                {
                    return false;
                }

                if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    _cache.Remove(key);
                    return false;
                }

                // Maximum attempts reached.
                if (entry.Attempts >= maxAttempts)
                    return false;

                // Wrong OTP.
                if (!string.Equals(
                    entry.Hash,
                    otpHash,
                    StringComparison.Ordinal))
                {
                    entry.Attempts++;

                    return false;
                }

                // Correct OTP: consume it.
                _cache.Remove(key);

                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task DeleteOtpAsync(
            Guid userId,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                _cache.Remove(key);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task StoreTemporarySecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateExpiration(expiration);

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                var entry = new SecretEntry
                {
                    Hash = secretHash,
                    ReservedBy = null,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(expiration)
                };

                _cache.Set(
                    key,
                    entry,
                    ExpirationOptions(entry.ExpiresAt));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> TryReserveSecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_cache.TryGetValue<SecretEntry>(key, out var entry) ||
                    entry is null)
                {
                    return false;
                }

                if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    _cache.Remove(key);
                    return false;
                }

                // Secret must match and must not be reserved.
                if (entry.Hash != secretHash ||
                    entry.ReservedBy is not null)
                {
                    return false;
                }

                entry.ReservedBy = operationId;

                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> CompleteSecretReservationAsync(
            Guid userId,
            string purpose,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_cache.TryGetValue<SecretEntry>(key, out var entry) ||
                    entry is null)
                {
                    return false;
                }

                if (entry.ExpiresAt <= DateTimeOffset.UtcNow ||
                    entry.ReservedBy != operationId)
                {
                    return false;
                }

                // Reservation completed: consume secret.
                _cache.Remove(key);

                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> ReleaseSecretReservationAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_cache.TryGetValue<SecretEntry>(key, out var entry) ||
                    entry is null)
                {
                    return false;
                }

                if (entry.ExpiresAt <= DateTimeOffset.UtcNow ||
                    entry.ReservedBy != operationId)
                {
                    return false;
                }

                // Restore the original secret.
                entry.Hash = secretHash;
                entry.ReservedBy = null;

                return true;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
