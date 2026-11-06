namespace AppointmentBooking.Application.DTOs;

public record ProviderDto(
    Guid Id,
    Guid UserId,
    string BusinessName,
    string Category,
    string? Description,
    string? Address,
    string? ContactPhone,
    string OwnerName,
    string OwnerEmail,
    bool IsActive,
    int ServicesCount = 0
);

public record CreateProviderRequest(
    string BusinessName,
    string Category,
    string? Description,
    string? Address,
    string? ContactPhone
);
