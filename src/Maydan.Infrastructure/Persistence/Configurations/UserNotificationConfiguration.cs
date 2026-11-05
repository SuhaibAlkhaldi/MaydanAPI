using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Infrastructure.Persistence.Configurations
{
    public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
    {
        public void Configure(EntityTypeBuilder<UserNotification> b)
        {
            b.ToTable("UserNotifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(100).IsRequired();
            b.Property(x => x.TitleAr).HasMaxLength(200); b.Property(x => x.TitleEn).HasMaxLength(200);
            b.Property(x => x.BodyAr).HasMaxLength(1000); b.Property(x => x.BodyEn).HasMaxLength(1000);
            b.Property(x => x.DataJson).HasMaxLength(2000);
            b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        }
    }

    public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
    {
        public void Configure(EntityTypeBuilder<UserDevice> b)
        {
            b.ToTable("UserDevices");
            b.HasKey(x => x.Id);
            b.Property(x => x.Token).HasMaxLength(512).IsRequired();
            b.Property(x => x.Platform).HasMaxLength(20);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => x.UserId);
        }
    }
}
