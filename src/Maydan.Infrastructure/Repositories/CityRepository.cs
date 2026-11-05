using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly MaydanDbContext _context;

    public CityRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<City?> GetByIdAsync(int cityId, CancellationToken cancellationToken = default) =>
        _context.Cities.FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

    public Task<City?> GetByIdIncludingDeletedAsync(int cityId, CancellationToken cancellationToken = default) =>
        _context.Cities.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

    public Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Cities.IgnoreQueryFilters().AsNoTracking().OrderBy(c => c.EnglishName).ToListAsync(cancellationToken);

    public async Task AddAsync(City city, CancellationToken cancellationToken = default) =>
        await _context.Cities.AddAsync(city, cancellationToken);

    public void Remove(City city) => _context.Cities.Remove(city);
}
