using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid customerUserId, CreateBookingRequest request, CancellationToken ct = default);
    Task<BookingDto> UpdateStatusAsync(Guid bookingId, Guid actingUserId, UserRole actingRole, UpdateBookingStatusRequest request, CancellationToken ct = default);
    Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<List<BookingDto>> GetUserBookingsAsync(Guid customerUserId, CancellationToken ct = default);
    Task<List<BookingDto>> GetProviderBookingsAsync(Guid providerId, CancellationToken ct = default);
    Task<List<BookingDto>> GetAllBookingsAsync(CancellationToken ct = default);
    Task<int> ExpireOldPendingBookingsAsync(TimeSpan threshold, CancellationToken ct = default);
}

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ISlotRepository _slotRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlotService _slotService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepo,
        ISlotRepository slotRepo,
        IServiceRepository serviceRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        ISlotService slotService,
        ILogger<BookingService> logger)
    {
        _bookingRepo = bookingRepo;
        _slotRepo = slotRepo;
        _serviceRepo = serviceRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _slotService = slotService;
        _logger = logger;
    }

    public async Task<BookingDto> CreateBookingAsync(Guid customerUserId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(customerUserId, ct);
        if (user is null)
        {
            throw new NotFoundException("Customer user account not found.");
        }

        var service = await _serviceRepo.GetByIdAsync(request.ServiceId, ct);
        if (service is null || !service.IsActive)
        {
            throw new NotFoundException("Service not found or is currently inactive.");
        }

        var slot = await _slotRepo.GetByIdAsync(request.SlotId, ct);
        if (slot is null)
        {
            throw new NotFoundException("Selected appointment slot not found.");
        }

        if (slot.ProviderId != service.ProviderId)
        {
            throw new ValidationException("The selected slot does not belong to the provider of this service.");
        }

        if (slot.IsBooked)
        {
            throw new ConflictException("This slot is already booked. Please select an available slot.");
        }

        // Prevent booking past slots
        var nowUtc = DateTime.UtcNow;
        var slotDateTime = slot.Date.ToDateTime(slot.StartTime, DateTimeKind.Utc);
        if (slotDateTime <= nowUtc)
        {
            throw new ValidationException("Cannot book a slot that has already passed.");
        }

        // --- DOUBLE-BOOKING PREVENTION ---
        // 1. Transaction scope
        // 2. Mark slot as booked & update ConcurrencyStamp for optimistic concurrency
        // 3. Save booking with unique index on SlotId
        // 4. Catch DbUpdateConcurrencyException / DbUpdateException and rollback
        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            slot.IsBooked = true;
            slot.ConcurrencyStamp = Guid.NewGuid(); // optimistic concurrency token change
            await _slotRepo.UpdateAsync(slot, ct);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = customerUserId,
                ServiceId = service.Id,
                SlotId = slot.Id,
                Status = BookingStatus.Pending,
                TotalPrice = service.Price,
                CustomerNotes = request.CustomerNotes?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _bookingRepo.AddAsync(booking, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Evict slot lookup cache
            _slotService.InvalidateSlotCache(slot.ProviderId, slot.Date);

            _logger.LogInformation("Successfully created Booking {BookingId} for User {UserId} on Slot {SlotId}",
                booking.Id, customerUserId, slot.Id);

            var fullBooking = await _bookingRepo.GetByIdWithDetailsAsync(booking.Id, ct);
            return MapToDto(fullBooking ?? booking);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Concurrency conflict: Slot {SlotId} was booked concurrently by another user.", slot.Id);
            _slotService.InvalidateSlotCache(slot.ProviderId, slot.Date);
            throw new ConflictException("This slot was just booked by another user. Please choose a different slot.");
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Database constraint violation while booking slot {SlotId}.", slot.Id);
            _slotService.InvalidateSlotCache(slot.ProviderId, slot.Date);
            throw new ConflictException("This slot is no longer available. Please select another slot.");
        }
    }

    public async Task<BookingDto> UpdateStatusAsync(
        Guid bookingId,
        Guid actingUserId,
        UserRole actingRole,
        UpdateBookingStatusRequest request,
        CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdWithDetailsAsync(bookingId, ct);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID {bookingId} was not found.");
        }

        // Authorization checks
        if (actingRole == UserRole.Customer && booking.UserId != actingUserId)
        {
            throw new ForbiddenException("You cannot modify another customer's booking.");
        }
        if (actingRole == UserRole.Provider && booking.Slot.Provider.UserId != actingUserId)
        {
            throw new ForbiddenException("You cannot modify bookings that belong to another provider.");
        }

        var nowUtc = DateTime.UtcNow;

        // State Machine validation
        BookingStatusService.ValidateTransition(
            booking.Status,
            request.NewStatus,
            actingRole,
            booking.Slot.Date,
            booking.Slot.StartTime,
            nowUtc);

        var previousStatus = booking.Status;
        booking.Status = request.NewStatus;
        booking.UpdatedAt = nowUtc;

        if (request.NewStatus == BookingStatus.Cancelled)
        {
            booking.CancellationReason = request.Reason ?? $"Cancelled by {actingRole}";
            
            // Re-open the slot for booking if cancelled
            booking.Slot.IsBooked = false;
            booking.Slot.ConcurrencyStamp = Guid.NewGuid();
            await _slotRepo.UpdateAsync(booking.Slot, ct);
            _slotService.InvalidateSlotCache(booking.Slot.ProviderId, booking.Slot.Date);
        }

        await _bookingRepo.UpdateAsync(booking, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {BookingId} transitioned from {OldStatus} to {NewStatus} by User {UserId} ({Role})",
            booking.Id, previousStatus, request.NewStatus, actingUserId, actingRole);

        return MapToDto(booking);
    }

    public async Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdWithDetailsAsync(bookingId, ct);
        return booking is null ? null : MapToDto(booking);
    }

    public async Task<List<BookingDto>> GetUserBookingsAsync(Guid customerUserId, CancellationToken ct = default)
    {
        var bookings = await _bookingRepo.GetByUserIdAsync(customerUserId, ct);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<List<BookingDto>> GetProviderBookingsAsync(Guid providerId, CancellationToken ct = default)
    {
        var bookings = await _bookingRepo.GetByProviderIdAsync(providerId, ct);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<List<BookingDto>> GetAllBookingsAsync(CancellationToken ct = default)
    {
        var bookings = await _bookingRepo.GetAllAsync(ct);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<int> ExpireOldPendingBookingsAsync(TimeSpan threshold, CancellationToken ct = default)
    {
        var expiredBookings = await _bookingRepo.GetExpiredPendingBookingsAsync(threshold, ct);
        if (expiredBookings.Count == 0) return 0;

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = "Automatically expired: Pending booking was not confirmed within the allowed time window.";
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Slot is not null)
            {
                booking.Slot.IsBooked = false;
                booking.Slot.ConcurrencyStamp = Guid.NewGuid();
                _slotService.InvalidateSlotCache(booking.Slot.ProviderId, booking.Slot.Date);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Expired {Count} unconfirmed pending bookings.", expiredBookings.Count);
        return expiredBookings.Count;
    }

    private static BookingDto MapToDto(Booking b)
    {
        return new BookingDto(
            b.Id,
            b.UserId,
            b.User?.FullName ?? "Unknown Customer",
            b.User?.Email ?? string.Empty,
            b.User?.PhoneNumber,
            b.Slot?.ProviderId ?? Guid.Empty,
            b.Slot?.Provider?.BusinessName ?? "Provider",
            b.ServiceId,
            b.Service?.Name ?? "Service",
            b.TotalPrice,
            b.SlotId,
            b.Slot?.Date ?? default,
            b.Slot?.StartTime ?? default,
            b.Slot?.EndTime ?? default,
            b.Status,
            b.CustomerNotes,
            b.CancellationReason,
            b.CreatedAt,
            b.UpdatedAt
        );
    }
}
