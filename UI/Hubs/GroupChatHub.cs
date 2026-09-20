using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using UI.Services.Interfaces;

namespace UI.Hubs
{
    [Authorize]
    public class GroupChatHub : Hub
    {
        private readonly IChatService _chat;
        public GroupChatHub(IChatService chat) => _chat = chat;
        private Guid Me => Guid.Parse(Context.UserIdentifier!);

        public override async Task OnConnectedAsync()
        {
            foreach (var g in await _chat.MyGroupsAsync(Context.User!, Context.ConnectionAborted))
                await Groups.AddToGroupAsync(Context.ConnectionId, g.Id.ToString());
            await base.OnConnectedAsync();
        }

        public async Task Send(Guid groupId, string text, Guid? replyToId)
        {
            var (msg, error) = await _chat.SendAsync(Context.User!, groupId, text, replyToId, Context.ConnectionAborted);
            if (error is not null) { await Clients.Caller.SendAsync("Error", error); return; }
            await Clients.Group(groupId.ToString()).SendAsync("Message", msg);
        }

        public async Task Edit(Guid messageId, string text)
        {
            var (msg, error) = await _chat.EditAsync(Me, messageId, text, Context.ConnectionAborted);
            if (error is not null) { await Clients.Caller.SendAsync("Error", error); return; }
            await Clients.Group(msg!.GroupId.ToString()).SendAsync("Edited", msg);
        }

        public async Task Delete(Guid messageId)
        {
            var (groupId, error) = await _chat.DeleteAsync(Context.User!, messageId, Context.ConnectionAborted);
            if (error is not null) { await Clients.Caller.SendAsync("Error", error); return; }
            await Clients.Group(groupId.ToString()).SendAsync("Deleted", messageId);
        }

        public Task Typing(Guid groupId) => Clients.OthersInGroup(groupId.ToString()).SendAsync("Typing", groupId, Context.User!.FindFirst("fullName")?.Value ?? Context.User!.Identity!.Name);

        public Task MarkRead(Guid groupId) => _chat.MarkReadAsync(Me, groupId, Context.ConnectionAborted);
    }
}
