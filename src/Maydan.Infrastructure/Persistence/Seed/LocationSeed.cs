using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Persistence.Seed;

// existed with zero rows (see LocationService's own header comment for the full story). This app
// is Jordan/Aqaba-specific today (ASEZA coordination, every existing test entity is Jordanian, and
// ProductionHouseSignupComponent/AssociationFormComponent both auto-select "Jordan"/"Amman" by
// capital shares its governorate's name in every case, and "City" is this schema's only
// instead). Same HasData-in-OnModelCreating convention as ProjectTypeSeed.Seed, migrated the same
// way (EF diffs the HasData against the existing empty tables and emits InsertData).
public static class LocationSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().HasData(
            new City { Id = 1, EnglishName = "Amman", ArabicName = "عمان", IsActive = true, IsDeleted = false },
            new City { Id = 2, EnglishName = "Irbid", ArabicName = "إربد", IsActive = true, IsDeleted = false },
            new City { Id = 3, EnglishName = "Zarqa", ArabicName = "الزرقاء", IsActive = true, IsDeleted = false },
            new City { Id = 4, EnglishName = "Balqa", ArabicName = "البلقاء", IsActive = true, IsDeleted = false },
            new City { Id = 5, EnglishName = "Madaba", ArabicName = "مأدبا", IsActive = true, IsDeleted = false },
            new City { Id = 6, EnglishName = "Karak", ArabicName = "الكرك", IsActive = true, IsDeleted = false },
            new City { Id = 7, EnglishName = "Tafilah", ArabicName = "الطفيلة", IsActive = true, IsDeleted = false },
            new City { Id = 8, EnglishName = "Ma'an", ArabicName = "معان", IsActive = true, IsDeleted = false },
            new City { Id = 9, EnglishName = "Aqaba", ArabicName = "العقبة", IsActive = true, IsDeleted = false },
            new City { Id = 10, EnglishName = "Jerash", ArabicName = "جرش", IsActive = true, IsDeleted = false },
            new City { Id = 11, EnglishName = "Ajloun", ArabicName = "عجلون", IsActive = true, IsDeleted = false },
            new City { Id = 12, EnglishName = "Mafraq", ArabicName = "المفرق", IsActive = true, IsDeleted = false }
        );
    }
}
