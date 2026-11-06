using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public interface IProviderService
{
    Task<List<ProviderDto>> GetProvidersAsync(string? category = null, CancellationToken ct = default);
    Task<List<ProviderDto>> GetAllProvidersForAdminAsync(CancellationToken ct = default);
    Task<ProviderDto?> GetByIdAsync(Guid providerId, CancellationToken ct = default);
    Task<ProviderDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ProviderDto> CreateProviderProfileAsync(Guid userId, CreateProviderRequest request, CancellationToken ct = default);
    Task<List<ServiceDto>> GetServicesByProviderIdAsync(Guid providerId, bool activeOnly = true, CancellationToken ct = default);
    Task<ServiceDto> AddServiceAsync(Guid providerId, CreateServiceRequest request, CancellationToken ct = default);
    Task<ServiceDto> UpdateServiceAsync(Guid serviceId, Guid providerUserId, UpdateServiceRequest request, CancellationToken ct = default);
}

public class ProviderService : IProviderService
{
    private readonly IProviderRepository _providerRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ProviderService(
        IProviderRepository providerRepo,
        IServiceRepository serviceRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork)
    {
        _providerRepo = providerRepo;
        _serviceRepo = serviceRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProviderDto>> GetProvidersAsync(string? category = null, CancellationToken ct = default)
    {
        var providers = await _providerRepo.GetAllActiveAsync(category, ct);
        return providers.Select(MapToDto).ToList();
    }

    public async Task<List<ProviderDto>> GetAllProvidersForAdminAsync(CancellationToken ct = default)
    {
        var providers = await _providerRepo.GetAllAsync(ct);
        return providers.Select(MapToDto).ToList();
    }

    public async Task<ProviderDto?> GetByIdAsync(Guid providerId, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByIdAsync(providerId, ct);
        return provider is null ? null : MapToDto(provider);
    }

    public async Task<ProviderDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByUserIdAsync(userId, ct);
        return provider is null ? null : MapToDto(provider);
    }

    public async Task<ProviderDto> CreateProviderProfileAsync(Guid userId, CreateProviderRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var existing = await _providerRepo.GetByUserIdAsync(userId, ct);
        if (existing is not null)
        {
            throw new ConflictException("A provider profile already exists for this user account.");
        }

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = request.BusinessName.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address?.Trim(),
            ContactPhone = request.ContactPhone?.Trim() ?? user.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _providerRepo.AddAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        provider.User = user;
        return MapToDto(provider);
    }

    public async Task<List<ServiceDto>> GetServicesByProviderIdAsync(Guid providerId, bool activeOnly = true, CancellationToken ct = default)
    {
        var services = await _serviceRepo.GetByProviderIdAsync(providerId, activeOnly, ct);
        return services.Select(MapToServiceDto).ToList();
    }

    public async Task<ServiceDto> AddServiceAsync(Guid providerId, CreateServiceRequest request, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByIdAsync(providerId, ct);
        if (provider is null)
        {
            throw new NotFoundException("Provider not found.");
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            DurationMinutes = 30, // 30-minute standard slot
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _serviceRepo.AddAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToServiceDto(service);
    }

    public async Task<ServiceDto> UpdateServiceAsync(Guid serviceId, Guid providerUserId, UpdateServiceRequest request, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId, ct);
        if (service is null)
        {
            throw new NotFoundException("Service not found.");
        }

        var provider = await _providerRepo.GetByIdAsync(service.ProviderId, ct);
        if (provider is null || provider.UserId != providerUserId)
        {
            throw new ForbiddenException("You cannot edit a service belonging to another provider.");
        }

        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.Price = request.Price;
        service.IsActive = request.IsActive;

        await _serviceRepo.UpdateAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToServiceDto(service);
    }

    private static ProviderDto MapToDto(Provider p)
    {
        return new ProviderDto(
            p.Id,
            p.UserId,
            p.BusinessName,
            p.Category,
            p.Description,
            p.Address,
            p.ContactPhone,
            p.User?.FullName ?? "Unknown",
            p.User?.Email ?? string.Empty,
            p.IsActive,
            p.Services?.Count ?? 0
        );
    }

    private static ServiceDto MapToServiceDto(Service s)
    {
        return new ServiceDto(
            s.Id,
            s.ProviderId,
            s.Name,
            s.Description,
            s.Price,
            s.DurationMinutes,
            s.IsActive
        );
    }
}
