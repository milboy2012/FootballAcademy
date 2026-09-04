using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Configurations
{
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> b)
        {
            b.ToTable("Players");
            b.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            b.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            b.Property(p => p.Note).HasMaxLength(1000);

            b.HasOne(p => p.Parent).WithMany()
                .HasForeignKey(p => p.ParentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Group).WithMany(g => g.Players)
                .HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => new { p.LastName, p.FirstName });

            b.HasOne(p => p.User).WithOne()
                .HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(p => p.UserId).IsUnique();
        }
    }
}
