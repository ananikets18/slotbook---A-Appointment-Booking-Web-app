using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;

    public ServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Services
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Service>> GetByProviderIdAsync(Guid providerId, bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Services
            .Where(s => s.ProviderId == providerId);

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Service service, CancellationToken ct = default)
    {
        await _context.Services.AddAsync(service, ct);
    }

    public Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        _context.Services.Update(service);
        return Task.CompletedTask;
    }
}
