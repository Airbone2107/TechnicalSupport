using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;

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
        private readonly IMapper _mapper;

        public TicketsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<TicketHub> hubContext,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets([FromQuery] PaginationParams paginationParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .OrderByDescending(t => t.UpdatedAt);

            if (roles.Contains("Client"))
                query = query.Where(t => t.CustomerId == userId);
            else if (roles.Contains("Technician"))
                query = query.Where(t => t.AssigneeId == userId || t.AssigneeId == null);

            var pagedTicketsEntities = await query.ToPagedResultAsync(paginationParams.PageNumber, paginationParams.PageSize);
            
            var ticketDtos = _mapper.Map<List<TicketDto>>(pagedTicketsEntities.Items);

            var pagedResultDto = new PagedResult<TicketDto>(ticketDtos, pagedTicketsEntities.TotalCount, pagedTicketsEntities.PageNumber, pagedTicketsEntities.PageSize);

            return Ok(ApiResponse.Success(pagedResultDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.TicketId == id);
                
            if (ticket == null)
            {
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
            }

            var ticketDto = _mapper.Map<TicketDto>(ticket);
            return Ok(ApiResponse.Success(ticketDto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ticket = _mapper.Map<Ticket>(model);
            
            ticket.CustomerId = userId;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            
            await _context.Entry(ticket).Reference(t => t.Status).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Customer).LoadAsync();

            var ticketDto = _mapper.Map<TicketDto>(ticket);

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.TicketId }, ApiResponse.Success(ticketDto, "Ticket created successfully."));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusModel model)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
            }

            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveStatusUpdate", model.StatusId);

            var ticketDto = _mapper.Map<TicketDto>(ticket);
            return Ok(ApiResponse.Success(ticketDto, "Ticket status updated successfully."));
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

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();
            var commentDto = _mapper.Map<CommentDto>(comment);

            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveNewMessage", commentDto);
            
            return Ok(ApiResponse.Success(commentDto, "Comment added successfully."));
        }
    }
} 