using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class AssociationRepository : IAssociationRepository
{
    private readonly MaydanDbContext _context;

    public AssociationRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
        .AsNoTracking()
            .Select(a => new Association
            {
                Id = a.Id,
                ArabicName = a.ArabicName,
                EnglishName = a.EnglishName,
                City = new City
                {
                    ArabicName = a.City.ArabicName,
                    EnglishName = a.City.EnglishName,
                    Country = new Country
                    {
                        ArabicName = a.City.Country.ArabicName,
                        EnglishName = a.City.Country.EnglishName
                    }
                },
                LocationOnGoogleMaps = a.LocationOnGoogleMaps,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                ContactPhone = a.ContactPhone,
                ContactEmail = a.ContactEmail
            })
            //.Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Associations
        .AsNoTracking()
        .Select(a => new Association
        {
            Id = a.Id,
            ArabicName = a.ArabicName,
            EnglishName = a.EnglishName,
            City = new City
            {
                ArabicName = a.City.ArabicName,
                EnglishName = a.City.EnglishName,
                Country = new Country
                {
                    ArabicName = a.City.Country.ArabicName,
                    EnglishName = a.City.Country.EnglishName
                }
            },
            LocationOnGoogleMaps = a.LocationOnGoogleMaps,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            ContactPhone = a.ContactPhone,
            ContactEmail = a.ContactEmail
        })
        //.Include(a => a.City).ThenInclude(c => c.Country)
        .ToListAsync(cancellationToken);

    public async Task AddAsync(Association association, CancellationToken cancellationToken = default) =>
        await _context.Associations.AddAsync(association, cancellationToken);

    public void Remove(Association association) => _context.Associations.Remove(association);

    public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(a => new Association
            {
                Id = a.Id,
                ArabicName = a.ArabicName,
                EnglishName = a.EnglishName,
                City = new City
                {
                    ArabicName = a.City.ArabicName,
                    EnglishName = a.City.EnglishName,
                    Country = new Country
                    {
                        ArabicName = a.City.Country.ArabicName,
                        EnglishName = a.City.Country.EnglishName
                    }
                },
                LocationOnGoogleMaps = a.LocationOnGoogleMaps,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                ContactPhone = a.ContactPhone,
                ContactEmail = a.ContactEmail
            })
            //.Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
        .AsNoTracking()
        .Select(a => new Association
        {
            Id = a.Id,
            ArabicName = a.ArabicName,
            EnglishName = a.EnglishName,
            City = new City
            {
                ArabicName = a.City.ArabicName,
                EnglishName = a.City.EnglishName,
                Country = new Country
                {
                    ArabicName = a.City.Country.ArabicName,
                    EnglishName = a.City.Country.EnglishName
                }
            },
            LocationOnGoogleMaps = a.LocationOnGoogleMaps,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            ContactPhone = a.ContactPhone,
            ContactEmail = a.ContactEmail,
            Workers = a.Workers.Select(w => new Worker
            {
                Id = w.Id,
                FirstName = w.FirstName,
                MiddleName = w.MiddleName,
                LastName = w.LastName, 
                AssociationId = w.AssociationId
            }).ToList()
        })
        //.Include(a => a.City).ThenInclude(c => c.Country)
        //.Include(a => a.Workers)
        .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public async Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default)
    {
        var result = await _context.Associations
        .AsNoTracking()
         .AsNoTracking()
            .Select(a => new Association
            {
                Id = a.Id,
                ArabicName = a.ArabicName,
                EnglishName = a.EnglishName,
                City = new City
                {
                    ArabicName = a.City.ArabicName,
                    EnglishName = a.City.EnglishName,
                    Country = new Country
                    {
                        ArabicName = a.City.Country.ArabicName,
                        EnglishName = a.City.Country.EnglishName
                    }
                },
                LocationOnGoogleMaps = a.LocationOnGoogleMaps,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                ContactPhone = a.ContactPhone,
                ContactEmail = a.ContactEmail
            })
            //.Include(a => a.City).ThenInclude(c => c.Country)
            .Where(a => a.Id == associationId)
            .Select(a => new { Association = a, WorkersCount = a.Workers.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? null : (result.Association, result.WorkersCount);
    }

    public async Task<List<(Association Association, int WorkersCount)>> QueryAsync(
        bool isDeleted,
        string? searchTerm = null,
        bool? orderByWorkersCountAscending = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Association> query = isDeleted
            ? _context.Associations.IgnoreQueryFilters().Where(a => a.IsDeleted)
            : _context.Associations;

        query = query
            .AsNoTracking()
            .Select(a => new Association
            {
                Id = a.Id,
                ArabicName = a.ArabicName,
                EnglishName = a.EnglishName,
                City = new City
                {
                    ArabicName = a.City.ArabicName,
                    EnglishName = a.City.EnglishName,
                    Country = new Country
                    {
                        ArabicName = a.City.Country.ArabicName,
                        EnglishName = a.City.Country.EnglishName
                    }
                },
                LocationOnGoogleMaps = a.LocationOnGoogleMaps,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                ContactPhone = a.ContactPhone,
                ContactEmail = a.ContactEmail
            });

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(a => a.EnglishName.Contains(term) || a.ArabicName.Contains(term));
        }

        var projected = query.Select(a => new { Association = a, WorkersCount = a.Workers.Count() });

        var results = orderByWorkersCountAscending switch
        {
            true => await projected.OrderBy(x => x.WorkersCount).ToListAsync(cancellationToken),
            false => await projected.OrderByDescending(x => x.WorkersCount).ToListAsync(cancellationToken),
            null => await projected.OrderBy(x => x.Association.EnglishName).ToListAsync(cancellationToken)
        };

        return results.Select(x => (x.Association, x.WorkersCount)).ToList();
    }

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) =>
        await _context.Associations
            .AnyAsync(a => a.ContactPhone == phone, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Associations
            .AnyAsync(a => a.ContactEmail == email, cancellationToken);
}