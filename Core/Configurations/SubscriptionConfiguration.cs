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
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> b)
        {
            b.ToTable("Subscriptions");
            b.Property(s => s.Price).HasPrecision(12, 2);
            b.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            b.HasOne(s => s.Player).WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlayerId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(s => new { s.PlayerId, s.To });
        }
    }
}
