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
    public class AbsenceNoticeConfiguration : IEntityTypeConfiguration<AbsenceNotice>
    {
        public void Configure(EntityTypeBuilder<AbsenceNotice> b)
        {
            b.ToTable("Attendances");
            b.Property(t => t.Comment).HasMaxLength(500);

            b.HasOne(t => t.Training).WithMany(g => g.AbsenceNotices)
                .HasForeignKey(t => t.TrainingId).OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.Player).WithMany(g => g.AbsenceNotices)
                .HasForeignKey(t => t.TrainingId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(t => new { t.PlayerId, t.TrainingId});
        }
    }
}
