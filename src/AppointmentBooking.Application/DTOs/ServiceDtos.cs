namespace AppointmentBooking.Application.DTOs;

public record ServiceDto(
    Guid Id,
    Guid ProviderId,
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    bool IsActive
);

public record CreateServiceRequest(
    string Name,
    string? Description,
    decimal Price
);

public record UpdateServiceRequest(
    string Name,
    string? Description,
    decimal Price,
    bool IsActive
);
