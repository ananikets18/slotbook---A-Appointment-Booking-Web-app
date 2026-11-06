using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly Core.Interfaces.IUserRepository _userRepo;
    private readonly Core.Interfaces.IProviderRepository _providerRepo;
    private readonly Core.Interfaces.IUnitOfWork _unitOfWork;

    public AuthService(
        Core.Interfaces.IUserRepository userRepo,
        Core.Interfaces.IProviderRepository providerRepo,
        Core.Interfaces.IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _providerRepo = providerRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (existing is not null)
        {
            return new AuthResult(false, "An account with this email already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            PasswordHash = passwordHash,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user, ct);

        // If registering as Provider, automatically create their initial Provider profile
        if (request.Role == Core.Enums.UserRole.Provider)
        {
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? $"{user.FullName}'s Services" : request.BusinessName.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
                ContactPhone = user.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _providerRepo.AddAsync(provider, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResult(
            true,
            "Registration successful.",
            new UserDto(user.Id, user.Email, user.FullName, user.PhoneNumber, user.Role, user.CreatedAt)
        );
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (user is null)
        {
            return null;
        }

        bool verified = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return verified ? user : null;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null) return null;

        return new UserDto(user.Id, user.Email, user.FullName, user.PhoneNumber, user.Role, user.CreatedAt);
    }
}
