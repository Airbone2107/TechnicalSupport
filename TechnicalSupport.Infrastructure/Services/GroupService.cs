using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Services
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

        public async Task<List<GroupDto>> GetGroupsAsync()
        {
            var groups = await _context.Groups.ToListAsync();
            return _mapper.Map<List<GroupDto>>(groups);
        }

        public async Task<bool> AddMemberToGroupAsync(int groupId, string userId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == groupId);
            var user = await _userManager.FindByIdAsync(userId);
            var isTechnician = user != null && await _userManager.IsInRoleAsync(user, "Technician");

            if (!groupExists || !isTechnician) return false;

            var membershipExists = await _context.TechnicianGroups
                .AnyAsync(tg => tg.GroupId == groupId && tg.UserId == userId);

            if (membershipExists) return true; // Đã là thành viên

            var technicianGroup = new TechnicianGroup { GroupId = groupId, UserId = userId };
            _context.TechnicianGroups.Add(technicianGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberFromGroupAsync(int groupId, string userId)
        {
            var membership = await _context.TechnicianGroups
                .FirstOrDefaultAsync(tg => tg.GroupId == groupId && tg.UserId == userId);

            if (membership == null) return false; // Không tìm thấy để xóa

            _context.TechnicianGroups.Remove(membership);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
