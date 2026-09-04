using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        public IActionResult Index() => View();
    }
}
