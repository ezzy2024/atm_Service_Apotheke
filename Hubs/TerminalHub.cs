using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace ServiceApotheke.API.Hubs
{
    [Authorize]
    public class TerminalHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var terminalId = Context.GetHttpContext()?.Request.Query["terminalId"].ToString();
            
            if (!string.IsNullOrEmpty(terminalId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, terminalId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var terminalId = Context.GetHttpContext()?.Request.Query["terminalId"].ToString();
            if (!string.IsNullOrEmpty(terminalId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, terminalId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendOffer(string terminalId, string offer)
        {
            await Clients.OthersInGroup(terminalId).SendAsync("ReceiveOffer", offer);
        }

        public async Task SendAnswer(string terminalId, string answer)
        {
            await Clients.OthersInGroup(terminalId).SendAsync("ReceiveAnswer", answer);
        }

        public async Task SendIceCandidate(string terminalId, string candidate)
        {
            await Clients.OthersInGroup(terminalId).SendAsync("ReceiveIceCandidate", candidate);
        }
    }
}
