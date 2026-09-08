using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Core
{
    public class DbInitializer
    {
        public async static Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            var data = serviceProvider.GetRequiredService<ContextAuth>();

            // Создание ролей
            string[] roleNames = { "Manager", "Coach", "Parent", "Player" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    bool isAdmin = false;
                    if (roleName == "Manager" || roleName == "Coach")
                        isAdmin = true;

                    var role = new AppRole
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Description = roleName switch { 
                            "Manager" => "Менеджер",
                            "Coach" => "Тренер",
                            "Parent" => "Родитель(опекун)",
                            "Player" => "Ученик академии"
                        },
                        IsAdministration = isAdmin,
                        CreatedAt = DateTime.UtcNow,
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            // Создание администратора
            var adminUser = await userManager.FindByEmailAsync("manager@example.com");
            if (adminUser == null)
            {
                var user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "manager@example.com",
                    NormalizedUserName = "MANAGER@EXAMPLE.COM",
                    Email = "manager@example.com",
                    NormalizedEmail = "MANAGER@EXAMPLE.COM",
                    EmailConfirmed = true,
                    FirstName = "Manager",
                    LastName = "User",                    
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                IdentityResult result = new IdentityResult();
                try
                {
                    result = await userManager.CreateAsync(user, "Gjktnbkj22@");
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Manager");
                }               
                
            }

            
        }

        public static async Task InitializeEntityDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<ContextAuth>();
            string[] trainingPlanNames = { "Месяц", "3 месяца", "Год", "8 занятий", "16 занятий" };

            //заполняем типы подписок                    
            if (data.TrainingPlans.FirstOrDefault(s => s.Name == trainingPlanNames[0]) == null)
                data.TrainingPlans.Add(new TrainingPlan { Name = trainingPlanNames[0], Type = PlanType.Period, Period = PeriodUnit.Month, VisitsValidDays = 30, Price = 80, Description = "Абонемент на месяц", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.TrainingPlans.FirstOrDefault(s => s.Name == trainingPlanNames[1]) == null)
                data.TrainingPlans.Add(new TrainingPlan { Name = trainingPlanNames[1], Type = PlanType.Period, Period = PeriodUnit.Quarter, VisitsValidDays = 90, Price = 200, Description = "Абонемент на 3 месяца", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.TrainingPlans.FirstOrDefault(s => s.Name == trainingPlanNames[2]) == null)
                data.TrainingPlans.Add(new TrainingPlan { Name = trainingPlanNames[2], Type = PlanType.Period, Period = PeriodUnit.Year, VisitsValidDays = 365, Price = 600, Description = "Абонемент на год", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.TrainingPlans.FirstOrDefault(s => s.Name == trainingPlanNames[3]) == null)
                data.TrainingPlans.Add(new TrainingPlan { Name = trainingPlanNames[3], Type = PlanType.Visits, Period = PeriodUnit.Month, VisitsValidDays = 30, Price = 80, Description = "Абонемент на 8 занятий", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.TrainingPlans.FirstOrDefault(s => s.Name == trainingPlanNames[4]) == null)
                data.TrainingPlans.Add(new TrainingPlan { Name = trainingPlanNames[4], Type = PlanType.Visits, Period = PeriodUnit.Quarter, VisitsValidDays = 60, Price = 200, Description = "Абонемент на 16 занятий", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            //заполняем Skills
            string[] skillNames = { "Дриблинг", "Пас", "Удар", "Скорость", "Выносливость", "Игровое мышление", "Прием мяча" };
            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[0]) == null)
                data.Skills.Add(new Skill { Name = skillNames[0], Description = "Владение мячом", SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[1]) == null)
                data.Skills.Add(new Skill { Name = skillNames[1], Description = "Использование мяча", SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[2]) == null)
                data.Skills.Add(new Skill { Name = skillNames[2], Description = "Использование мяча", SortOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[3]) == null)
                data.Skills.Add(new Skill { Name = skillNames[3], Description = "Физические навыки", SortOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[4]) == null)
                data.Skills.Add(new Skill { Name = skillNames[4], Description = "Физические навыки", SortOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[5]) == null)
                data.Skills.Add(new Skill { Name = skillNames[5], Description = "Видение поля", SortOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            if (data.Skills.FirstOrDefault(s => s.Name == skillNames[6]) == null)
                data.Skills.Add(new Skill { Name = skillNames[6], Description = "Владение мячом", SortOrder = 7, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });

            data.SaveChanges();
        }
    }
}
