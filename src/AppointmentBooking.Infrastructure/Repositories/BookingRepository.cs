using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Include(b => b.Slot)
                .ThenInclude(s => s.Provider)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Include(b => b.Slot)
                .ThenInclude(s => s.Provider)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetByProviderIdAsync(Guid providerId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Include(b => b.Slot)
                .ThenInclude(s => s.Provider)
            .Where(b => b.Slot.ProviderId == providerId)
            .OrderByDescending(b => b.Slot.Date)
            .ThenByDescending(b => b.Slot.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Include(b => b.Slot)
                .ThenInclude(s => s.Provider)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetExpiredPendingBookingsAsync(TimeSpan expiryAge, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(expiryAge);
        return await _context.Bookings
            .Include(b => b.Slot)
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAt <= cutoff)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        await _context.Bookings.AddAsync(booking, ct);
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Update(booking);
        return Task.CompletedTask;
    }
}
