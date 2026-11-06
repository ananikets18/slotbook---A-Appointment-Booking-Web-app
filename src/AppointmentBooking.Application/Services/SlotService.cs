using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.DomainServices;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public interface ISlotService
{
    Task<List<SlotDto>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<List<SlotDto>> GetAllSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
    void InvalidateSlotCache(Guid providerId, DateOnly date);
}

public class SlotService : ISlotService
{
    private readonly ISlotRepository _slotRepo;
    private readonly IProviderRepository _providerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SlotService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public SlotService(
        ISlotRepository slotRepo,
        IProviderRepository providerRepo,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<SlotService> logger)
    {
        _slotRepo = slotRepo;
        _providerRepo = providerRepo;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    private static string GetCacheKey(Guid providerId, DateOnly date) =>
        $"slots:avail:{providerId}:{date:yyyyMMdd}";

    public async Task<List<SlotDto>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(providerId, date);

        if (_cache.TryGetValue(cacheKey, out List<SlotDto>? cachedSlots) && cachedSlots is not null)
        {
            _logger.LogDebug("Cache HIT for available slots of provider {ProviderId} on {Date}", providerId, date);
            return cachedSlots;
        }

        _logger.LogDebug("Cache MISS for available slots of provider {ProviderId} on {Date}", providerId, date);
        var slots = await _slotRepo.GetAvailableSlotsAsync(providerId, date, ct);

        var dtos = slots.Select(s => new SlotDto(
            s.Id,
            s.ProviderId,
            s.Date,
            s.StartTime,
            s.EndTime,
            s.IsBooked
        )).ToList();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheDuration)
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

        _cache.Set(cacheKey, dtos, cacheEntryOptions);
        return dtos;
    }

    public async Task<List<SlotDto>> GetAllSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        var slots = await _slotRepo.GetSlotsByProviderAndDateAsync(providerId, date, ct);
        return slots.Select(s => new SlotDto(
            s.Id,
            s.ProviderId,
            s.Date,
            s.StartTime,
            s.EndTime,
            s.IsBooked
        )).ToList();
    }

    public async Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByIdAsync(providerId, ct);
        if (provider is null)
        {
            throw new NotFoundException($"Provider with ID {providerId} was not found.");
        }

        var candidateSlots = SlotGenerator.GenerateFixedDailySlots(providerId, fromDate, toDate);
        var slotsToAdd = new List<Slot>();

        // Check each date to avoid inserting duplicate slots for dates that already have slots
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var alreadyExists = await _slotRepo.HasSlotsForDateAsync(providerId, date, ct);
            if (!alreadyExists)
            {
                var daySlots = candidateSlots.Where(s => s.Date == date);
                slotsToAdd.AddRange(daySlots);
            }
            InvalidateSlotCache(providerId, date);
        }

        if (slotsToAdd.Count > 0)
        {
            await _slotRepo.AddRangeAsync(slotsToAdd, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return slotsToAdd.Count;
    }

    public void InvalidateSlotCache(Guid providerId, DateOnly date)
    {
        var cacheKey = GetCacheKey(providerId, date);
        _cache.Remove(cacheKey);
        _logger.LogInformation("Evicted cache for {CacheKey}", cacheKey);
    }
}
