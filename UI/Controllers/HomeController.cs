using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Models;

namespace UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        //private readonly IUnitOfWork _data;
        private readonly IUoW _dat;

        public HomeController(ILogger<HomeController> logger, IUoW da)
        {
            _logger = logger;
            
            _dat = da;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var list = await _dat.Players.GetAllAsync(cancellationToken);            

            foreach (Player pl in list) { 
                
            }
            //User user = new User();
            //user.Email = Email.Create("admin@gmail.com");
            //user.FullName = FullName.Create("Иванов", "Иван", "Иванович");
            //user.Role = UserRole.Coach;
            //Coach coach = new Coach(user.Email, user.FullName, 10, PhoneNumber.Create("+375447401875"), null, null);

            //_data.Users.AddAsync(coach);

            
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
