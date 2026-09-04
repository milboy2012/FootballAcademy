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
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> b)
        {
            b.ToTable("Payments");
            b.Property(p => p.Amount).HasPrecision(12, 2);
            b.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.Comment).HasMaxLength(500);

            b.HasOne(p => p.Subscription).WithMany(s => s.Payments)
                .HasForeignKey(p => p.SubscriptionId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(p => p.PaidAt);
        }
    }
}
