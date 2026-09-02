using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class ContextAuth : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public ContextAuth(DbContextOptions<ContextAuth> options)
            : base(options)
        {
        }
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<Player> Children { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Manager> Managers{ get; set; }
        public DbSet<Parent> Parents{ get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Player>(entity =>
            {
                entity.HasBaseType<AppUser>();
            });
            builder.Entity<Parent>(entity =>
            {
                entity.HasBaseType<AppUser>();
            });
            builder.Entity<Manager>(entity =>
            {
                entity.HasBaseType<AppUser>();
            });
            builder.Entity<Coach>(entity =>
            {
                entity.HasBaseType<AppUser>();
            });

            // Переименование таблиц
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<AppRole>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            // Настройка связей (опционально)
            builder.Entity<IdentityUserRole<Guid>>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Настройка индексов
            builder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<AppUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            builder.Entity<AppRole>()
                .HasIndex(r => r.NormalizedName)
                .IsUnique();

        }
    }
}
