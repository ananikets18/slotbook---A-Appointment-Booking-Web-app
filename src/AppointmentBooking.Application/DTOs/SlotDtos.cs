namespace AppointmentBooking.Application.DTOs;

public record SlotDto(
    Guid Id,
    Guid ProviderId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsBooked
);

public record GenerateSlotsRequest(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate
);
