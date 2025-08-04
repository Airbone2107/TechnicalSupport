# TODO: Tái cấu trúc Trang Ticket Queue

Tài liệu này là hướng dẫn từng bước để triển khai các thay đổi được nêu trong `Plan.md`. Mục tiêu là tái cấu trúc giao diện và backend của trang Ticket Queue để phân luồng công việc tốt hơn cho Agent và Ticket Manager.

## Giai đoạn 1: Cập nhật Backend

Mục tiêu của giai đoạn này là nâng cấp API để hỗ trợ các yêu cầu lọc phức tạp hơn, đặc biệt là lọc theo nhiều trạng thái cùng lúc và lọc ticket theo nhóm của người dùng.

---

### **Bước 1.1: Mở rộng `TicketFilterParams.cs`**

Chúng ta sẽ cập nhật `TicketFilterParams.cs` để API có thể nhận được một danh sách các ID trạng thái thay vì chỉ một, và thêm một cờ boolean để lọc ticket theo nhóm của người dùng.

**File cần thay đổi:** `TechnicalSupport.Application/Common/TicketFilterParams.cs`

**Nội dung mới:**
```csharp
// TechnicalSupport.Application/Common/TicketFilterParams.cs
namespace TechnicalSupport.Application.Common
{
    public class TicketFilterParams : PaginationParams
    {
        /// <summary>
        /// Danh sách các ID trạng thái để lọc. Thay thế cho StatusId.
        /// </summary>
        public List<int>? StatusIds { get; set; }
        
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public string? SearchQuery { get; set; }
        public bool? UnassignedToGroupOnly { get; set; }
        public bool? CreatedByMe { get; set; }

        /// <summary>
        /// Nếu true, chỉ trả về các ticket thuộc các nhóm mà người dùng hiện tại là thành viên.
        /// </summary>
        public bool? TicketForMyGroup { get; set; }

        // Thuộc tính `StatusId` không còn được sử dụng và đã được loại bỏ.
    }
}
```

---

### **Bước 1.2: Cập nhật logic lọc trong `TicketService.cs`**

Bây giờ, chúng ta sẽ cập nhật phương thức `GetTicketsAsync` trong `TicketService.cs` để sử dụng các tham số mới. Logic sẽ được sửa đổi để lọc theo `StatusIds` và `TicketForMyGroup`.

**File cần thay đổi:** `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`

**Nội dung mới:**
```csharp
// TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Extensions;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Realtime;

namespace TechnicalSupport.Infrastructure.Features.Tickets
{
    public partial class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<TicketHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TicketService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<TicketHub> hubContext,
            IMapper mapper,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext.User;

        public async Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams)
        {
            var userPrincipal = GetCurrentUser();
            var userId = _userManager.GetUserId(userPrincipal);
            
            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .Include(t => t.Group)
                .OrderByDescending(t => t.UpdatedAt);

            // Logic cũ cho createdByMe vẫn được giữ lại để phục vụ các chức năng khác
            if (filterParams.CreatedByMe == true)
            {
                query = query.Where(t => t.CustomerId == userId);
            }

            // Logic lọc theo nhóm của user
            if (filterParams.TicketForMyGroup == true)
            {
                var userGroupIds = await _context.TechnicianGroups
                                .Where(tg => tg.UserId == userId)
                                .Select(tg => tg.GroupId)
                                .ToListAsync();

                query = query.Where(t => t.GroupId.HasValue && userGroupIds.Contains(t.GroupId.Value));
            }
            
            // Logic lọc theo ticket được gán cho user hiện tại
            if (!string.IsNullOrWhiteSpace(filterParams.AssigneeId))
            {
                query = query.Where(t => t.AssigneeId == filterParams.AssigneeId);
            }
            
            // Logic lọc theo danh sách trạng thái
            if (filterParams.StatusIds != null && filterParams.StatusIds.Any())
            {
                query = query.Where(t => filterParams.StatusIds.Contains(t.StatusId));
            }
            
            if (!string.IsNullOrWhiteSpace(filterParams.Priority))
            {
                query = query.Where(t => t.Priority == filterParams.Priority);
            }
            
            if (filterParams.UnassignedToGroupOnly == true)
            {
                query = query.Where(t => !t.GroupId.HasValue);
            }

            if (!string.IsNullOrWhiteSpace(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.TicketId.ToString().Contains(searchTerm));
            }
            
            return await query
                .AsNoTracking()
                .ProjectTo<TicketDto>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(filterParams.PageNumber, filterParams.PageSize);
        }

        // ... các phương thức khác không thay đổi ...
        public async Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Customer)
                .Include(t => t.Assignee)
                .Include(t => t.Group)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.UploadedBy)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return null;
            }

            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), ticket, "Read");
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to view this ticket.");
            }

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto> CreateTicketAsync(CreateTicketModel model)
        {
            var customerId = _userManager.GetUserId(GetCurrentUser());

            var problemType = await _context.ProblemTypes.FindAsync(model.ProblemTypeId);

            var ticket = _mapper.Map<Ticket>(model);
            ticket.CustomerId = customerId;
            ticket.StatusId = 1; // Mặc định là "Open"
            ticket.GroupId = problemType?.GroupId;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _context.Entry(ticket).Reference(t => t.Status).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Customer).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.Group).LoadAsync();

            return _mapper.Map<TicketDto>(ticket);
        }

        public async Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return null;

            ticket.StatusId = model.StatusId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveStatusUpdate", id, model.StatusId);

            return await GetTicketByIdAsync(id);
        }

        public async Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model)
        {
            var userId = _userManager.GetUserId(GetCurrentUser());
            var user = await _userManager.FindByIdAsync(userId);

            var comment = new Comment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = model.Content
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var commentDto = _mapper.Map<CommentDto>(comment);
            commentDto.User = _mapper.Map<UserDto>(user);

            await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("ReceiveNewMessage", commentDto);

            return commentDto;
        }

        public async Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");
            
            var userToAssign = await _userManager.FindByIdAsync(model.AssigneeId);
            if (userToAssign == null) throw new InvalidOperationException("User to assign not found.");

            ticket.AssigneeId = model.AssigneeId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("TicketAssigned", ticketId, userToAssign.DisplayName);

            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            var groupToAssign = await _context.Groups.FindAsync(model.GroupId);
            if (groupToAssign == null) throw new InvalidOperationException("Group to assign not found.");

            ticket.GroupId = model.GroupId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("TicketAssignedToGroup", ticketId, groupToAssign.Name);


            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return (false, "Ticket not found.");
            }
            
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            
            return (true, "Ticket deleted successfully.");
        }

        public async Task<TicketDto> ClaimTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            if (ticket.AssigneeId != null) throw new InvalidOperationException("Ticket is already assigned.");

            var userId = _userManager.GetUserId(GetCurrentUser());
            ticket.AssigneeId = userId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTicketByIdAsync(ticketId);
        }

        public async Task<TicketDto> RejectFromGroupAsync(int ticketId)
        {
             var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found.");

            ticket.GroupId = null;
            ticket.AssigneeId = null;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTicketByIdAsync(ticketId);
        }
    }
}
```

---

## Giai đoạn 2: Tái cấu trúc Frontend

Đây là phần việc chính, thay đổi hoàn toàn giao diện và logic của trang `TicketQueuePage`.

---

### **Bước 2.1: Cập nhật `TicketFilterParams` ở Frontend**

Tương tự như backend, chúng ta cần cập nhật interface `TicketFilterParams` trong file `types/entities.ts` để client có thể gửi đúng tham số.

**File cần thay đổi:** `technical-support-ui/src/types/entities.ts`

**Nội dung mới:**
```typescript
// technical-support-ui/src/types/entities.ts
export interface User {
  id: string;
  displayName: string;
  email: string;
  expertise?: string;
}

export interface Status {
  statusId: number;
  name: string;
}

export interface Group {
  groupId: number;
  name: string;
  description?: string;
}

export interface ProblemType {
  problemTypeId: number;
  name: string;
  description: string;
  groupId?: number | null;
}

export interface Attachment {
  attachmentId: number;
  ticketId: number;
  originalFileName: string;
  storedPath: string;
  fileType: string;
  uploadedAt: string;
  uploadedByDisplayName: string;
}

export interface Comment {
  commentId: number;
  ticketId: number;
  content: string;
  createdAt: string;
  user: User;
}

export interface Ticket {
  ticketId: number;
  title: string;
  description: string;
  priority: 'Low' | 'Medium' | 'High';
  createdAt: string;
  updatedAt: string;
  closedAt?: string | null;
  status: Status;
  customer: User;
  assignee?: User | null;
  group?: Group | null;
  problemType?: ProblemType | null;
  comments?: Comment[];
  attachments?: Attachment[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
}

export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
}

export interface TicketFilterParams extends PaginationParams {
  statusIds?: number[]; // Thay đổi từ statusId: number
  priority?: string;
  assigneeId?: string;
  searchQuery?: string;
  unassignedToGroupOnly?: boolean;
  createdByMe?: boolean;
  ticketForMyGroup?: boolean; // Thêm thuộc tính mới
}

export interface UserDetail extends User {
  roles: string[];
  emailConfirmed: boolean;
  lockoutEnd?: Date | null;
}

export interface PermissionRequest {
  id: number;
  requester: User;
  requestedPermission: string;
  justification: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  processor?: User;
  processedAt?: string;
  processorNotes?: string;
  createdAt: string;
}

export interface UserFilterParams extends PaginationParams {
  role?: string;
  displayNameQuery?: string;
}
```

---

### **Bước 2.2: Tái cấu trúc hoàn toàn trang `TicketQueuePage`**

Đây là bước quan trọng nhất. Chúng ta sẽ xóa bỏ logic cũ của trang `TicketQueuePage.tsx` và thay thế bằng hệ thống tab mới, logic gọi API linh hoạt và thanh tìm kiếm có điều kiện như đã mô tả trong `Plan.md`.

**File cần thay đổi:** `technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`

**Nội dung mới:**
```tsx
// technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx
import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useAuth } from 'contexts/AuthContext';
import { getTickets } from '../api/ticketService';
import { Ticket, TicketFilterParams, Status } from 'types/entities';
import LoadingSpinner from 'components/LoadingSpinner';
import TicketCard from '../components/TicketCard';
import { Box, Typography, Paper, TextField, Select, MenuItem, FormControl, InputLabel, Button, Stack, Tabs, Tab } from '@mui/material';

// Danh sách các trạng thái để hiển thị trong bộ lọc
const ALL_STATUSES: Status[] = [
  { statusId: 1, name: "Open" }, { statusId: 2, name: "In Progress" },
  { statusId: 3, name: "Resolved" }, { statusId: 4, name: "Closed" },
  { statusId: 5, name: "On Hold" },
];

// Định nghĩa các tab và logic API tương ứng
const TABS_CONFIG = {
  // Tabs cho Agent & Manager
  assigned: { label: 'Assigned Ticket', params: { statusIds: [1, 2, 5] }, assigneeRequired: true },
  active: { label: 'Active Ticket', params: { ticketForMyGroup: true, statusIds: [1, 2] } },
  onHold: { label: 'On Hold', params: { ticketForMyGroup: true, statusIds: [5] } },
  archive: { label: 'Archive', params: { ticketForMyGroup: true, statusIds: [3, 4] } },
  // Tabs chỉ dành cho Manager
  unassigned: { label: 'Unassigned Ticket', params: { unassignedToGroupOnly: true }, managerOnly: true },
  all: { label: 'All Ticket', params: {}, managerOnly: true },
};

type TabKey = keyof typeof TABS_CONFIG;

const TicketQueuePage: React.FC = () => {
  const { user, hasPermission } = useAuth();
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchFilters, setSearchFilters] = useState<Pick<TicketFilterParams, 'searchQuery' | 'priority' | 'statusIds'>>({});
  
  const isTicketManager = hasPermission('tickets:assign_to_group');
  
  // Tab mặc định là 'assigned'
  const [activeTab, setActiveTab] = useState<TabKey>('assigned');

  // Logic gọi API chính
  const fetchTickets = useCallback(() => {
    if (!user) return;

    setLoading(true);

    const tabConfig = TABS_CONFIG[activeTab];
    if (!tabConfig) return;

    // Chỉ gọi API cho tab của manager nếu user có quyền
    if (tabConfig.managerOnly && !isTicketManager) return;
    
    let baseParams: TicketFilterParams = { ...tabConfig.params };

    // Gán assigneeId nếu tab yêu cầu
    if (tabConfig.assigneeRequired) {
      baseParams.assigneeId = user.nameid;
    }

    // Kết hợp tham số từ tab với tham số từ thanh tìm kiếm
    const finalParams: TicketFilterParams = {
      ...baseParams,
      ...searchFilters,
      pageNumber: 1,
      pageSize: 100, // Lấy 100 ticket cho mỗi lần tải
    };

    getTickets(finalParams)
      .then(res => setTickets(res.succeeded ? res.data.items : []))
      .catch(err => {
        console.error("Error fetching tickets:", err);
        setTickets([]);
      })
      .finally(() => setLoading(false));
  }, [activeTab, searchFilters, user, isTicketManager]);

  // Gọi API khi tab hoặc bộ lọc thay đổi
  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  // Xử lý thay đổi tab
  const handleTabChange = (event: React.SyntheticEvent, newValue: TabKey) => {
    setActiveTab(newValue);
    // Reset bộ lọc tìm kiếm khi chuyển tab để tránh nhầm lẫn
    setSearchFilters({}); 
  };
  
  // Xử lý thay đổi trên thanh tìm kiếm
  const handleSearchFilterChange = (e: React.ChangeEvent<HTMLInputElement | any>) => {
    const { name, value } = e.target;
    setSearchFilters(prev => ({ ...prev, [name]: value === "" ? undefined : value }));
  };
  
  const handleSearchOnEnter = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
        fetchTickets();
    }
  }

  const resetFilters = () => {
    setSearchFilters({});
  };

  // Danh sách các tab sẽ được render dựa trên quyền của người dùng
  const availableTabs = useMemo(() => {
    return (Object.keys(TABS_CONFIG) as TabKey[]).filter(key => 
      !TABS_CONFIG[key].managerOnly || isTicketManager
    );
  }, [isTicketManager]);


  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>Ticket Queue</Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
          {availableTabs.map(key => (
            <Tab key={key} label={TABS_CONFIG[key].label} value={key} />
          ))}
        </Tabs>
      </Box>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center">
          <TextField 
            name="searchQuery" 
            label="Search Keyword" 
            variant="outlined"
            size="small"
            fullWidth
            value={searchFilters.searchQuery || ''} 
            onChange={handleSearchFilterChange} 
            onKeyDown={handleSearchOnEnter}
            // Chỉ hiển thị ở những tab có ý nghĩa
            disabled={activeTab === 'assigned'}
          />
          <FormControl size="small" fullWidth>
            <InputLabel>Priority</InputLabel>
            <Select name="priority" value={searchFilters.priority || ''} label="Priority" onChange={handleSearchFilterChange}>
              <MenuItem value="">All Priorities</MenuItem>
              <MenuItem value="Low">Low</MenuItem>
              <MenuItem value="Medium">Medium</MenuItem>
              <MenuItem value="High">High</MenuItem>
            </Select>
          </FormControl>
          
          {/* Chỉ hiển thị bộ lọc Status cho tab 'All' của Manager */}
          {isTicketManager && activeTab === 'all' && (
            <FormControl size="small" fullWidth>
              <InputLabel>Status</InputLabel>
              <Select name="statusIds" value={searchFilters.statusIds || ''} label="Status" onChange={handleSearchFilterChange}>
                <MenuItem value="">All Statuses</MenuItem>
                {ALL_STATUSES.map(s => <MenuItem key={s.statusId} value={s.statusId}>{s.name}</MenuItem>)}
              </Select>
            </FormControl>
          )}

          <Button onClick={fetchTickets} variant="contained">Search</Button>
          <Button onClick={resetFilters} variant="outlined">Reset</Button>
        </Stack>
      </Paper>

      {loading ? (
        <LoadingSpinner message="Loading ticket queue..." />
      ) : (
        <Stack spacing={2}>
          {tickets.length > 0 ? (
            tickets.map(t => <TicketCard key={t.ticketId} ticket={t} />)
          ) : (
            <Paper sx={{ p: 4, textAlign: 'center' }}>
              <Typography>No tickets found in this view.</Typography>
            </Paper>
          )}
        </Stack>
      )}
    </Box>
  );
};

export default TicketQueuePage;
```