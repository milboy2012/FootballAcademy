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
    public class VenueConfiguration : IEntityTypeConfiguration<Venue>
    {
        public void Configure(EntityTypeBuilder<Venue> b)
        {
            b.ToTable("Venues");
            b.Property(v => v.Name).HasMaxLength(200).IsRequired();
            b.Property(v => v.Address).HasMaxLength(500);

            b.Property(v => v.Description).HasMaxLength(1000); b.HasIndex(v => v.Name).IsUnique();
        }
    }
}
