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
        //public DbSet<Coach> Coaches { get; set; }
        //public DbSet<Player> Children { get; set; }
        //public DbSet<Group> Groups { get; set; }
        //public DbSet<Manager> Managers{ get; set; }
        //public DbSet<Parent> Parents{ get; set; }

        public DbSet<AbsenceNotice> AbsenceNotices=> Set<AbsenceNotice>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Coach> Coaches => Set<Coach>();
        
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
        public DbSet<SkillScore> SkillScores => Set<SkillScore>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<TrainingGroup> Groups => Set<TrainingGroup>();
        public DbSet<Venue> Venues => Set<Venue>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<Player>(entity =>
            //{
            //    entity.HasBaseType<AppUser>();
            //});
            //builder.Entity<Parent>(entity =>
            //{
            //    entity.HasBaseType<AppUser>();
            //});
            //builder.Entity<Manager>(entity =>
            //{
            //    entity.HasBaseType<AppUser>();
            //});
            //builder.Entity<Coach>(entity =>
            //{
            //    entity.HasBaseType<AppUser>();
            //});

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

            builder.Entity<AppUser>().Property(u => u.FirstName).HasMaxLength(100);
            builder.Entity<AppUser>().Property(u => u.LastName).HasMaxLength(100);
            builder.Entity<AppRole>().Property(r => r.Description).HasMaxLength(500);

            // Общие настройки для всех BaseEntity: UUID из Postgres + фильтр мягкого удаления
            foreach (var entityType in builder.Model.GetEntityTypes()
                         .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .HasDefaultValueSql("gen_random_uuid()");

                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Not(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
                builder.Entity(entityType.ClrType)
                    .HasQueryFilter(System.Linq.Expressions.Expression.Lambda(body, parameter));
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        break;
                    case EntityState.Deleted:           // мягкое удаление вместо физического
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = now;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
