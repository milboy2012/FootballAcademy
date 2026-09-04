using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace UI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        public ManagerController(IUoW data, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _data = data;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> AllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? field = null)
        {
            //Player player = new Player
            //{
            //    FirstName = "Иванов",
            //    LastName = "Иван",
            //    BirthDate = DateTime.UtcNow,
            //    CreatedAt = DateTime.UtcNow,
            //    Email = "parent@example.com",
            //    UserName = "parent",
                
            //    IsActive = true
            //};


            //var result = await _userManager.CreateAsync(player, "Gjktnbkj22@");
            //if (result.Succeeded) {
            //    _userManager.AddToRoleAsync(player, "Player");
            //}
            _data.SaveEntitiesAsync();


            var data = await _data.Players.GetAllAsync();
            return View(data);
        }

        // GET: /Players/GetData (для серверной обработки Tabulator)
        [HttpGet]
        public async Task<object> GetData(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? field = null)
        {
            //var query = _unitOfWork.Players
            //    .Include(p => p.Team)
            //    .AsQueryable();

            //// Поиск
            //if (!string.IsNullOrEmpty(search))
            //{
            //    search = search.ToLower();
            //    query = query.Where(p =>
            //        p.FirstName.ToLower().Contains(search) ||
            //        p.LastName.ToLower().Contains(search) ||
            //        p.Team.Name.ToLower().Contains(search));
            //}

            //// Сортировка
            //if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(sort))
            //{
            //    query = sort.ToLower() == "asc"
            //        ? query.OrderBy(p => EF.Property<object>(p, field))
            //        : query.OrderByDescending(p => EF.Property<object>(p, field));
            //}

            //var total = await query.CountAsync();
            //var data = await query
            //    .Skip((page - 1) * size)
            //    .Take(size)
            //    .Select(p => new
            //    {
            //        p.Id,
            //        p.FirstName,
            //        p.LastName,
            //        BirthDate = p.BirthDate.ToString("dd.MM.yyyy"),
            //        p.Position,
            //        TeamName = p.Team.Name,
            //        Age = DateTime.Now.Year - p.BirthDate.Year
            //    })
            //    .ToListAsync();

            //получаем IQeryable для формирования запроса
            var query = _data.Players.Query();

            //применяем поиск, если есть
            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(p =>
                    p.FirstName.ToLower().Contains(searchTerm) ||
                    p.LastName.ToLower().Contains(searchTerm));
            }

            //получаем общее количество записей до пагинации
            var total = await query.CountAsync();

            //применяем пагинацию и получаем только одну страницу
            var players = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new
            {
                id = p.Id.ToString(),
                firstName = p.FirstName,
                lastName = p.LastName                
            })
            .ToListAsync();

            // 5. Вычисляем последнюю страницу
            var lastPage = total > 0 ? (int)Math.Ceiling((double)total / size) : 1;

            //return Json(new
            //{
            //    data = players,
            //    total = total,
            //    last_page = lastPage
            //});
            return players;
        }



        public IActionResult AllGroups()
        {
            //var data = _data.Childs.
            return View();
        }

        public IActionResult AllCoaches()
        {
            //var data = _data.Childs.
            return View();
        }
    }
}
