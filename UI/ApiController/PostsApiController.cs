using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Models.ViewModels.Post;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController, Route("api/posts"), IgnoreAntiforgeryToken]
    public class PostsApiController : ControllerBase
    {
        private readonly IPostService _svc; public PostsApiController(IPostService svc) => _svc = svc;

        [HttpGet("feed"), AllowAnonymous]
        public async Task<IActionResult> Feed([FromQuery] PostType? type, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            return Ok(await _svc.GetFeedAsync(User, type, Math.Clamp(take, 1, 100), ct));
        }

        [HttpGet("{id:guid}"), AllowAnonymous]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            return await _svc.GetAsync(id, User, ct) is { } p ? Ok(p) : NotFound();
        }

        [HttpGet, Authorize(Roles = "Manager")]
        public async Task<IActionResult> All(CancellationToken ct) => Ok(new { data = await _svc.GetAllAsync(ct) });

        [HttpPost, Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(PostEditDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body)) 
                return BadRequest(new { error = "Заголовок и текст обязательны" });

            return Ok(new { id = await _svc.CreateAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), dto, ct) });
        }

        [HttpPut("{id:guid}"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(Guid id, PostEditDto dto, CancellationToken ct) { 
            var e = await _svc.UpdateAsync(id, dto, ct); return e is null ? NoContent() : BadRequest(new { error = e }); 
        }

        [HttpDelete("{id:guid}"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct) 
        { 
            var e = await _svc.DeleteAsync(id, ct); return e is null ? NoContent() : BadRequest(new { error = e }); 
        }

        [HttpPost("{id:guid}/image"), Authorize(Roles = "Manager"), RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Image(Guid id, IFormFile file, CancellationToken ct) 
        { 
            var (path, e) = await _svc.SetImageAsync(id, file, ct); return e is null ? Ok(new { path }) : BadRequest(new { error = e }); 
        }
    }
}
