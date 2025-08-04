using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Authorization
{
    public class CommentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Comment>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CommentAuthorizationHandler(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Comment resource)
        {
            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            // Admin có toàn quyền
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }
            
            // Người tạo comment có quyền sửa/xóa
            if (requirement.Name == CommentOperations.Update.Name ||
                requirement.Name == CommentOperations.Delete.Name)
            {
                if (resource.UserId == user.Id)
                {
                    context.Succeed(requirement);
                    return; 
                }
            }

            // Kiểm tra quyền tạm thời
            var tempPermission = await _context.TemporaryPermissions
                .FirstOrDefaultAsync(p => p.UserId == user.Id &&
                                        p.ClaimType == nameof(Comment) &&
                                        p.ClaimValue == $"{resource.CommentId}:{requirement.Name}" &&
                                        p.ExpirationAt > DateTime.UtcNow);

            if (tempPermission != null)
            {
                context.Succeed(requirement);
            }
        }
    }
} 