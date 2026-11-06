using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.DomainServices;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public interface ISlotService
{
    Task<List<SlotDto>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<List<SlotDto>> GetAllSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}

public class SlotService : ISlotService
{
    private readonly ISlotRepository _slotRepo;
    private readonly IProviderRepository _providerRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SlotService> _logger;

    public SlotService(
        ISlotRepository slotRepo,
        IProviderRepository providerRepo,
        IBookingRepository bookingRepo,
        IUnitOfWork unitOfWork,
        ILogger<SlotService> logger)
    {
        _slotRepo = slotRepo;
        _providerRepo = providerRepo;
        _bookingRepo = bookingRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<SlotDto>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        // On-demand expiry: release any pending bookings older than 30 minutes
        await ReleaseExpiredPendingBookingsAsync(ct);

        var slots = await _slotRepo.GetAvailableSlotsAsync(providerId, date, ct);
        return slots.Select(s => new SlotDto(
            s.Id,
            s.ProviderId,
            s.Date,
            s.StartTime,
            s.EndTime,
            s.IsBooked
        )).ToList();
    }

    public async Task<List<SlotDto>> GetAllSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        // On-demand expiry: release any pending bookings older than 30 minutes
        await ReleaseExpiredPendingBookingsAsync(ct);

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

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var alreadyExists = await _slotRepo.HasSlotsForDateAsync(providerId, date, ct);
            if (!alreadyExists)
            {
                var daySlots = candidateSlots.Where(s => s.Date == date);
                slotsToAdd.AddRange(daySlots);
            }
        }

        if (slotsToAdd.Count > 0)
        {
            await _slotRepo.AddRangeAsync(slotsToAdd, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return slotsToAdd.Count;
    }

    private async Task ReleaseExpiredPendingBookingsAsync(CancellationToken ct = default)
    {
        try
        {
            var expiredBookings = await _bookingRepo.GetExpiredPendingBookingsAsync(TimeSpan.FromMinutes(30), ct);
            if (expiredBookings.Count == 0) return;

            foreach (var booking in expiredBookings)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = "Automatically expired: Pending reservation exceeded 30 minutes.";
                booking.UpdatedAt = DateTime.UtcNow;

                if (booking.Slot is not null)
                {
                    booking.Slot.IsBooked = false;
                    booking.Slot.ConcurrencyStamp = Guid.NewGuid();
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("On-demand expiry: automatically released {Count} expired pending booking(s).", expiredBookings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run on-demand expiry sweep.");
        }
    }
}
