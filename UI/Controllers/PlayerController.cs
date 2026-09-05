using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Player, Manager")]
    public class PlayerController : Controller
    {
        public IActionResult Index() => View();
        
    }
}
