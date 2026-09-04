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
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> b)
        {
            b.ToTable("Attendances");
            b.Property(a => a.Comment).HasMaxLength(500);

            b.HasOne(a => a.Training).WithMany(t => t.Attendances)
                .HasForeignKey(a => a.TrainingId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(a => a.Player).WithMany(p => p.Attendances)
                .HasForeignKey(a => a.PlayerId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(a => new { a.TrainingId, a.PlayerId }).IsUnique();
        }
    }
}
