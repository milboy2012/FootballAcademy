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

        }
    }
}
