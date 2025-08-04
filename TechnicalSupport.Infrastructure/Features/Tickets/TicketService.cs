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

            // --- BẮT ĐẦU LOGIC REALTIME MỚI ---

            // 1. Thông báo cho các Ticket Manager về ticket mới
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

            // 2. Gửi sự kiện để kích hoạt animation ở client
            await _hubContext.Clients.All.SendAsync(HubEvents.NewTicketAdded, _mapper.Map<TicketDto>(ticket));

            // 3. Nếu ticket được TỰ ĐỘNG GÁN NHÓM, gửi thêm thông báo
            if (ticket.GroupId.HasValue)
            {
                var group = await _context.Groups.FindAsync(ticket.GroupId.Value);
                if (group != null)
                {
                    // Lấy danh sách thành viên trong nhóm
                    var members = await _context.TechnicianGroups
                        .Where(tg => tg.GroupId == ticket.GroupId.Value)
                        .Select(tg => tg.UserId)
                        .ToListAsync();

                    // Gửi thông báo cho các thành viên
                    var groupNotification = new NotificationPayload
                    {
                        Message = $"Ticket mới #{ticket.TicketId} đã được tự động phân vào nhóm '{group.Name}'.",
                        Link = $"/tickets/{ticket.TicketId}"
                    };
                    if (members.Any())
                    {
                        await _hubContext.Clients.Users(members).SendAsync(HubEvents.ReceiveNotification, groupNotification);
                    }
                }

                // Quan trọng: Gửi sự kiện cập nhật danh sách chung để các client khác (như Manager)
                // thấy ticket biến mất khỏi hàng chờ "Unassigned"
                await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);
            }
            // --- KẾT THÚC LOGIC REALTIME MỚI ---

            return _mapper.Map<TicketDto>(ticket);
        }

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

            // REALTIME: Gửi thông báo cho các bên liên quan
            var currentUser = await _userManager.GetUserAsync(GetCurrentUser());
            var notification = new NotificationPayload
            {
                Message = $"Trạng thái của ticket #{id} đã được cập nhật thành '{newStatus.Name}' bởi {currentUser.DisplayName}.",
                Link = $"/tickets/{id}"
            };

            // Gửi cho người tạo ticket
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notification);
            // Gửi cho người đang được gán (nếu có)
            if (!string.IsNullOrEmpty(ticket.AssigneeId) && ticket.AssigneeId != currentUser.Id)
            {
                await _hubContext.Clients.User(ticket.AssigneeId).SendAsync(HubEvents.ReceiveNotification, notification);
            }

            // Gửi cho những người đang xem ticket
            await _hubContext.Clients.Group($"Ticket_{id}").SendAsync(HubEvents.TicketListUpdated);
            // Gửi cho tất cả để cập nhật danh sách
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(id);
        }

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

            // REALTIME: Gửi comment tới những người đang xem ticket
            await _hubContext.Clients.Group($"Ticket_{ticketId}").SendAsync(HubEvents.ReceiveNotification, new NotificationPayload { Message = "Có bình luận mới trong ticket." });

            // Gửi thông báo cho người không xem
            var notification = new NotificationPayload
            {
                Message = $"{user.DisplayName} đã bình luận trong ticket #{ticketId}: '{ticket.Title}'.",
                Link = $"/tickets/{ticketId}"
            };
            // Nếu người bình luận không phải là khách hàng, báo cho khách hàng
            if (ticket.CustomerId != userId)
            {
                await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notification);
            }
            // Nếu người bình luận không phải là người được gán, báo cho người được gán
            if (ticket.AssigneeId != null && ticket.AssigneeId != userId)
            {
                await _hubContext.Clients.User(ticket.AssigneeId).SendAsync(HubEvents.ReceiveNotification, notification);
            }

            // Cập nhật danh sách
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);


            return commentDto;
        }

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

            // REALTIME: Gửi thông báo
            // 1. Cho agent được gán
            var notificationForAgent = new NotificationPayload
            {
                Message = $"Bạn vừa được {currentUser.DisplayName} gán ticket #{ticketId}: '{ticket.Title}'.",
                Link = $"/tickets/{ticketId}",
                IsHighlight = true // Thông báo quan trọng
            };
            await _hubContext.Clients.User(userToAssign.Id).SendAsync(HubEvents.ReceiveNotification, notificationForAgent);

            // 2. Cho khách hàng
            var notificationForClient = new NotificationPayload
            {
                Message = $"Ticket #{ticketId} của bạn đã được gán cho chuyên viên {userToAssign.DisplayName}.",
                Link = $"/tickets/{ticketId}",
            };
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notificationForClient);

            // 3. Cập nhật danh sách
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

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

            // REALTIME: Thông báo cho các thành viên trong nhóm và cập nhật danh sách
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

        public async Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return (false, "Ticket not found.");
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            // REALTIME: Cập nhật danh sách
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return (true, "Ticket deleted successfully.");
        }

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

            // REALTIME: Thông báo cho khách hàng
            var notificationForClient = new NotificationPayload
            {
                Message = $"Chuyên viên {user.DisplayName} đã tiếp nhận ticket #{ticketId} của bạn.",
                Link = $"/tickets/{ticketId}",
            };
            await _hubContext.Clients.User(ticket.CustomerId).SendAsync(HubEvents.ReceiveNotification, notificationForClient);
            await _hubContext.Clients.All.SendAsync(HubEvents.TicketListUpdated);

            return await GetTicketByIdAsync(ticketId);
        }

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

            // REALTIME: Thông báo cho các Ticket Manager
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
    }
}