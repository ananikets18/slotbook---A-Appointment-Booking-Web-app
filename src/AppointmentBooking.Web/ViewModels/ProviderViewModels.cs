using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ViewModels;

public class ProviderListViewModel
{
    public List<ProviderDto> Providers { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }
    public string? SearchTerm { get; set; }
}

public class ProviderDetailsViewModel
{
    public ProviderDto Provider { get; set; } = null!;
    public List<ServiceDto> Services { get; set; } = new();
    public DateOnly InitialDate { get; set; }
    public List<SlotDto> AvailableSlots { get; set; } = new();
    public Guid? PreselectedServiceId { get; set; }
}
