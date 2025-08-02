using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;

namespace TechnicalSupport.Infrastructure.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;
        private readonly IMapper _mapper;

        public TicketService(
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

        public async Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .OrderByDescending(t => t.UpdatedAt);

            // Logic phân quyền
            if (roles.Contains("Client"))
            {
                query = query.Where(t => t.CustomerId == userId);
            }
            else if (roles.Contains("Technician"))
            {
                query = query.Where(t => t.AssigneeId == userId || t.AssigneeId == null);
            }

            // Áp dụng các bộ lọc
            if (filterParams.StatusId.HasValue)
            {
                query = query.Where(t => t.StatusId == filterParams.StatusId.Value);
            }
            if (!string.IsNullOrEmpty(filterParams.Priority))
            {
                query = query.Where(t => t.Priority == filterParams.Priority);
            }
            if (!string.IsNullOrEmpty(filterParams.AssigneeId))
            {
                query = query.Where(t => t.AssigneeId == filterParams.AssigneeId);
            }
            if (!string.IsNullOrEmpty(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(searchTerm) || t.Description.ToLower().Contains(searchTerm));
            }

            var pagedTicketsEntities = await query.ToPagedResultAsync(filterParams.PageNumber, filterParams.PageSize);
            var ticketDtos = _mapper.Map<List<TicketDto>>(pagedTicketsEntities.Items);
            return new PagedResult<TicketDto>(ticketDtos, pagedTicketsEntities.TotalCount, filterParams.PageNumber, filterParams.PageSize);
        }

        public async Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return null;
            }

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto> CreateTicketAsync(CreateTicketModel model, string userId)
        {
            var ticket = _mapper.Map<Ticket>(model);

            ticket.CustomerId = userId;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _context.Entry(ticket).Reference(t => t.Status).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Customer).LoadAsync();

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return null;
            }

            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;

            // Lưu thay đổi vào DB TRƯỚC KHI gửi thông báo
            await _context.SaveChangesAsync();

            // Gửi thông báo đến các client đang theo dõi ticket này
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveStatusUpdate", model.StatusId);

            // Tải lại Status để đảm bảo dữ liệu trả về là mới nhất
            await _context.Entry(ticket).Reference(t => t.Status).LoadAsync();

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model, string userId)
        {
            var ticketExists = await _context.Tickets.AnyAsync(t => t.TicketId == ticketId);
            if (!ticketExists)
            {
                return null;
            }

            var comment = new Comment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();
            var commentDto = _mapper.Map<CommentDto>(comment);

            // Gửi comment mới tới các client
            await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("ReceiveNewMessage", commentDto);

            return commentDto;
        }

        public async Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model, string currentUserId)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                return null; // Ticket không tồn tại
            }

            var assigneeUser = await _userManager.FindByIdAsync(model.AssigneeId);
            if (assigneeUser == null)
            {
                // Người được gán không tồn tại. Có thể throw exception hoặc trả về null.
                return null;
            }

            var isTechnician = await _userManager.IsInRoleAsync(assigneeUser, "Technician");
            if (!isTechnician)
            {
                // Chỉ có thể gán cho Technician
                // Có thể throw một exception cụ thể để Controller bắt và trả về lỗi 400
                throw new InvalidOperationException("User is not a Technician and cannot be assigned a ticket.");
            }

            ticket.AssigneeId = assigneeUser.Id;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Tải lại thông tin Assignee để trả về DTO đầy đủ
            await _context.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

            // Gửi thông báo real-time
            await _hubContext.Clients.All.SendAsync("TicketAssigned", ticketId, assigneeUser.DisplayName);

            return _mapper.Map<TicketDto>(ticket);
        }
    }
}