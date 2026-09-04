using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UI.Models.ViewModels.Group;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/groups")]
    [Authorize(Roles = "Manager")]
    [IgnoreAntiforgeryToken]
    public class GroupsApiController : ControllerBase
    {
        private readonly IGroupService _svc;
        public GroupsApiController(IGroupService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] GroupsQuery q, CancellationToken ct)
        {
            q.SortField ??= Request.Query["sort[0][field]"].FirstOrDefault();
            q.SortDir ??= Request.Query["sort[0][dir]"].FirstOrDefault();
            return Ok(await _svc.GetPageAsync(q, ct));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await _svc.GetAsync(id, ct) is { } g ? Ok(g) : NotFound();

        [HttpGet("coaches")]
        public async Task<IActionResult> Coaches(CancellationToken ct) => Ok(await _svc.GetCoachesAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(GroupEditDto dto, CancellationToken ct)
        {
            var (id, error) = await _svc.CreateAsync(dto, ct);
            return error is null ? Ok(new { id }) : BadRequest(new { error });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, GroupEditDto dto, CancellationToken ct) => R(await _svc.UpdateAsync(id, dto, ct));

        [HttpPatch("{id:guid}/coach/{coachId:guid}")]
        public async Task<IActionResult> AssignCoach(Guid id, Guid coachId, CancellationToken ct) => R(await _svc.AssignCoachAsync(id, coachId, ct));

        [HttpGet("{id:guid}/players")]
        public async Task<IActionResult> Players(Guid id, CancellationToken ct) => Ok(new { data = await _svc.GetPlayersAsync(id, ct) });

        /// <summary>Перевод в другую группу или отчисление (targetGroupId = null).</summary>
        [HttpPost("{id:guid}/players/move")]
        public async Task<IActionResult> Move(Guid id, MovePlayersDto dto, CancellationToken ct) => R(await _svc.MovePlayersAsync(id, dto, ct));

        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id, [FromQuery] Guid? moveTo, CancellationToken ct) => R(await _svc.ArchiveAsync(id, true, moveTo, ct));

        [HttpPost("{id:guid}/unarchive")]
        public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) => R(await _svc.ArchiveAsync(id, false, null, ct));

        private IActionResult R(string? error) => error is null ? NoContent() : BadRequest(new { error });
    }
}
