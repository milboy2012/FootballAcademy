using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        public ManagerController(IUoW data, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _data = data;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> AllUsers(CancellationToken cancellationToken)
        {
            Player player = new Player
            {
                FirstName = "Иванов",
                LastName = "Иван",
                BirthDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Email = "parent@example.com",
                UserName = "parent",
                
                IsActive = true
            };


            var result = await _userManager.CreateAsync(player, "Gjktnbkj22@");
            if (result.Succeeded) {
                _userManager.AddToRoleAsync(player, "Player");
            }
            _data.SaveEntitiesAsync(cancellationToken);


            var data = await _data.Players.GetAllAsync(cancellationToken);
            return View(data);
        }

        public IActionResult AllGroups()
        {
            //var data = _data.Childs.
            return View();
        }

        public IActionResult AllCoaches()
        {
            //var data = _data.Childs.
            return View();
        }
    }
}
