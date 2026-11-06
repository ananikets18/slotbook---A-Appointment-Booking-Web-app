using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Web.ViewModels;

public class ProviderDashboardViewModel
{
    public ProviderDto Provider { get; set; } = null!;
    public List<BookingItemViewModel> RecentBookings { get; set; } = new();
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalServicesCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ManageServicesViewModel
{
    public Guid ProviderId { get; set; }
    public List<ServiceDto> Services { get; set; } = new();
    public CreateServiceRequest NewService { get; set; } = new(string.Empty, null, 0);
}

public class ManageSlotsViewModel
{
    public Guid ProviderId { get; set; }
    public DateOnly SelectedDate { get; set; }
    public List<SlotDto> Slots { get; set; } = new();
    public DateOnly GenerateFromDate { get; set; }
    public DateOnly GenerateToDate { get; set; }
}
