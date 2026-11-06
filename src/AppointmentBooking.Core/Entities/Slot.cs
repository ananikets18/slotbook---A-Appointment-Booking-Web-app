using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; } = false;

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Changed every time the slot state changes.
    /// </summary>
    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public Booking? Booking { get; set; }
}
