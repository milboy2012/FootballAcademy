using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces;
using Domain.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;



namespace Domain.Model
{
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.Items["User"] as User;

            if (user == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var authService = context.HttpContext.RequestServices.GetService<IUserRepository>();

            // Проверяем, есть ли у пользователя хотя бы одна из требуемых ролей
            var hasRole = false;
            foreach (var role in _roles)
            {
                if (authService.UserHasRoleAsync(user.Id, role).GetAwaiter().GetResult())
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
