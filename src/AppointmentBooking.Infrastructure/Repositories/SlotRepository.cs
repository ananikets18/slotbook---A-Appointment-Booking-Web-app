using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Repositories;

public class SlotRepository : ISlotRepository
{
    private readonly AppDbContext _context;

    public SlotRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Slot?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Slots
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Slot>> GetSlotsByProviderAndDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Slots
            .Where(s => s.ProviderId == providerId && s.Date == date)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<Slot>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Slots
            .Where(s => s.ProviderId == providerId && s.Date == date && !s.IsBooked)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<bool> HasSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Slots
            .AnyAsync(s => s.ProviderId == providerId && s.Date == date, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Slot> slots, CancellationToken ct = default)
    {
        await _context.Slots.AddRangeAsync(slots, ct);
    }

    public Task UpdateAsync(Slot slot, CancellationToken ct = default)
    {
        _context.Slots.Update(slot);
        return Task.CompletedTask;
    }
}
