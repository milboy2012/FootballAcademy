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
    public class TrainingConfiguration : IEntityTypeConfiguration<Training>
    {
        public void Configure(EntityTypeBuilder<Training> b)
        {
            b.ToTable("Trainings");
            b.Property(t => t.Note).HasMaxLength(1000);
            b.Property(t => t.CancelReason).HasMaxLength(500);
            b.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(t => t.Kind).HasConversion<string>().HasMaxLength(20);

            // Основная группа — с обратной коллекцией
            b.HasOne(t => t.Group).WithMany(g => g.Trainings)
                .HasForeignKey(t => t.GroupId).OnDelete(DeleteBehavior.Cascade);

            // Соперник — БЕЗ обратной навигации
            b.HasOne(t => t.OpponentGroup).WithMany().HasForeignKey(t => t.OpponentGroupId).OnDelete(DeleteBehavior.Restrict);


            b.HasOne(t => t.Venue).WithMany(v => v.Trainings)
                .HasForeignKey(t => t.VenueId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(t => new { t.StartsAt, t.EndsAt });     // выборка диапазона для календаря
            b.HasIndex(t => new { t.VenueId, t.StartsAt });    // проверка пересечений по полю

            
            
            
            b.HasIndex(t => t.SeriesId);
        }
    }
}
