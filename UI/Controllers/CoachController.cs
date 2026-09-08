using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UI.Models.ViewModels.Player;

namespace UI.Controllers
{
    
    public class CoachController : Controller
    {
        [Authorize(Roles = "Manager")]
        public ActionResult Index() => View();
        [Authorize(Roles = "Coach")]
        public ActionResult CoachTraining() => View();
        [Authorize(Roles = "Manager, Coach")]
        public ActionResult Training(Guid id) => View(id);
        

















        // GET: CoachController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CoachController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CoachController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CoachController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: CoachController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CoachController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CoachController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
