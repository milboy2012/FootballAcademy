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
    public class CoachConfiguration : IEntityTypeConfiguration<Coach>
    {
        public void Configure(EntityTypeBuilder<Coach> builder)
        {
            //таблица
            builder.ToTable("Coach");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(t => t.TrainingSessions)
                .WithOne(c => c.Coach)
                .HasForeignKey(f => f.CoachId);

            builder
                .HasMany(s=>s.Players)
                .WithOne(s=>s.Coach)
                .HasForeignKey(s=>s.CoachId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder
                .HasMany(s=>s.Groups)
                .WithOne(s=>s.Coach)
                .HasForeignKey(s=>s.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasMany(s => s.Schedules)
                .WithOne(s => s.Coach)
                .HasForeignKey(s => s.CoachId)
                .OnDelete(DeleteBehavior.Cascade);


            //индексы
        }
    }
}
