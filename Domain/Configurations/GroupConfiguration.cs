using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            //таблица
            builder.ToTable("Group");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(t => t.TrainingSessions)
                .WithOne(g => g.Group)
                .HasForeignKey(f=>f.GroupId).OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(s=>s.Schedules)
                .WithOne(s=>s.Group)
                .HasForeignKey(f=>f.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            //индексы
        }
    }
}
