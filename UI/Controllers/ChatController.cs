using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Manager,Coach,Parent,Player")]
    public class ChatController : Controller { 
        public IActionResult Index(Guid? group) { 
            ViewBag.Group = group; 
            return View(); 
        } 
    }
}
