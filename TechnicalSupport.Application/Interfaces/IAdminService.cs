using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<UserDetailDto>> GetUsersAsync(PaginationParams paginationParams);
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        Task<UserDetailDto?> UpdateUserAsync(string userId, UpdateUserByAdminModel model);
    }
}
