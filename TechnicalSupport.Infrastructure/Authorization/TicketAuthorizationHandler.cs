using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Authorization
{
    public class TicketAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Ticket>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TicketAuthorizationHandler(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Ticket resource)
        {
            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            // Admin hoặc Ticket Manager có quyền truy cập toàn bộ
            if (userRoles.Contains("Admin") || userRoles.Contains("Ticket Manager"))
            {
                context.Succeed(requirement);
                return;
            }

            var isManager = userRoles.Contains("Manager");
            var isAgent = userRoles.Contains("Agent");

            // 1. Kiểm tra quyền sở hữu (Customer)
            if (resource.CustomerId == user.Id)
            {
                // Khách hàng có quyền đọc, thêm bình luận, tải file, cập nhật ticket nếu đang mở
                if (requirement.Name == TicketOperations.Read.Name ||
                    requirement.Name == TicketOperations.AddComment.Name ||
                    requirement.Name == TicketOperations.UploadFile.Name ||
                    (requirement.Name == TicketOperations.Update.Name && resource.Status.Name == "Open"))
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // 2. Kiểm tra quyền được giao (Assignee) và theo vai trò
            if (isAgent) // Bao gồm cả Manager và Admin (đã return)
            {
                // Agent/Manager có thể đọc ticket được gán cho mình
                if (requirement.Name == TicketOperations.Read.Name && resource.AssigneeId == user.Id)
                {
                    context.Succeed(requirement);
                    return;
                }

                // Agent/Manager có thể thực hiện các hành động trên ticket được gán cho mình
                if ((requirement.Name == TicketOperations.Update.Name ||
                     requirement.Name == TicketOperations.ChangeStatus.Name ||
                     requirement.Name == TicketOperations.AddComment.Name ||
                     requirement.Name == TicketOperations.UploadFile.Name) &&
                    resource.AssigneeId == user.Id)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // 3. Kiểm tra quyền theo nhóm
            if (isAgent && resource.GroupId.HasValue)
            {
                var userGroupIds = await _context.TechnicianGroups
                    .Where(tg => tg.UserId == user.Id)
                    .Select(tg => tg.GroupId)
                    .ToListAsync();

                if (userGroupIds.Contains(resource.GroupId.Value))
                {
                    // Nếu ticket thuộc nhóm của Agent/Manager, họ có quyền đọc, bình luận, ...
                    if (requirement.Name == TicketOperations.Read.Name ||
                        requirement.Name == TicketOperations.AddComment.Name ||
                        requirement.Name == TicketOperations.UploadFile.Name)
                    {
                        context.Succeed(requirement);
                        return;
                    }
                    // Manager có quyền Assign và ChangeStatus trên ticket của nhóm mình
                    if (isManager && (requirement.Name == TicketOperations.Assign.Name || requirement.Name == TicketOperations.ChangeStatus.Name))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }

            // 4. Kiểm tra quyền tạm thời (sau khi các check khác thất bại)
            var tempPermission = await _context.TemporaryPermissions
                .FirstOrDefaultAsync(p => p.UserId == user.Id &&
                                        p.ClaimType == nameof(Ticket) &&
                                        p.ClaimValue == $"{resource.TicketId}:{requirement.Name}" &&
                                        p.ExpirationAt > DateTime.UtcNow);

            if (tempPermission != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}