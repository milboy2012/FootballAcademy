using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> b)
        {
            b.ToTable("Notifications");
            b.Property(a => a.Title).HasMaxLength(200);
            b.Property(a=>a.Message).HasMaxLength(2000);
            b.Property(a=>a.Link).HasMaxLength(500);

            b.HasIndex(a => new { a.UserId, a.ReadAt}).IsUnique();
        }
    }
}
