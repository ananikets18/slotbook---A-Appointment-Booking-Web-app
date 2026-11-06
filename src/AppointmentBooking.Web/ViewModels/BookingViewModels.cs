using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Web.ViewModels;

public class BookingListViewModel
{
    public List<BookingItemViewModel> Bookings { get; set; } = new();
    public string PageTitle { get; set; } = "My Appointments";
    public string? StatusFilter { get; set; }
}

public class BookingItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string? CustomerNotes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool CanCancel => Status == BookingStatus.Pending ||
        (Status == BookingStatus.Confirmed && SlotDate.ToDateTime(StartTime, DateTimeKind.Utc) > DateTime.UtcNow);

    public string StatusBadgeClass => Status switch
    {
        BookingStatus.Pending => "bg-warning text-dark",
        BookingStatus.Confirmed => "bg-success",
        BookingStatus.Cancelled => "bg-danger",
        BookingStatus.Completed => "bg-primary",
        _ => "bg-secondary"
    };
}
