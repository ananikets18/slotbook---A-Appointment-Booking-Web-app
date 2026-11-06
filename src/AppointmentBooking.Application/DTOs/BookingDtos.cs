using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid UserId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    Guid ProviderId,
    string ProviderName,
    Guid ServiceId,
    string ServiceName,
    decimal TotalPrice,
    Guid SlotId,
    DateOnly SlotDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BookingStatus Status,
    string? CustomerNotes,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateBookingRequest(
    Guid ServiceId,
    Guid SlotId,
    string? CustomerNotes = null
);

public record UpdateBookingStatusRequest(
    BookingStatus NewStatus,
    string? Reason = null
);
