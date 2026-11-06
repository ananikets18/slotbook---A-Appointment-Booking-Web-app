using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ViewModels;

public class HomeViewModel
{
    public List<ProviderDto> FeaturedProviders { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public int TotalProviders { get; set; }
    public int TotalServices { get; set; }
    public int TotalBookings { get; set; }
    public string? SelectedCategory { get; set; }
}
