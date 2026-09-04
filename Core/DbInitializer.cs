using Core.Entity;
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
    }
}
