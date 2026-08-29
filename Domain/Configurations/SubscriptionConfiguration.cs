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
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            //таблица
            builder.ToTable("Subscription");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            //свойства

            //связи
            builder
                .HasMany(s => s.Payments)
                .WithOne(s => s.Subscription)
                .HasForeignKey(s => s.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            //индексы
        }
    }
}
