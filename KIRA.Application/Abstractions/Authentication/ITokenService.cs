using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Abstractions.Authentication
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(Guid userId,
            string fullName,
            string email,
            string? phoneNumber,
            IEnumerable<string> roles);

        string GenerateRefreshToken();
    }
}
