using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace KIRA.Infratructure.Services.Identity
{
    public sealed class OtpHasher : IOtpHasher
    {
        private readonly byte[] _key;

        public OtpHasher(IConfiguration configuration)
        {
            var secret = configuration["OtpSettings:HashKey"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "OTP hash key is not configured.");
            }

            try
            {
                _key = Convert.FromBase64String(secret);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "OTP hash key must be a valid Base64 string.",
                    ex);
            }

            if (_key.Length < 32)
            {
                throw new InvalidOperationException(
                    "OTP hash key must be at least 32 bytes.");
            }
        }

        public string Hash(Guid userId, string code)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(
                userId,
                Guid.Empty);

            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var value = $"{userId:N}:{code}";
            var bytes = Encoding.UTF8.GetBytes(value);

            var hash = HMACSHA256.HashData(_key, bytes);

            return Convert.ToBase64String(hash);
        }

        public bool Verify(
            Guid userId,
            string code,
            string storedHash)
        {
            if (userId == Guid.Empty ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            byte[] expectedHash;

            try
            {
                expectedHash = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            // HMAC-SHA256 produces exactly 32 bytes.
            if (expectedHash.Length != 32)
            {
                return false;
            }

            var value = $"{userId:N}:{code}";
            var bytes = Encoding.UTF8.GetBytes(value);

            var actualHash = HMACSHA256.HashData(_key, bytes);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
    }
}
