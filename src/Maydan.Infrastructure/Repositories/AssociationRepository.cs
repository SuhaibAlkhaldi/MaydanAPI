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

    public async Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
      await _context.Associations
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

    public async Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Associations
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


    public async Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default) =>
        await _context.Associations
            .AsNoTracking()
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.CityId == cityId, cancellationToken);

   

    public async Task AddAsync(Association association, CancellationToken cancellationToken = default) =>
        await _context.Associations.AddAsync(association, cancellationToken);

    public void Remove(Association association) => _context.Associations.Remove(association);

    public async Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) =>
        await _context.Associations
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


    public async Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) =>
        await  _context.Associations
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
            ? _context.Associations
                .IgnoreQueryFilters()
                .Where(a => a.IsDeleted)
            : _context.Associations;

        query = query.AsNoTracking();

        // Search
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(a =>
                a.EnglishName.Contains(term) ||
                a.ArabicName.Contains(term));
        }

        // Select only the required columns
        var projected = query.Select(a => new
        {
            a.Id,
            a.ArabicName,
            a.EnglishName,
            a.CityId,

            CityArabicName = a.City.ArabicName,
            CityEnglishName = a.City.EnglishName,
            CountryId = a.City.CountryId,

            CountryArabicName = a.City.Country.ArabicName,
            CountryEnglishName = a.City.Country.EnglishName,

            a.LocationOnGoogleMaps,
            a.Latitude,
            a.Longitude,
            a.ContactPhone,
            a.ContactEmail,
            a.IsDeleted,

            WorkersCount = a.Workers.Count(w => !w.IsDeleted)
        });

        // Ordering
        var orderedQuery = orderByWorkersCountAscending switch
        {
            true => projected.OrderBy(x => x.WorkersCount),

            false => projected.OrderByDescending(x => x.WorkersCount),

            null => projected.OrderBy(x => x.EnglishName)
        };

        // Execute query
        var results = await orderedQuery
            .ToListAsync(cancellationToken);

        // Map the selected fields after SQL execution
        return results.Select(x => (
            Association: new Association
            {
                Id = x.Id,
                ArabicName = x.ArabicName,
                EnglishName = x.EnglishName,
                CityId = x.CityId,

                City = new City
                {
                    Id = x.CityId,
                    ArabicName = x.CityArabicName,
                    EnglishName = x.CityEnglishName,
                    CountryId = x.CountryId,

                    Country = new Country
                    {
                        Id = x.CountryId,
                        ArabicName = x.CountryArabicName,
                        EnglishName = x.CountryEnglishName
                    }
                },

                LocationOnGoogleMaps = x.LocationOnGoogleMaps,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                ContactPhone = x.ContactPhone,
                ContactEmail = x.ContactEmail,
                IsDeleted = x.IsDeleted
            },
            WorkersCount: x.WorkersCount
        )).ToList();
    }

}
