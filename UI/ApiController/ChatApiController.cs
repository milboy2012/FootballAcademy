using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UI.Services.Interfaces;

namespace UI.ApiController
{
    [ApiController, Route("api/chat"), Authorize, IgnoreAntiforgeryToken]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatService _chat; 
        public ChatApiController(IChatService chat) => _chat = chat;
        [HttpGet("groups")] 
        public async Task<IActionResult> Groups(CancellationToken ct) => Ok(await _chat.MyGroupsAsync(User, ct));

        [HttpGet("{groupId:guid}/messages")] 
        public async Task<IActionResult> History(Guid groupId, [FromQuery] DateTime? before, [FromQuery] int take = 50, CancellationToken ct = default) => Ok(await _chat.HistoryAsync(User, groupId, before, Math.Clamp(take, 1, 100), ct));
        
        [HttpGet("{groupId:guid}/members")] 
        public async Task<IActionResult> Members(Guid groupId, CancellationToken ct) => Ok(await _chat.MembersAsync(User, groupId, ct));
    }
}
