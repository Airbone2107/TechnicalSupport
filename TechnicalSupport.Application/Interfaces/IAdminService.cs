using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<UserDetailDto>> GetUsersAsync(PaginationParams paginationParams);
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        Task<UserDetailDto?> UpdateUserAsync(string userId, UpdateUserByAdminModel model);
        Task<(bool Success, string Message)> PromoteUserToTechnicianAsync(string userId, PromoteUserModel model);
        Task<(bool Success, string Message)> DeleteUserAsync(string userId);
    }
} 