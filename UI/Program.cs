using Core;
using Core.Entity;
using Core.Interfaces;
using Core.Options;
using Core.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UI.Filters;
using UI.Services;
using UI.Services.Interfaces;
using UI.Services.Options;


namespace UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            //builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            //builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<Context>();
            //builder.Services.AddAuthorization();
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            //строка подключения к базе данных postgresql
            var pgconnectionString = builder.Configuration.GetConnectionString("pgconnectionString");

            //builder.Services.AddDbContext<Context>(options => options.UseNpgsql(pgconnectionString, npgsqlOptions =>
            //{
            //    //настройка миграции
            //    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");

            //    // Включение поддержки UUID (разобрать)
            //    //npgsqlOptions.UseNetTopologySuite(); // Для геоданных
            //}));


            var pgconnectionStringAuth = builder.Configuration.GetConnectionString("pgconnectionStringAuth");

            builder.Services.AddDbContext<ContextAuth>(options => options.UseNpgsql(pgconnectionStringAuth, npgsqlOptions =>
            {
                //настройка миграции
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");

                // Включение поддержки UUID (разобрать)
                //npgsqlOptions.UseNetTopologySuite(); // Для геоданных
            }));

            // Регистрация UnitOfWork
            //builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Регистрация репозиториев
            //builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            //builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
            //builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            //builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            //builder.Services.AddScoped<ICoachRepository, CoachRepository>();
            //builder.Services.AddScoped<IGroupRepository,  GroupRepository>();
            //builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            //builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            //builder.Services.AddScoped<IParentRepository, ParentRepository>();
            //builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            //builder.Services.AddScoped<IPlayerAchievementRepository, PlayerAchievementRepository>();
            //builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
            //builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
            //builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            //builder.Services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
            //builder.Services.AddScoped<IUserRepository, UserRepository>();

            //Core
            builder.Services.AddScoped<IUoW, UoW>();
            builder.Services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));

            //UI
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IPlayerAccountService, PlayerAccountService>();
            builder.Services.AddScoped<ICoachOnboardingService, CoachOnboardingService>();
            builder.Services.AddScoped<IGroupService, GroupService>();
            builder.Services.AddScoped<ICoachTrainingService, CoachTrainingService>();
            builder.Services.AddScoped<IParentService, ParentService>();
            builder.Services.AddScoped<IPlayerCabinetService, PlayerCabinetService>();



            // Настройка Identity с кастомными моделями
            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                // Настройки пароля
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Настройки блокировки
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Настройки пользователя
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ContextAuth>()
            .AddDefaultTokenProviders();

            // Настройка Cookie аутентификации
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;

                //для /api/* вместо редиректа на страницу логина возвращался 401 (иначе Tabulator получит HTML)
                options.Events.OnRedirectToLogin = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            //Фильтрация, если у пользователя признак временного пароля перенаправляем его на смену
            builder.Services.AddScoped<MustChangePasswordFilter>();
            builder.Services.AddControllersWithViews(o => o.Filters.AddService<MustChangePasswordFilter>());

            //Блокировка активной сессии время
            builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

            //------------JWT-
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                      ?? throw new InvalidOperationException("Секция Jwt не найдена");

            builder.Services.AddScoped<ITokenService, TokenService>();

            builder.Services.AddAuthentication(options =>
            {
                // «умная» схема: смотрит, есть ли Bearer-заголовок, иначе — cookie Identity
                options.DefaultScheme = "CookieOrJwt";
                options.DefaultChallengeScheme = "CookieOrJwt";
            })
            .AddPolicyScheme("CookieOrJwt", "Cookie or JWT", options =>
            {
                options.ForwardDefaultSelector = ctx =>
                    ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : IdentityConstants.ApplicationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });


            var app = builder.Build();

            // Инициализация базы данных
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ContextAuth>();
                //await context.Database.MigrateAsync();
                await DbInitializer.Initialize(services);
            }


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
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
