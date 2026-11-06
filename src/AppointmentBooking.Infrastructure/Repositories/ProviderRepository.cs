using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Repositories;

public class ProviderRepository : IProviderRepository
{
    private readonly AppDbContext _context;

    public ProviderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Providers
            .Include(p => p.User)
            .Include(p => p.Services)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Provider?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Providers
            .Include(p => p.User)
            .Include(p => p.Services)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task<List<Provider>> GetAllActiveAsync(string? category = null, CancellationToken ct = default)
    {
        var query = _context.Providers
            .Include(p => p.User)
            .Include(p => p.Services.Where(s => s.IsActive))
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        return await query.OrderBy(p => p.BusinessName).ToListAsync(ct);
    }

    public async Task<List<Provider>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Providers
            .Include(p => p.User)
            .Include(p => p.Services)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Provider provider, CancellationToken ct = default)
    {
        await _context.Providers.AddAsync(provider, ct);
    }

    public Task UpdateAsync(Provider provider, CancellationToken ct = default)
    {
        _context.Providers.Update(provider);
        return Task.CompletedTask;
    }
}
