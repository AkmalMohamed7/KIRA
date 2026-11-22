
using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Abstractions.Authentication
{
    public interface IOtpHasher
    {
        string Hash(Guid userId, string code);

        bool Verify(
            Guid userId,
            string code,
            string storedHash);
    }
}
