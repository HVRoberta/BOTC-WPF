namespace BOTC.Domain.Events;

/// <summary>
/// Base record type for all domain events raised by aggregates.
/// Domain events represent state changes that have occurred within the domain.
/// </summary>
public abstract record DomainEvent
{
    protected DomainEvent(DateTime occurredAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(occurredAtUtc.Kind, DateTimeKind.Utc);
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>
    /// The timestamp (UTC) when this domain event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; }
}


