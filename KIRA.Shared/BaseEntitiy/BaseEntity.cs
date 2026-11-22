using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Shared.BaseEntitiy
{
    public class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
