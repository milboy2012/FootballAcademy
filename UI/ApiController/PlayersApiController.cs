using Core.Entity;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Player;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController]
    [Route("api/players")]    
    [IgnoreAntiforgeryToken] // API защищён cookie/JWT; CSRF-токен для fetch добавим позже
    public class PlayersApiController : ControllerBase
    {
        private readonly IPlayerService _players;
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPlayerAccountService _accounts; 


        public PlayersApiController(IPlayerService players, IUoW data, UserManager<AppUser> userManager, IPlayerAccountService account)
        {
            _players = players;
            _data = data;
            _userManager = userManager;
            _accounts = account;
        }

        private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Coach");
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Список для Tabulator. Родитель видит только своих детей.</summary>
        [HttpGet]
        public async Task<ActionResult<TabulatorPage<PlayerListItemDto>>> GetPage([FromQuery] TabulatorQuery q, CancellationToken ct)
        {
            // Tabulator шлёт sort[0][field] / sort[0][dir] — разбираем вручную
            q.SortField ??= Request.Query["sort[0][field]"].FirstOrDefault();
            q.SortDir ??= Request.Query["sort[0][dir]"].FirstOrDefault();

            return Ok(await _players.GetPageAsync(q, IsStaff ? null : CurrentUserId, ct));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PlayerListItemDto>> Get(Guid id, CancellationToken ct)
        {
            var item = await _players.GetAsync(id, ct);
            if (item is null) return NotFound();
            if (!IsStaff && item.ParentId != CurrentUserId) return Forbid();
            return Ok(item);
        }

        [HttpPost]        
        public async Task<IActionResult> Create(PlayerEditDto dto, CancellationToken ct)
        {
            NormalizeForCaller(dto);
            var error = await ValidateAsync(dto, ct);
            if (error is not null) return BadRequest(new { error });
            var id = await _players.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]        
        public async Task<IActionResult> Update(Guid id, PlayerEditDto dto, CancellationToken ct)
        {
            if (!IsStaff)
            {
                var existing = await _players.GetAsync(id, CancellationToken.None);
                if(existing is null) return NotFound();
                if(existing.ParentId != CurrentUserId) return Forbid();
                dto.GroupId = existing.GroupId;
                dto.IsActive = existing.IsActive;
            }
            NormalizeForCaller(dto);
            var error = await ValidateAsync(dto, ct);
            if (error is not null) return BadRequest(new { error });
            return await _players.UpdateAsync(id, dto, ct) ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _players.DeleteAsync(id, ct) ? NoContent() : NotFound();

        /// <summary>Справочники для выпадающих списков формы.</summary>
        [HttpGet("lookups")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Lookups(CancellationToken ct)
        {
            var groups = await _data.Groups.Query().AsNoTracking().OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.Name }).ToListAsync(ct); 
            

            var parents = (await _userManager.GetUsersInRoleAsync("Parent"))
                .Where(u => u.IsActive)
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, Name = $"{u.LastName} {u.FirstName} ({u.Email})" });

            return Ok(new { groups, parents });
        }

        private async Task<string?> ValidateAsync(PlayerEditDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return "Имя и фамилия обязательны";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dto.BirthDate > today || dto.BirthDate < today.AddYears(-18))
                return "Некорректная дата рождения";
            //if (!await _data.Users.AnyAsync(u => u.Id == dto.ParentId, ct))
            //    return "Родитель не найден";
            //if (dto.GroupId is not null && !await _ctx.Groups.AnyAsync(g => g.Id == dto.GroupId, ct))
            //    return "Группа не найдена";
            return null;
        }


        /// <summary>Родитель может привязывать детей только к себе.</summary>
        private void NormalizeForCaller(PlayerEditDto dto)
        {
            if (!IsStaff)
            {
                dto.ParentId = CurrentUserId;
                dto.IsActive = true;
            }
        }


        //создание аккаунта для ребенка
        private async Task<bool> CanManageAsync(Guid playerId, CancellationToken ct)
        {
            if (IsStaff) return true;
            var parentId = await _data.Players.Query().Where(p => p.Id == playerId).Select(p => p.ParentId).FirstOrDefaultAsync(ct);
            return parentId == CurrentUserId;
        }

        public record CreateAccountRequest(string? Password);

        /// <summary>Создать учётную запись ребёнку. Возвращает логин и пароль — показать один раз.</summary>
        [HttpPost("{id:guid}/account")]
        public async Task<IActionResult> CreateAccount(Guid id, CreateAccountRequest req, CancellationToken ct)
        {
            if (!await CanManageAsync(id, ct)) return Forbid();
            var (info, error) = await _accounts.CreateAsync(id, req.Password, ct);
            return error is null ? Ok(info) : BadRequest(new { error });
        }

        [HttpPost("{id:guid}/account/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
        {
            if (!await CanManageAsync(id, ct)) return Forbid();
            var (pwd, error) = await _accounts.ResetPasswordAsync(id, ct);
            return error is null ? Ok(new { password = pwd }) : BadRequest(new { error });
        }

        [HttpPatch("{id:guid}/account/active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool value, CancellationToken ct)
        {
            if (!await CanManageAsync(id, ct)) return Forbid();
            var error = await _accounts.SetActiveAsync(id, value, ct);
            return error is null ? NoContent() : BadRequest(new { error });
        }
    }
}
