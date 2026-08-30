using Domain.Entity;
using Domain.Interfaces;
using Domain.Repositories;
using Domain.Repositories.Interfaces;
using Domain.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UI.Data;

namespace UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            //builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<Context>();
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            //строка подключения к базе данных postgresql
            var pgconnectionString = builder.Configuration.GetConnectionString("pgconnectionString");

            builder.Services.AddDbContext<Context>(options => options.UseNpgsql(pgconnectionString, npgsqlOptions =>
            {
                //настройка миграции
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");

                // Включение поддержки UUID (разобрать)
                //npgsqlOptions.UseNetTopologySuite(); // Для геоданных
            }));


            // Регистрация UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Регистрация репозиториев
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
            builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<ICoachRepository, CoachRepository>();
            builder.Services.AddScoped<IGroupRepository,  GroupRepository>();
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IParentRepository, ParentRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IPlayerAchievementRepository, PlayerAchievementRepository>();
            builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
            builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
            builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            builder.Services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();


            var app = builder.Build();

            //подключение к базе данных
            

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
