using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Groups
{
    /// <summary>
    /// Cung cấp logic nghiệp vụ để quản lý các nhóm hỗ trợ.
    /// </summary>
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;


        /// <summary>
        /// Khởi tạo một instance mới của GroupService.
        /// </summary>
        public GroupService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<GroupDto> CreateGroupAsync(CreateGroupModel model)
        {
            var group = _mapper.Map<Group>(model);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return _mapper.Map<GroupDto>(group);
        }

        /// <inheritdoc />
        public async Task<List<GroupDto>> GetAllGroupsAsync()
        {
            var groups = await _context.Groups.ToListAsync();
            return _mapper.Map<List<GroupDto>>(groups);
        }

        /// <inheritdoc />
        public async Task<List<UserDto>> GetMembersInGroupAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Group not found.");
            }

            var currentUserPrincipal = _httpContextAccessor.HttpContext.User;
            var userId = _userManager.GetUserId(currentUserPrincipal);

            // SỬA LỖI: Lấy claims trực tiếp từ Principal (JWT token) thay vì từ database
            var canManageAllGroups = currentUserPrincipal.HasClaim("permissions", "groups:manage");

            if (!canManageAllGroups)
            {
                var canReadOwnMembers = currentUserPrincipal.HasClaim("permissions", "groups:read_own_members");
                if (canReadOwnMembers)
                {
                    // Kiểm tra xem người dùng hiện tại có phải là thành viên của nhóm họ đang yêu cầu không
                    var isMemberOfGroup = await _context.TechnicianGroups
                        .AnyAsync(tg => tg.GroupId == groupId && tg.UserId == userId);
                    
                    if (!isMemberOfGroup)
                    {
                        // Nếu không phải thành viên, từ chối truy cập
                        throw new UnauthorizedAccessException("User does not have permission to view members of this specific group.");
                    }
                }
                else
                {
                     // Nếu không có quyền nào hợp lệ, từ chối
                     throw new UnauthorizedAccessException("User does not have required permissions to view group members.");
                }
            }

            // Nếu logic kiểm tra vượt qua (là Manager hoặc là Group Manager của nhóm này), tiếp tục lấy dữ liệu
            var users = await _context.TechnicianGroups
                .Where(tg => tg.GroupId == groupId)
                .Select(tg => tg.User)
                .ToListAsync();
            
            return _mapper.Map<List<UserDto>>(users);
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message)> AddMemberToGroupAsync(int groupId, AddMemberModel model)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == groupId);
            if (!groupExists) return (false, "Group not found.");

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return (false, "User not found.");

            var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
            if (!isAgent) return (false, "Only users with the 'Agent' role can be added to a group.");

            var alreadyMember = await _context.TechnicianGroups
                .AnyAsync(tg => tg.GroupId == groupId && tg.UserId == model.UserId);
            if (alreadyMember) return (false, "User is already a member of this group.");
            
            _context.TechnicianGroups.Add(new TechnicianGroup { GroupId = groupId, UserId = model.UserId });
            await _context.SaveChangesAsync();

            return (true, "Member added successfully.");
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message)> RemoveMemberFromGroupAsync(int groupId, string userId)
        {
            var membership = await _context.TechnicianGroups
                .FirstOrDefaultAsync(tg => tg.GroupId == groupId && tg.UserId == userId);
            
            if (membership == null) return (false, "User is not a member of this group.");

            _context.TechnicianGroups.Remove(membership);
            await _context.SaveChangesAsync();

            return (true, "Member removed successfully.");
        }
    }
}