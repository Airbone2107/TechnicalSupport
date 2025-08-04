using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Domain.Enums;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;

namespace TechnicalSupport.Infrastructure.Features.Tickets
{
    public partial class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TicketService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<TicketHub> hubContext,
            IMapper mapper,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext.User;

        public async Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams)
        {
            var userPrincipal = GetCurrentUser();
            var userId = _userManager.GetUserId(userPrincipal);

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .Include(t => t.Group)
                .OrderByDescending(t => t.UpdatedAt);

            // Lọc theo ticket do người dùng tạo
            if (filterParams.CreatedByMe == true)
            {
                query = query.Where(t => t.CustomerId == userId);
            }

            // Lọc theo ticket được gán cho người dùng hiện tại
            if (filterParams.MyTicket == true)
            {
                query = query.Where(t => t.AssigneeId == userId);
            }

            // Lọc theo ticket thuộc nhóm của người dùng
            if (filterParams.MyGroupTicket == true)
            {
                var userGroupIds = await _context.TechnicianGroups
                                .Where(tg => tg.UserId == userId)
                                .Select(tg => tg.GroupId)
                                .ToListAsync();

                query = query.Where(t => t.GroupId.HasValue && userGroupIds.Contains(t.GroupId.Value));
            }

            // Lọc theo danh sách tên trạng thái (List<string>)
            if (filterParams.Statuses != null && filterParams.Statuses.Any())
            {
                query = query.Where(t => filterParams.Statuses.Contains(t.Status.Name));
            }

            if (!string.IsNullOrWhiteSpace(filterParams.Priority))
            {
                query = query.Where(t => t.Priority == filterParams.Priority);
            }

            if (filterParams.UnassignedToGroupOnly == true)
            {
                query = query.Where(t => !t.GroupId.HasValue);
            }

            if (!string.IsNullOrWhiteSpace(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.TicketId.ToString().Contains(searchTerm));
            }

            return await query
                .AsNoTracking()
                .ProjectTo<TicketDto>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(filterParams.PageNumber, filterParams.PageSize);
        }

        public async Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .Include(t => t.Group)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.UploadedBy)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return null;
            }

            // SỬA LỖI: Thay thế chuỗi "Read" bằng đối tượng Operations.Read
            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), ticket, Operations.Read);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to view this ticket.");
            }

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto> CreateTicketAsync(CreateTicketModel model)
        {
            var customerId = _userManager.GetUserId(GetCurrentUser());

            var problemType = await _context.ProblemTypes.FindAsync(model.ProblemTypeId);

            var ticket = _mapper.Map<Ticket>(model);
            ticket.CustomerId = customerId;

            var openStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == nameof(TicketStatusEnum.Open));
            if (openStatus == null)
            {
                throw new InvalidOperationException("Default 'Open' status not found in the database.");
            }
            ticket.StatusId = openStatus.StatusId;

            ticket.GroupId = problemType?.GroupId;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _context.Entry(ticket).Reference(t => t.Status).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Customer).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Group).LoadAsync();

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return null;

            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveStatusUpdate", id, model.StatusId);

            return await GetTicketByIdAsync(id);
        }

        public async Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model)
        {
            var userId = _userManager.GetUserId(GetCurrentUser());
            var user = await _userManager.FindByIdAsync(userId);

            var comment = new Comment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = model.Content
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var commentDto = _mapper.Map<CommentDto>(comment);
            commentDto.User = _mapper.Map<UserDto>(user);

            await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("ReceiveNewMessage", commentDto);

            return commentDto;
        }

        public async Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var userToAssign = await _userManager.FindByIdAsync(model.AssigneeId);
            if (userToAssign == null) throw new InvalidOperationException("User to assign not found.");

            ticket.AssigneeId = model.AssigneeId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("TicketAssigned", ticketId, userToAssign.DisplayName);

            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var groupToAssign = await _context.Groups.FindAsync(model.GroupId);
            if (groupToAssign == null) throw new InvalidOperationException("Group to assign not found.");

            ticket.GroupId = model.GroupId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("TicketAssignedToGroup", ticketId, groupToAssign.Name);


            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return (false, "Ticket not found.");
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return (true, "Ticket deleted successfully.");
        }

        public async Task<TicketDto> ClaimTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            if (ticket.AssigneeId != null) throw new InvalidOperationException("Ticket is already assigned.");

            var userId = _userManager.GetUserId(GetCurrentUser());
            ticket.AssigneeId = userId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<TicketDto> RejectFromGroupAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            ticket.GroupId = null;
            ticket.AssigneeId = null;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTicketByIdAsync(ticketId);
        }
    }
}