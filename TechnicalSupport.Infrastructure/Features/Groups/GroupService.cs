using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Groups
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public GroupService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<GroupDto> CreateGroupAsync(CreateGroupModel model)
        {
            var group = _mapper.Map<Group>(model);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<List<GroupDto>> GetAllGroupsAsync()
        {
            var groups = await _context.Groups.ToListAsync();
            return _mapper.Map<List<GroupDto>>(groups);
        }

        public async Task<List<UserDto>> GetMembersInGroupAsync(int groupId)
        {
            var users = await _context.TechnicianGroups
                .Where(tg => tg.GroupId == groupId)
                .Select(tg => tg.User)
                .ToListAsync();
            
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<(bool Success, string Message)> AddMemberToGroupAsync(int groupId, AddMemberModel model)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == groupId);
            if (!groupExists) return (false, "Group not found.");

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return (false, "User not found.");

            // SỬA LỖI CỐT LÕI: Thay "Technician" bằng "Agent"
            var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
            if (!isAgent) return (false, "Only users with the 'Agent' role can be added to a group.");

            var alreadyMember = await _context.TechnicianGroups
                .AnyAsync(tg => tg.GroupId == groupId && tg.UserId == model.UserId);
            if (alreadyMember) return (false, "User is already a member of this group.");
            
            _context.TechnicianGroups.Add(new TechnicianGroup { GroupId = groupId, UserId = model.UserId });
            await _context.SaveChangesAsync();

            return (true, "Member added successfully.");
        }

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