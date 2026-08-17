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
    public class PlayerAchivementConfiguration : IEntityTypeConfiguration<PlayerAchievement>
    {
        public void Configure(EntityTypeBuilder<PlayerAchievement> builder)
        {
            // Таблица
            builder.ToTable("PlayerAchievement");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства
            builder.ComplexProperty(c => c.Achievement, b =>
            {
                b.IsRequired();
                b.Property(p => p.Name).HasColumnName("Name");
                b.Property(p => p.Description).HasColumnName("Description");
            });
        }
    }
}
