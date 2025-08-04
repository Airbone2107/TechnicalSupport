using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Features.Admin.Abstractions
{
    public interface IAdminService
    {
        Task<PagedResult<UserDetailDto>> GetUsersAsync(UserFilterParams filterParams);
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        Task<UserDetailDto?> UpdateUserAsync(string userId, UpdateUserByAdminModel model);
        Task<(bool Success, string Message)> UpdateUserRolesAsync(string userId, UpdateUserRolesModel model);
        Task<(bool Success, string Message)> DeleteUserAsync(string userId);
    }
} 