using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class SubscriptionsController : Controller
    {
        public IActionResult Index() => View();
        
    }
}
