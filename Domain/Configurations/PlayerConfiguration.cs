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
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            //таблица
            builder.ToTable("Player");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(s => s.Scores)
                .WithOne(s => s.Player)
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(s => s.Attendances)
                .WithOne(s => s.Player)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(s => s.Group)
                .WithOne(s => s.Player);

            builder
                .HasMany(s => s.PlayerAchievements)
                .WithOne(s => s.Player)
                .HasForeignKey(f => f.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(s=>s.Messages)
                .WithOne(s=>s.Player)
                .HasForeignKey(s=>s.PlayerId)
                .OnDelete (DeleteBehavior.Cascade);

            builder
                .HasMany(s => s.Payments)
                .WithOne(s => s.Player)
                .HasForeignKey(f => f.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(s => s.Subscription)
                .WithOne(s => s.Player);
                

            //индексы
        }
    }
}
