using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Authorize(Roles = "Coach, Manager")]
    public class CoachController : Controller
    {
        // GET: CoachController
        public ActionResult Index()
        {
            return View();
        }

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
