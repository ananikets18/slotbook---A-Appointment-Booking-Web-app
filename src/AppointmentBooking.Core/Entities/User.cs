using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Provider? ProviderProfile { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
