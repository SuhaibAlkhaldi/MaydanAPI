using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class ProductionCompanyRepository : IProductionCompanyRepository
{
    private readonly MaydanDbContext _context;

    public ProductionCompanyRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<ProductionCompany?> GetByIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) =>
        _context.ProductionCompanies
            .Include(p => p.City)
            .FirstOrDefaultAsync(p => p.Id == productionCompanyId, cancellationToken);

    public Task<List<ProductionCompany>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.ProductionCompanies.ToListAsync(cancellationToken);

    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default) =>
        _context.ProductionCompanies.AnyAsync(p => p.RegistrationNumber == registrationNumber, cancellationToken);

    public Task<bool> EnglishNameExistsAsync(string englishName, CancellationToken cancellationToken = default) =>
        _context.ProductionCompanies.AnyAsync(p => p.EnglishName == englishName, cancellationToken);

    public Task<List<ProductionCompany>> QueryAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IQueryable<ProductionCompany> query = _context.ProductionCompanies
            .AsNoTracking()
            .Include(p => p.City);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p => p.EnglishName.Contains(term) || p.ArabicName.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return query.OrderBy(p => p.EnglishName).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductionCompany productionCompany, CancellationToken cancellationToken = default) =>
        await _context.ProductionCompanies.AddAsync(productionCompany, cancellationToken);

    public void Remove(ProductionCompany productionCompany) => _context.ProductionCompanies.Remove(productionCompany);
}
