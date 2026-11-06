using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Service>> GetByProviderIdAsync(Guid providerId, bool activeOnly = true, CancellationToken ct = default);
    Task AddAsync(Service service, CancellationToken ct = default);
    Task UpdateAsync(Service service, CancellationToken ct = default);
}
