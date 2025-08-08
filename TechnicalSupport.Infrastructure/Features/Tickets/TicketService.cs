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
using TechnicalSupport.Application.Features.Realtime.DTOs;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Domain.Enums;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;

namespace TechnicalSupport.Infrastructure.Features.Tickets
{
    /// <summary>
    /// Cung cấp logic nghiệp vụ để quản lý tickets.
    /// </summary>
    public partial class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Khởi tạo một instance mới của TicketService.
        /// </summary>
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

        /// <inheritdoc />
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

            if (filterParams.CreatedByMe == true)
            {
                query = query.Where(t => t.CustomerId == userId);
            }

            if (filterParams.MyTicket == true)
            {
                query = query.Where(t => t.AssigneeId == userId);
            }

            if (filterParams.MyGroupTicket == true)
            {
                var userGroupIds = await _context.TechnicianGroups
                                .Where(tg => tg.UserId == userId)
                                .Select(tg => tg.GroupId)
                                .ToListAsync();

                query = query.Where(t => t.GroupId.HasValue && userGroupIds.Contains(t.GroupId.Value));
            }

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

        /// <inheritdoc />
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

            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), ticket, Operations.Read);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to view this ticket.");
            }

            return _mapper.Map<TicketDto>(ticket);
        }

        /// <inheritdoc />
        public async Task<TicketDto> CreateTicketAsync(CreateTicketModel model)
        {
            var customerId = _userManager.GetUserId(GetCurrentUser());
            var customer = await _userManager.FindByIdAsync(customerId);
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

            // Gửi thông báo real-time sau khi tạo ticket
            await NotifyOnTicketCreated(ticket, customer);

            return _mapper.Map<TicketDto>(ticket);
        }

        /// <inheritdoc />
        public async Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null) return null;

            var newStatus = await _context.Statuses.FindAsync(model.StatusId);
            if (newStatus == null) return null;

            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;
            if (newStatus.Name == "Closed" || newStatus.Name == "Resolved")
            {
                ticket.ClosedAt = DateTime.UtcNow;
            }
            else
            {
                ticket.ClosedAt = null;
            }

            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(GetCurrentUser());
            var notification = new NotificationPayload
            {
                Message = $"Trạng thái của ticket #{id} đã được cập nhật thành '{newStatus.Name}' bởi {currentUser.DisplayName}.",
                Link = $"/tickets/{id}"
            };
            
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notification);
            if (!string.IsNullOrEmpty(ticket.AssigneeId) && ticket.AssigneeId != currentUser.Id)
            {
                await _hubContext.Clients.User(ticket.AssigneeId).SendAsync(HubEvents.ReceiveNotification, notification);
            }

            await _hubContext.Clients.Group($"Ticket_{id}").SendAsync(HubEvents.TicketListUpdated);
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(id);
        }

        /// <inheritdoc />
        public async Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null) return null;

            var userId = _userManager.GetUserId(GetCurrentUser());
            var user = await _userManager.FindByIdAsync(userId);

            var comment = new Comment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var commentDto = _mapper.Map<CommentDto>(comment);
            commentDto.User = _mapper.Map<UserDto>(user);

            await _hubContext.Clients.Group($"Ticket_{ticketId}").SendAsync(HubEvents.ReceiveNotification, new NotificationPayload { Message = "Có bình luận mới trong ticket." });

            var notification = new NotificationPayload
            {
                Message = $"{user.DisplayName} đã bình luận trong ticket #{ticketId}: '{ticket.Title}'.",
                Link = $"/tickets/{ticketId}"
            };
            if (ticket.CustomerId != userId)
            {
                await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notification);
            }
            if (ticket.AssigneeId != null && ticket.AssigneeId != userId)
            {
                await _hubContext.Clients.User(ticket.AssigneeId).SendAsync(HubEvents.ReceiveNotification, notification);
            }

            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return commentDto;
        }

        /// <inheritdoc />
        public async Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model)
        {
            var ticket = await _context.Tickets.Include(t => t.Customer).FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var userToAssign = await _userManager.FindByIdAsync(model.AssigneeId);
            if (userToAssign == null) throw new InvalidOperationException("User to assign not found.");

            var currentUser = await _userManager.GetUserAsync(GetCurrentUser());

            ticket.AssigneeId = model.AssigneeId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var notificationForAgent = new NotificationPayload
            {
                Message = $"Bạn vừa được {currentUser.DisplayName} gán ticket #{ticketId}: '{ticket.Title}'.",
                Link = $"/tickets/{ticketId}",
                IsHighlight = true
            };
            await _hubContext.Clients.User(userToAssign.Id).SendAsync(HubEvents.ReceiveNotification, notificationForAgent);

            var notificationForClient = new NotificationPayload
            {
                Message = $"Ticket #{ticketId} của bạn đã được gán cho chuyên viên {userToAssign.DisplayName}.",
                Link = $"/tickets/{ticketId}",
            };
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notificationForClient);

            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(ticketId);
        }

        /// <inheritdoc />
        public async Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var groupToAssign = await _context.Groups.FindAsync(model.GroupId);
            if (groupToAssign == null) throw new InvalidOperationException("Group to assign not found.");

            ticket.GroupId = model.GroupId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var members = await _context.TechnicianGroups
                .Where(tg => tg.GroupId == model.GroupId)
                .Select(tg => tg.UserId)
                .ToListAsync();

            var notification = new NotificationPayload
            {
                Message = $"Một ticket mới (#{ticketId}) đã được chuyển vào nhóm {groupToAssign.Name} của bạn.",
                Link = $"/tickets/{ticketId}"
            };
            await _hubContext.Clients.Users(members).SendAsync(HubEvents.ReceiveNotification, notification);
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(ticketId);
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return (false, "Ticket not found.");
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return (true, "Ticket deleted successfully.");
        }

        /// <inheritdoc />
        public async Task<TicketDto> ClaimTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.Include(t => t.Customer).FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            if (ticket.AssigneeId != null) throw new InvalidOperationException("Ticket is already assigned.");

            var userId = _userManager.GetUserId(GetCurrentUser());
            var user = await _userManager.FindByIdAsync(userId);

            ticket.AssigneeId = userId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            var notificationForClient = new NotificationPayload
            {
                Message = $"Chuyên viên {user.DisplayName} đã tiếp nhận ticket #{ticketId} của bạn.",
                Link = $"/tickets/{ticketId}",
            };
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notificationForClient);
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(ticketId);
        }

        /// <inheritdoc />
        public async Task<TicketDto> RejectFromGroupAsync(int ticketId)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var oldGroupName = ticket.Group?.Name;

            ticket.GroupId = null;
            ticket.AssigneeId = null;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            var ticketManagers = await _userManager.GetUsersInRoleAsync("Ticket Manager");
            var notification = new NotificationPayload
            {
                Message = $"Ticket #{ticket.TicketId} đã bị đẩy khỏi nhóm {oldGroupName} và cần phân loại lại.",
                Link = $"/tickets/{ticket.TicketId}",
                Type = "warning"
            };
            foreach (var manager in ticketManagers)
            {
                await _hubContext.Clients.User(manager.Id).SendAsync(HubEvents.ReceiveNotification, notification);
            }
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(ticketId);
        }
        
        /// <summary>
        /// Gửi các thông báo real-time cần thiết khi một ticket mới được tạo.
        /// </summary>
        /// <param name="ticket">Ticket vừa được tạo.</param>
        /// <param name="customer">Người tạo ticket.</param>
        private async Task NotifyOnTicketCreated(Ticket ticket, ApplicationUser customer)
        {
            // Thông báo cho tất cả Ticket Manager.
            var ticketManagers = await _userManager.GetUsersInRoleAsync("Ticket Manager");
            var managerNotification = new NotificationPayload
            {
                Message = $"Ticket mới #{ticket.TicketId}: '{ticket.Title}' đã được tạo bởi {customer.DisplayName}.",
                Link = $"/tickets/{ticket.TicketId}",
                Type = "info"
            };
            foreach (var manager in ticketManagers)
            {
                await _hubContext.Clients.User(manager.Id).SendAsync(HubEvents.ReceiveNotification, managerNotification);
            }

            // Gửi sự kiện để client cập nhật UI (vd: animation).
            await _hubContext.Clients.All.SendAsync(HubEvents.NewTicketAdded, _mapper.Map<TicketDto>(ticket));

            // Nếu ticket được tự động gán vào nhóm, gửi thông báo cho thành viên nhóm.
            if (ticket.GroupId.HasValue && ticket.Group != null)
            {
                var members = await _context.TechnicianGroups
                    .Where(tg => tg.GroupId == ticket.GroupId.Value)
                    .Select(tg => tg.UserId)
                    .ToListAsync();

                if (members.Any())
                {
                    var groupNotification = new NotificationPayload
                    {
                        Message = $"Ticket mới #{ticket.TicketId} đã được tự động phân vào nhóm '{ticket.Group.Name}'.",
                        Link = $"/tickets/{ticket.TicketId}"
                    };
                    await _hubContext.Clients.Users(members).SendAsync(HubEvents.ReceiveNotification, groupNotification);
                }
                
                // Cập nhật danh sách chung để client thấy ticket không còn trong hàng đợi "Unassigned".
                await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);
            }
        }
    }
}