using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(CreateGroupModel model);
        Task<List<GroupDto>> GetAllGroupsAsync();
        Task<List<UserDto>> GetMembersInGroupAsync(int groupId);
        Task<(bool Success, string Message)> AddMemberToGroupAsync(int groupId, AddMemberModel model);
        Task<(bool Success, string Message)> RemoveMemberFromGroupAsync(int groupId, string userId);
    }
} 