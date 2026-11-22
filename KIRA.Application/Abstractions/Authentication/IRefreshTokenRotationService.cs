using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Abstractions.Authentication
{
    public interface IRefreshTokenRotationService
    {
        Task<bool> RotateAsync(
            string oldToken,
            Guid userId,
            string newToken,
            DateTimeOffset expiresOnUtc,
            CancellationToken cancellationToken);
    }
}
