using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Abstractions.Authentication
{
    public interface IEmailJobScheduler
    {
        string EnqueueEmail(
            string recipient,
            string subject,
            string body);
    }
}
