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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Таблица
            builder.ToTable("Users");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства
            builder.ComplexProperty(c => c.Email, b =>
            {
                b.IsRequired();
                b.Property(p => p.Value).HasColumnName("Email");
            });
            builder.ComplexProperty(c => c.Phone, b =>
            {
                b.IsRequired();
                b.Property(p => p.Value).HasColumnName("PhoneNumber");
            });
            builder.ComplexProperty(c => c.FullName, b =>
            {
                b.IsRequired();
                b.Property(p => p.Fam).HasColumnName("Fam");
                b.Property(p => p.Im).HasColumnName("Im");
                b.Property(p => p.Ot).HasColumnName("Ot");
            });

            //связи
            builder
                .HasMany(a=>a.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(f=>f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(s => s.Messages)
                .WithOne(s => s.Sender)
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(s => s.Player)
                .WithOne(s => s.User);

            builder
                .HasOne(s => s.Coach)
                .WithOne(s => s.User);

            builder
                .HasOne(s => s.Parent)
                .WithOne(s => s.User);

            builder
                .HasMany(s=>s.Notifications)
                .WithOne(s=>s.User)
                .HasForeignKey(s=>s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //индексы
        }
    }
}
