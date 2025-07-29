using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(CreateGroupModel model);
        Task<List<GroupDto>> GetGroupsAsync();
        Task<bool> AddMemberToGroupAsync(int groupId, string userId);
        Task<bool> RemoveMemberFromGroupAsync(int groupId, string userId);
    }
}
