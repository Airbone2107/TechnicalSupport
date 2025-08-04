using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Permissions.DTOs;

namespace TechnicalSupport.Application.Features.Permissions.Abstractions
{
    public interface IPermissionRequestService
    {
        Task<PermissionRequestDto> CreateRequestAsync(CreatePermissionRequestModel model);
        Task<PagedResult<PermissionRequestDto>> GetRequestsAsync(PaginationParams paginationParams, bool pendingOnly);
        Task<(bool Success, string Message)> ApproveRequestAsync(int requestId, ProcessPermissionRequestModel model);
        Task<(bool Success, string Message)> RejectRequestAsync(int requestId, ProcessPermissionRequestModel model);
    }
} 