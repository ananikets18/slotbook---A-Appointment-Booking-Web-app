using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Provider?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Provider>> GetAllActiveAsync(string? category = null, CancellationToken ct = default);
    Task<List<Provider>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Provider provider, CancellationToken ct = default);
    Task UpdateAsync(Provider provider, CancellationToken ct = default);
}
