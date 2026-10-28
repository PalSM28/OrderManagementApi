using System;

namespace OrderManagement.Entity
{
    public interface IAuditable
    {
        DateTime CreatedUtc { get; set; }
        DateTime? UpdatedUtc { get; set; }

        // C# 8: default interface method. Implementers get this for free.
        string Describe() =>
            $"Created {CreatedUtc:O}; " + (UpdatedUtc.HasValue ? $"last updated {UpdatedUtc:O}" : "never updated");
    }
}
