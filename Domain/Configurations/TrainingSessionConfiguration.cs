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


    public class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
    {
        public void Configure(EntityTypeBuilder<TrainingSession> builder)
        {
            //таблица
            builder.ToTable("TrainingSession");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(s => s.Attendances)
                .WithOne(s => s.TrainingSession)
                .HasForeignKey(s => s.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(s => s.Score)
                .WithOne(s => s.TrainingSession);

            builder
                .HasOne(s=>s.Schedule)
                .WithOne(s => s.TrainigSession);
                
                


            //индексы
        }
    }
}
