using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Shared.Result
{
    public record Error(string Code, string Description, ErrorType Type)
    {
    }
}
