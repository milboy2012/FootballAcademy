using Core.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UI.Filters
{
    // Пользователя с временным паролем пускает только на смену пароля и выход
    public class MustChangePasswordFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { "Account/ChangePassword", "Account/Logout", "Account/Login", "Account/AccessDenied" };

        private readonly UserManager<AppUser> _userManager;
        public MustChangePasswordFilter(UserManager<AppUser> userManager) => _userManager = userManager;
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            if (http.User.Identity?.IsAuthenticated == true &&
                !http.Request.Path.StartsWithSegments("/api"))
            {
                var user = await _userManager.GetUserAsync(http.User);
                if (user?.MustChangePassword == true)
                {
                    http.Items["MustChangePassword"] = true;
                    var rd = context.RouteData.Values;
                    var key = $"{rd["controller"]}/{rd["action"]}";
                    if (!Allowed.Contains(key))
                    {
                        // Отдаём Home/Index с открытой модалкой, а не редиректим по кругу
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                        return;
                    }
                }
            }
            await next();
        }
    }
}
