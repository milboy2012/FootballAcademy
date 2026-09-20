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
    public class ParentProfileConfiguration : IEntityTypeConfiguration<ParentProfile>
    {
        public void Configure(EntityTypeBuilder<ParentProfile> b)
        {
            b.ToTable("ParentProfiles");

            b.HasOne(pp => pp.User)
            .WithOne(p=>p.ParentProfile) // Если у AppUser нет свойства ParentProfile
            .HasForeignKey<ParentProfile>(pp => pp.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление

            b.HasIndex(u => u.UserId).IsUnique();
        }
    }
}
