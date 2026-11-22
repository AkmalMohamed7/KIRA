using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Infratructure.BackgroundJobs
{
    public sealed record EmailJobPayload(
         string Recipient,
         string Subject,
         string Body);
}
