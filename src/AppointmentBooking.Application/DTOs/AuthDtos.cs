using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    UserRole Role,
    DateTime CreatedAt
);

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    UserRole Role = UserRole.Customer,
    string? BusinessName = null,
    string? Category = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResult(
    bool Success,
    string? Message = null,
    UserDto? User = null
);
