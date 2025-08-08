using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.Abstractions;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Admin
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }
        
        private string GetCurrentUserId() => _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<PagedResult<UserDetailDto>> GetUsersAsync(UserFilterParams filterParams)
        {
            var query = _userManager.Users.AsQueryable();

            // Lọc theo tên hiển thị
            if (!string.IsNullOrWhiteSpace(filterParams.DisplayNameQuery))
            {
                query = query.Where(u => u.DisplayName.Contains(filterParams.DisplayNameQuery));
            }

            // Lọc theo vai trò
            if (!string.IsNullOrWhiteSpace(filterParams.Role))
            {
                // Tìm kiếm ID của các user thuộc vai trò được chỉ định
                var usersInRole = await _userManager.GetUsersInRoleAsync(filterParams.Role);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                
                // Lọc query chính dựa trên danh sách ID đã tìm thấy
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync();

            var pagedUsers = await query
                .OrderBy(u => u.DisplayName)
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .ToListAsync();

            var userDtos = new List<UserDetailDto>();
            foreach (var user in pagedUsers)
            {
                var userDto = _mapper.Map<UserDetailDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(userDto);
            }

            return new PagedResult<UserDetailDto>(userDtos, totalCount, filterParams.PageNumber, filterParams.PageSize);
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var userDto = _mapper.Map<UserDetailDto>(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);
            return userDto;
        }

        public async Task<UserDetailDto?> UpdateUserAsync(string userId, UpdateUserByAdminModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            if (model.DisplayName != null)
            {
                user.DisplayName = model.DisplayName;
            }
            if (model.Expertise != null)
            {
                user.Expertise = model.Expertise;
            }

            await _userManager.UpdateAsync(user);
            return await GetUserByIdAsync(userId);
        }

        public async Task<(bool Success, string Message)> UpdateUserRolesAsync(string userId, UpdateUserRolesModel model)
        {
            // Xác thực mật khẩu người dùng hiện tại
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                return (false, "Cannot identify the current administrator.");
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(currentUser, model.CurrentPassword);
            if (!isPasswordCorrect)
            {
                return (false, "Mật khẩu xác thực không đúng.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }
            
            // Không cho phép chỉnh sửa vai trò của Admin, trừ khi có logic phức tạp hơn
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                 return (false, "Cannot modify roles of an Admin user.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = model.Roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(model.Roles).ToList();

            if (rolesToRemove.Contains("Admin"))
            {
                return (false, "Cannot remove Admin role.");
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return (false, "Failed to remove roles.");
            }

            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                // Rollback: Thêm lại các role đã xóa nếu việc thêm mới thất bại
                await _userManager.AddToRolesAsync(user, rolesToRemove);
                return (false, "Failed to add new roles.");
            }

            return (true, "User roles updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa các bình luận do người dùng này tạo
                await _context.Comments.Where(c => c.UserId == userId).ExecuteDeleteAsync();

                // 2. Gán lại các ticket mà người dùng này đang phụ trách
                await _context.Tickets
                    .Where(t => t.AssigneeId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (string?)null));

                // 3. Xóa các file đính kèm do người dùng này tải lên
                var attachmentsToDelete = await _context.Attachments.Where(a => a.UploadedById == userId).ToListAsync();
                foreach (var attachment in attachmentsToDelete)
                {
                    // Cần dịch vụ file storage để xóa file vật lý
                    //_fileStorageService.DeleteFile(attachment.StoredPath);
                }
                _context.Attachments.RemoveRange(attachmentsToDelete);
                await _context.SaveChangesAsync();
                
                // 4. Xóa các ticket do người dùng này tạo (Customer)
                var ticketsToDelete = await _context.Tickets.Where(t => t.CustomerId == userId).ToListAsync();
                if (ticketsToDelete.Any())
                {
                    var ticketIds = ticketsToDelete.Select(t => t.TicketId).ToList();
                    await _context.Comments.Where(c => ticketIds.Contains(c.TicketId)).ExecuteDeleteAsync();
                    await _context.Attachments.Where(a => ticketIds.Contains(a.TicketId)).ExecuteDeleteAsync();
                    _context.Tickets.RemoveRange(ticketsToDelete);
                    await _context.SaveChangesAsync();
                }

                // 5. Xóa bản ghi người dùng
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return (false, "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await transaction.CommitAsync();
                return (true, "User and all related data deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception (ex)
                return (false, "An error occurred during user deletion.");
            }
        }
    }
} 