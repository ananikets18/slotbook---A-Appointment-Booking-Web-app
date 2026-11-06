using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

public interface ISlotRepository
{
    Task<Slot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Slot>> GetSlotsByProviderAndDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<List<Slot>> GetAvailableSlotsAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task<bool> HasSlotsForDateAsync(Guid providerId, DateOnly date, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Slot> slots, CancellationToken ct = default);
    Task UpdateAsync(Slot slot, CancellationToken ct = default);
}
