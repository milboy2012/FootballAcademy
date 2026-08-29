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
    public class ParentConfiguration : IEntityTypeConfiguration<Parent>
    {
        public void Configure(EntityTypeBuilder<Parent> builder)
        {
            //таблица
            builder.ToTable("Parent");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(p => p.Children)
                .WithOne(s => s.Parent)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(s => s.Payments)
                .WithOne(s => s.Parent)
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(s => s.Subscription)
                .WithOne(s => s.Parent);

            builder
                .HasOne(s => s.User)
                .WithOne(s => s.Parent);



            //индексы
        }
    }
}
