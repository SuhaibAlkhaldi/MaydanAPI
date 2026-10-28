using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Persistence.Seed;

public static class LookupSeed
{
    public const int GenderCategoryId = 1;
    public const int MaritalStatusCategoryId = 2;

    public const int GenderMaleItemId = 1;
    public const int GenderFemaleItemId = 2;

    public const int MaritalStatusSingleItemId = 3;
    public const int MaritalStatusMarriedItemId = 4;
    public const int MaritalStatusDivorcedItemId = 5;
    public const int MaritalStatusWidowedItemId = 6;

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LookupCategory>().HasData(
            new LookupCategory { Id = GenderCategoryId, Code = "Gender", ArabicName = "الجنس", EnglishName = "Gender", IsActive = true, IsDeleted = false },
            new LookupCategory { Id = MaritalStatusCategoryId, Code = "MaritalStatus", ArabicName = "الحالة الاجتماعية", EnglishName = "Marital Status", IsActive = true, IsDeleted = false }
        );

        modelBuilder.Entity<LookupItem>().HasData(
            new LookupItem { Id = GenderMaleItemId, CategoryId = GenderCategoryId, Code = "Male", ArabicName = "ذكر", EnglishName = "Male", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = GenderFemaleItemId, CategoryId = GenderCategoryId, Code = "Female", ArabicName = "أنثى", EnglishName = "Female", SortOrder = 2, IsActive = true, IsDeleted = false },

            new LookupItem { Id = MaritalStatusSingleItemId, CategoryId = MaritalStatusCategoryId, Code = "Single", ArabicName = "أعزب", EnglishName = "Single", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusMarriedItemId, CategoryId = MaritalStatusCategoryId, Code = "Married", ArabicName = "متزوج", EnglishName = "Married", SortOrder = 2, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusDivorcedItemId, CategoryId = MaritalStatusCategoryId, Code = "Divorced", ArabicName = "مطلق", EnglishName = "Divorced", SortOrder = 3, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusWidowedItemId, CategoryId = MaritalStatusCategoryId, Code = "Widowed", ArabicName = "أرمل", EnglishName = "Widowed", SortOrder = 4, IsActive = true, IsDeleted = false }
        );
    }
}
