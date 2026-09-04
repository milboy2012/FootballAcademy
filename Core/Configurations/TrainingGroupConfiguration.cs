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
    public class TrainingGroupConfiguration : IEntityTypeConfiguration<TrainingGroup>
    {
        public void Configure(EntityTypeBuilder<TrainingGroup> b)
        {
            b.ToTable("TrainingGroups");
            b.Property(g => g.Name).HasMaxLength(50).IsRequired();
            b.Property(g => g.Color).HasMaxLength(7);
            b.HasIndex(g => g.Name).IsUnique();

            b.HasOne(g => g.Coach).WithMany(c => c.Groups)
                .HasForeignKey(g => g.CoachId).OnDelete(DeleteBehavior.Restrict);

            //конфигурация для уникальности имени — только среди неархивных (в новом сезоне «U-8» создаётся заново)
            b.HasIndex(g => g.Name).IsUnique().HasFilter("\"IsArchived\" = false AND \"IsDeleted\" = false");
            b.Property(g => g.Season).HasMaxLength(20);
            b.Property(g => g.Description).HasMaxLength(1000);
        }
    }
}
