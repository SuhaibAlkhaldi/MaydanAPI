
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

using System.Reflection;

namespace Maydan.Infrastructure.Persistence;

public class MaydanDbContext : DbContext
{
    public MaydanDbContext(DbContextOptions<MaydanDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<CityLocation> CityLocations => Set<CityLocation>();
    public DbSet<ProjectType> ProjectTypes { get; set; }
    public DbSet<Association> Associations => Set<Association>();
    public DbSet<AssociationProjectSupervisor> AssociationProjectSupervisors => Set<AssociationProjectSupervisor>();
    public DbSet<ProductionCompany> ProductionCompanies => Set<ProductionCompany>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerServiceLink> WorkerServiceLinks => Set<WorkerServiceLink>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaydanDbContext).Assembly);

        ProjectTypeSeed.Seed(modelBuilder);
        LocationSeed.Seed(modelBuilder);

        // Global soft-delete filter for every SharedEntities (IsDeleted set by SaveChangesAsync below).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(SharedEntities).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(MaydanDbContext)
                .GetMethod(nameof(BuildSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)method.Invoke(null, null)!);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<SharedEntities>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Deleted:
                    if (entry.Entity is UserGroup)
                    {
                        break;
                    }

                    // Soft delete: never hard-delete an SharedEntities.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = utcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static Expression<Func<TEntity, bool>> BuildSoftDeleteFilter<TEntity>() where TEntity : SharedEntities =>
        entity => !entity.IsDeleted;
}
