using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;

        public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Ticket> query = _context.Tickets.Include(t => t.Status).Include(t => t.Customer).Include(t => t.Assignee);
            
            if (roles.Contains("Client"))
                query = query.Where(t => t.CustomerId == userId);
            else if (roles.Contains("Technician"))
                query = query.Where(t => t.AssigneeId == userId);
            
            var tickets = await query.ToListAsync();
            return Ok(tickets);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _context.Tickets.Include(t => t.Status).Include(t => t.Customer).Include(t => t.Assignee).FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ticket = new Ticket
            {
                Title = model.Title,
                Description = model.Description,
                CustomerId = userId,
                StatusId = model.StatusId,
                Priority = model.Priority ?? "Medium",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusModel model)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();
            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveStatusUpdate", model.StatusId);
            return Ok(ticket);
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var comment = new Comment
            {
                TicketId = id,
                UserId = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveNewMessage", comment);
            return Ok(comment);
        }
    }
} 