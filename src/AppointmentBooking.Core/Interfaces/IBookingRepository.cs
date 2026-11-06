using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Core.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Booking>> GetByProviderIdAsync(Guid providerId, CancellationToken ct = default);
    Task<List<Booking>> GetAllAsync(CancellationToken ct = default);
    Task<List<Booking>> GetExpiredPendingBookingsAsync(TimeSpan expiryAge, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
}
