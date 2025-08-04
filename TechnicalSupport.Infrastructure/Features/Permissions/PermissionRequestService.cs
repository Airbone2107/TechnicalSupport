using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Permissions.Abstractions;
using TechnicalSupport.Application.Features.Permissions.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Permissions
{
    public class PermissionRequestService : IPermissionRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionRequestService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUserId() => _userManager.GetUserId(_httpContextAccessor.HttpContext.User);

        public async Task<PermissionRequestDto> CreateRequestAsync(CreatePermissionRequestModel model)
        {
            var requesterId = GetCurrentUserId();
            var request = _mapper.Map<PermissionRequest>(model);
            request.RequesterId = requesterId;

            _context.PermissionRequests.Add(request);
            await _context.SaveChangesAsync();
            
            await _context.Entry(request).Reference(r => r.Requester).LoadAsync();

            return _mapper.Map<PermissionRequestDto>(request);
        }

        public async Task<PagedResult<PermissionRequestDto>> GetRequestsAsync(PaginationParams paginationParams, bool pendingOnly)
        {
            var query = _context.PermissionRequests
                .Include(r => r.Requester)
                .Include(r => r.Processor)
                .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            if (pendingOnly)
            {
                query = query.Where(r => r.Status == PermissionRequestStatus.Pending);
            }

            return await query
                .ProjectTo<PermissionRequestDto>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<(bool Success, string Message)> ApproveRequestAsync(int requestId, ProcessPermissionRequestModel model)
        {
            var request = await _context.PermissionRequests.Include(r => r.Requester).FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Status != PermissionRequestStatus.Pending)
            {
                return (false, "Request not found or has already been processed.");
            }

            var processorId = GetCurrentUserId();

            // Logic xử lý yêu cầu
            var parts = request.RequestedPermission.Split(':');
            if (parts.Length < 2) return (false, "Invalid permission format.");

            var type = parts[0].ToUpper();
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (type == "ROLE")
                {
                    var roleName = parts[1];
                    var result = await _userManager.AddToRoleAsync(request.Requester, roleName);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Failed to add role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else if (type == "TEMP_PERM")
                {
                    if (parts.Length < 5) 
                    {
                        await transaction.RollbackAsync();
                        return (false, "Invalid temporary permission format. Expected: TEMP_PERM:ResourceType:ResourceId:Operation:DurationSeconds");
                    }
                    var resourceType = parts[1];
                    var resourceId = parts[2];
                    var operation = parts[3];
                    if (!int.TryParse(parts[4], out var durationSeconds))
                    {
                        await transaction.RollbackAsync();
                        return (false, "Invalid duration for temporary permission.");
                    }

                    var tempPerm = new TemporaryPermission
                    {
                        UserId = request.RequesterId,
                        ClaimType = resourceType,
                        ClaimValue = $"{resourceId}:{operation}",
                        ExpirationAt = DateTime.UtcNow.AddSeconds(durationSeconds)
                    };
                    _context.TemporaryPermissions.Add(tempPerm);
                }
                else
                {
                    await transaction.RollbackAsync();
                    return (false, "Unknown permission type.");
                }

                request.Status = PermissionRequestStatus.Approved;
                request.ProcessorId = processorId;
                request.ProcessorNotes = model.Notes;
                request.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // TODO: Gửi thông báo cho người dùng
                return (true, "Request approved successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception ex
                return (false, "An error occurred while approving the request.");
            }
        }

        public async Task<(bool Success, string Message)> RejectRequestAsync(int requestId, ProcessPermissionRequestModel model)
        {
            var request = await _context.PermissionRequests.FirstOrDefaultAsync(r => r.Id == requestId);
             if (request == null || request.Status != PermissionRequestStatus.Pending)
            {
                return (false, "Request not found or has already been processed.");
            }

            var processorId = GetCurrentUserId();
            request.Status = PermissionRequestStatus.Rejected;
            request.ProcessorId = processorId;
            request.ProcessorNotes = model.Notes;
            request.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // TODO: Gửi thông báo cho người dùng
            return (true, "Request rejected successfully.");
        }
    }
} 