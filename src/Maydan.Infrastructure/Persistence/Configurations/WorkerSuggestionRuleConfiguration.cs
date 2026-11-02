
using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations
{
    public class WorkerSuggestionRuleConfiguration
        : IEntityTypeConfiguration<WorkerSuggestionRule>
    {
        public void Configure(
            EntityTypeBuilder<WorkerSuggestionRule> builder)
        {
            builder.ToTable("WorkerSuggestionRules");

            builder.HasKey(x => x.Id);

            // Attribute Key from WorkerAttributeCatalog
            builder.Property(x => x.AttributeKey)
                .IsRequired()
                .HasMaxLength(100);

            // Filter Comparison Value
            builder.Property(x => x.ComparisonValue)
                .HasMaxLength(500);

            // Rule Priority
            builder.Property(x => x.PriorityOrder)
                .IsRequired();

            // Active Status
            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            // Index for retrieving active rules by priority
            builder.HasIndex(x => new
            {
                x.IsActive,
                x.PriorityOrder
            });

            // Rule Type (Filter / Sort)
            builder.HasOne(x => x.RuleType)
                .WithMany()
                .HasForeignKey(x => x.RuleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comparison Operator
            builder.HasOne(x => x.Operator)
                .WithMany()
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sort Direction
            builder.HasOne(x => x.SortDirection)
                .WithMany()
                .HasForeignKey(x => x.SortDirectionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comparison Lookup Item
            builder.HasOne(x => x.ComparisonLookupItem)
                .WithMany()
                .HasForeignKey(x => x.ComparisonLookupItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
