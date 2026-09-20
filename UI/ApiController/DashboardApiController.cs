using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController, Route("api/dashboard"), Authorize(Roles = "Manager"), IgnoreAntiforgeryToken]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _svc; public DashboardApiController(IDashboardService svc) => _svc = svc;
        [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _svc.GetAsync(ct));
    }
}
