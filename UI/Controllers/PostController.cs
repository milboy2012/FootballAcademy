using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class PostController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
