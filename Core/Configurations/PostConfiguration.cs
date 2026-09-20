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
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> b)
        {
            b.ToTable("Posts");

            b.HasKey(p => p.Id);

            b.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

            b.Property(p => p.Body)
                .IsRequired()
                .HasColumnType("nvarchar(max)"); // или "text" для PostgreSQL

            b.Property(p => p.ImagePath)
                .HasMaxLength(500);

            b.Property(p => p.Location)
                .HasMaxLength(200);

            // Enum как строки
            b.Property(p => p.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            b.Property(p => p.Audience)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Индексы
            b.HasIndex(p => new { p.IsPublished, p.IsPinned, p.PublishedAt })
                .HasDatabaseName("IX_Posts_Published_Pinned_Date");

            // Отношения
            b.HasOne(p => p.Author)
                //.WithMany() 
                // или .WithMany(u => u.Posts) если есть навигационное свойство
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Group)
                .WithMany(g => g.Posts) // предполагаем, что у TrainingGroup есть коллекция Posts
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
