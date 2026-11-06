namespace AppointmentBooking.Core.Entities;

public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string BusinessName { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // e.g., Clinic, Salon, Tutor
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}
