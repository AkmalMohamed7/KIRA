using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Infratructure.Services.Identity
{
    public class RefreshTokenRotationService : IRefreshTokenRotationService
    {
        private readonly KiraDbContext _db;

        public RefreshTokenRotationService(KiraDbContext db)
        {
            _db = db;
        }

        public async Task<bool> RotateAsync(
            string oldToken,
            Guid userId,
            string newToken,
            DateTimeOffset expiresOnUtc,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var now = DateTimeOffset.UtcNow;

            var changed = await _db.RefreshTokens
                .Where(x =>
                    x.Token == oldToken &&
                    x.ApplicationUserId == userId &&
                    !x.IsRevoked &&
                    x.ExpiresOnUtc > now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedOnUtc, now),
                    cancellationToken);

            if (changed != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var oldTokenEntity = await _db.RefreshTokens
                .AsNoTracking()
                .SingleAsync(
                    x => x.Token == oldToken &&
                         x.ApplicationUserId == userId,
                    cancellationToken);

            var replacement = new RefreshToken
            {
                ApplicationUserId = userId,
                Token = newToken,
                ExpiresOnUtc = expiresOnUtc,
                IsRevoked = false
            };

            _db.RefreshTokens.Add(replacement);

            await _db.SaveChangesAsync(cancellationToken);

            await _db.RefreshTokens
                .Where(x => x.Id == oldTokenEntity.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.ReplacedByTokenId,
                            replacement.Id),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return true;
        }
    }
}
