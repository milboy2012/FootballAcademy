using Core.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Models.ViewModels.Login;


namespace UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                //ищем по email или по логину(если это ребенок)
                var user = await _userManager.FindByEmailAsync(model.Email) 
                    ?? await _userManager.FindByNameAsync(model.Email);

                if (user == null || !user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                    return View(model);
                }
                if (!user.IsActive) { 
                    ModelState.AddModelError("", "Учётная запись заблокирована. Обратитесь к администрации академии."); 
                    return View(model); 
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (!string.IsNullOrEmpty(returnUrl)) return RedirectToLocal(returnUrl);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Player")) return RedirectToAction("Index", "My");
                    if (roles.Contains("Parent")) return RedirectToAction("Index", "Cabinet");
                    if (roles.Contains("Coach")) return RedirectToAction("Index", "Coach");
                }
                

                //return RedirectToAction("Index", "Home");

                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel();
            await LoadRoles(model);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };                
                

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Parent");                    

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            await LoadRoles(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var redirect = roles.Contains("Coach") ? Url.Action("Index", "Schedule") : Url.Action("Index", "Home");
            return Ok(new { redirect });
        }

        private async Task LoadRoles(RegisterViewModel model)
        {
            var rols = _roleManager.Roles.Where(s=>!s.IsAdministration).ToList();
            List<RoleViewModel> roles = new List<RoleViewModel>();
            foreach (var it in rols)
            {
                RoleViewModel add = new RoleViewModel();
                add.Name = it.Name;
                add.Description = it.Description;
                add.IsSelected = false;
                roles.Add(add);
            }

            //var roles = await _roleManager.Roles.Select(r => new RoleViewModel
            //{
            //    Name = r.Name,
            //    Description = r.Description,
            //    IsSelected = false
            //}).ToListAsync();

            model.Roles = roles;
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
