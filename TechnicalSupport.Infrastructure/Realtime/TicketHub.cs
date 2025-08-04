using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TechnicalSupport.Infrastructure.Realtime
{
    [Authorize]
    public class TicketHub : Hub
    {
        public async Task JoinTicketGroup(string ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}");
        }

        public async Task LeaveTicketGroup(string ticketId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}");
        }
    }
} 