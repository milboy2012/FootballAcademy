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
    public class CoachConfiguration : IEntityTypeConfiguration<Coach>
    {
        public void Configure(EntityTypeBuilder<Coach> b)
        {
            b.ToTable("Coaches");
            b.Property(c => c.Bio).HasMaxLength(2000);
            b.Property(c => c.Qualification).HasMaxLength(200);

            b.HasOne(c => c.User).WithOne()
                .HasForeignKey<Coach>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(c => c.UserId).IsUnique();
        }
    }
}
