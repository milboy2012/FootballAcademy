using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        public IActionResult Schedule() => View();
        public IActionResult Attendance(Guid id) => View(id);
        public IActionResult Progress(Guid id) => View(id);
    }
}
