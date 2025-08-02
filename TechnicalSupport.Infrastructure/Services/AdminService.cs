using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<PagedResult<UserDetailDto>> GetUsersAsync(PaginationParams paginationParams)
        {
            var query = _userManager.Users;

            var pagedUsers = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var userDtos = new List<UserDetailDto>();
            foreach (var user in pagedUsers)
            {
                var userDto = _mapper.Map<UserDetailDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(userDto);
            }

            var totalCount = await query.CountAsync();
            return new PagedResult<UserDetailDto>(userDtos, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
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

        public async Task<(bool Success, string Message)> PromoteUserToTechnicianAsync(string userId, PromoteUserModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Technician") || roles.Contains("Admin"))
            {
                return (false, "User is already a Technician or Admin.");
            }

            if (!roles.Contains("Client"))
            {
                return (false, "User is not a Client and cannot be promoted.");
            }

            var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, "Client");
            if (!removeRoleResult.Succeeded)
            {
                return (false, "Failed to remove Client role.");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, "Technician");
            if (!addRoleResult.Succeeded)
            {
                // Cố gắng thêm lại vai trò Client để tránh người dùng không có vai trò nào
                await _userManager.AddToRoleAsync(user, "Client");
                return (false, "Failed to add Technician role.");
            }
            
            if (!string.IsNullOrWhiteSpace(model.Expertise))
            {
                user.Expertise = model.Expertise;
                await _userManager.UpdateAsync(user);
            }

            return (true, "User promoted successfully.");
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

                // 2. Gán lại các ticket mà người dùng này đang phụ trách (nếu là Technician)
                await _context.Tickets
                    .Where(t => t.AssigneeId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (string?)null));

                // 3. Xóa các file đính kèm do người dùng này tải lên
                // (Thao tác này cần xóa file vật lý, nên phải làm cẩn thận)
                var attachmentsToDelete = await _context.Attachments.Where(a => a.UploadedById == userId).ToListAsync();
                foreach (var attachment in attachmentsToDelete)
                {
                    if (File.Exists(attachment.StoredPath))
                    {
                        File.Delete(attachment.StoredPath);
                    }
                }
                _context.Attachments.RemoveRange(attachmentsToDelete);
                await _context.SaveChangesAsync();
                
                // 4. Xóa các ticket do người dùng này tạo (Customer)
                // Lưu ý: Thao tác này có thể bị hạn chế bởi foreign key, cần xử lý cẩn thận
                // Ví dụ: Xóa các comment và attachment của ticket đó trước
                var ticketsToDelete = await _context.Tickets.Where(t => t.CustomerId == userId).ToListAsync();
                if (ticketsToDelete.Any())
                {
                    var ticketIds = ticketsToDelete.Select(t => t.TicketId).ToList();
                    await _context.Comments.Where(c => ticketIds.Contains(c.TicketId)).ExecuteDeleteAsync();
                    // Thêm logic xóa attachment của các ticket này nếu cần
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