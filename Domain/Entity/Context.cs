using Domain.Configurations;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
            
        }


        //public DbSet<Achievement> Achievements { get; set; }
        //public DbSet<Attendance> Attendances { get; set; }
        //public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Coach> Coaches { get; set; }
        //public DbSet<Group> Groups { get; set; }
        //public DbSet<Message> Messages { get; set; }
        //public DbSet<Notification> Notifications { get; set; }
        //public DbSet<Parent> Parents { get; set; }
        //public DbSet<Payment> Payments { get; set; }
        //public DbSet<Player> Players { get; set; }
        //public DbSet<PlayerAchievement> PlayerAchievements { get; set; }
        //public DbSet<Schedule> Schedules { get; set; }
        //public DbSet<Score> Scores { get; set; }
        //public DbSet<Subscription> Subscriptions { get; set; }
        //public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<UserRole> Roles { get; set; }
        public DbSet<User> Users { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.ApplyConfiguration(new AchievementConfiguration());
            //modelBuilder.ApplyConfiguration(new AttendanceConfiguration());
            //modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new CoachConfiguration());
            //modelBuilder.ApplyConfiguration(new GroupConfiguration());
            //modelBuilder.ApplyConfiguration(new MessageConfiguration());
            //modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            //modelBuilder.ApplyConfiguration(new ParentConfiguration());
            //modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            //modelBuilder.ApplyConfiguration(new PlayerAchivementConfiguration());
            //modelBuilder.ApplyConfiguration(new PlayerConfiguration());
            //modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
            //modelBuilder.ApplyConfiguration(new ScoreConfiguration());
            //modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
            //modelBuilder.ApplyConfiguration(new TrainingSessionConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());

            // Настройка Identity таблиц
            //modelBuilder.Entity<IdentityUserClaim<int>>(entity => entity.ToTable("UserClaims"));
            //modelBuilder.Entity<IdentityUserLogin<int>>(entity => entity.ToTable("UserLogins"));
            //modelBuilder.Entity<IdentityUserToken<int>>(entity => entity.ToTable("UserTokens"));
            //modelBuilder.Entity<IdentityRoleClaim<int>>(entity => entity.ToTable("RoleClaims"));

            modelBuilder.Entity<User>(entity =>
            {
                // Наследуем Id от BaseEntity
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                // Связь с Role
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

                modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.NormalizedName).IsUnique();
                entity.HasIndex(e => e.Name).IsUnique();

                // Настройка BaseEntity свойств
                //entity.Property(e => e.CreatedAt)
                //    .HasDefaultValueSql("GETUTCDATE()")
                //    .ValueGeneratedOnAdd();
            });

            // Настройка Identity таблиц с кастомными именами
            //modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
            //{
            //    entity.ToTable("UserClaims");
            //    entity.HasKey(e => e.Id);
            //});

            //modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
            //{
            //    entity.ToTable("UserLogins");
            //    entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            //});

            //modelBuilder.Entity<IdentityUserToken<int>>(entity =>
            //{
            //    entity.ToTable("UserTokens");
            //    entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            //});

            //modelBuilder.Entity<IdentityRoleClaim<int>>(entity =>
            //{
            //    entity.ToTable("RoleClaims");
            //    entity.HasKey(e => e.Id);
            //});



            base.OnModelCreating(modelBuilder);
        }

        //Postgresql дополнительные настройки

    }
}
