using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public AdminService(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<PagedResult<UserDetailDto>> GetUsersAsync(PaginationParams paginationParams)
        {
            var query = _userManager.Users.OrderBy(u => u.Email);

            var pagedUsers = await query.ToPagedResultAsync(paginationParams.PageNumber, paginationParams.PageSize);

            var userDetailDtos = new List<UserDetailDto>();
            foreach (var user in pagedUsers.Items)
            {
                var dto = _mapper.Map<UserDetailDto>(user);
                dto.Roles = await _userManager.GetRolesAsync(user);
                userDetailDtos.Add(dto);
            }

            return new PagedResult<UserDetailDto>(userDetailDtos, pagedUsers.TotalCount, pagedUsers.PageNumber, pagedUsers.PageSize);
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var dto = _mapper.Map<UserDetailDto>(user);
            dto.Roles = await _userManager.GetRolesAsync(user);

            return dto;
        }

        public async Task<UserDetailDto?> UpdateUserAsync(string userId, UpdateUserByAdminModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            user.DisplayName = model.DisplayName;

            // Cập nhật roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, model.Roles);

            // Cập nhật trạng thái khóa
            if (model.IsLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            await _userManager.UpdateAsync(user);

            return await GetUserByIdAsync(userId);
        }
    }
}
