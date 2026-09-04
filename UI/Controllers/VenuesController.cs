using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    public class VenuesController : Controller
    {
        public IActionResult Index() => View();
         
    }
}
