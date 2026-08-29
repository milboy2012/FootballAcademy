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
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            //таблица
            builder.ToTable("Payment");

            //ключ
            builder.HasKey(k=>k.Id);

            //свойства
            builder.ComplexProperty(k=>k.Amount, b =>
            {
                b.IsRequired();
                b.Property(p => p.Amount).HasColumnName("Amount");
                b.Property(p => p.Currency).HasColumnName("Currency");
            });            

            //связи

            //индексы

        }
    }
}
