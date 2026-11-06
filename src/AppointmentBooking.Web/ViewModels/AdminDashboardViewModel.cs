using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalProviders { get; set; }
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public decimal TotalPlatformVolume { get; set; }

    public List<UserDto> Users { get; set; } = new();
    public List<ProviderDto> Providers { get; set; } = new();
    public List<BookingItemViewModel> RecentBookings { get; set; } = new();
}
