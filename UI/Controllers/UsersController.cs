using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class UsersController : Controller { 
        public IActionResult Index() => View(); 
    }
}
