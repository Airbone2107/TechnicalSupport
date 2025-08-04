Chào bạn, để cập nhật giao diện và thêm chức năng phân trang cho danh sách Ticket, chúng ta cần chỉnh sửa một vài tệp ở phía giao diện người dùng (frontend). Dưới đây là các tệp đã được cập nhật với đầy đủ code.

### Tóm tắt các thay đổi:

1.  **`technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`**:
    *   Thêm state để quản lý trang hiện tại (`page`) và tổng số ticket (`totalCount`).
    *   Cập nhật hàm `fetchTickets` để gửi thông tin phân trang (`pageNumber`, `pageSize`) lên API.
    *   Lưu lại tổng số ticket từ phản hồi của API để tính toán số trang.
    *   Thêm component `<Pagination>` của Material-UI để người dùng có thể chuyển trang.
    *   Reset về trang đầu tiên mỗi khi người dùng thay đổi tab hoặc bộ lọc để đảm bảo trải nghiệm người dùng nhất quán.

2.  **`technical-support-ui/src/features/tickets/routes/MyTicketsPage.tsx`**:
    *   Thực hiện các thay đổi tương tự như trang `TicketQueuePage` để thêm chức năng phân trang vào danh sách "My Tickets", đảm bảo tính đồng bộ trên toàn ứng dụng.

Bạn chỉ cần sao chép và dán nội dung của các tệp dưới đây để áp dụng thay đổi.

### Các tệp đã được cập nhật:

```tsx
// technical-support-ui/src/features/tickets/routes/MyTicketsPage.tsx
import React, { useEffect, useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import { getTickets } from '../api/ticketService';
import { Ticket, TicketFilterParams } from 'types/entities';
import LoadingSpinner from 'components/LoadingSpinner';
import { Box, Typography, Button, Stack, Card, CardActionArea, CardContent, Chip, Paper, Pagination } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';

type StatusColor = "info" | "warning" | "success" | "default" | "error";

const statusMapping: Record<string, { color: StatusColor, borderColor: string }> = {
  "Open": { color: "info", borderColor: 'info.main' },
  "In Progress": { color: "warning", borderColor: 'warning.main' },
  "Resolved": { color: "success", borderColor: 'success.main' },
  "Closed": { color: "default", borderColor: 'grey.500' },
  "On Hold": { color: "error", borderColor: 'error.main' },
};

const priorityMapping: Record<string, StatusColor> = {
  "High": "error",
  "Medium": "warning",
  "Low": "success",
};

const PAGE_SIZE = 10;

const MyTicketsPage: React.FC = () => {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const navigate = useNavigate();

  useEffect(() => {
    setLoading(true);
    const params: TicketFilterParams = { 
      pageNumber: page, 
      pageSize: PAGE_SIZE,
      createdByMe: true 
    };
    
    getTickets(params)
      .then((response) => {
        if (response.succeeded) {
          setTickets(response.data.items);
          setTotalCount(response.data.totalCount);
        } else {
          console.error("Failed to fetch tickets:", response.message);
          setTickets([]);
          setTotalCount(0);
        }
      })
      .catch((error) => console.error("Error fetching tickets:", error))
      .finally(() => setLoading(false));
  }, [page]);

  const handlePageChange = (event: React.ChangeEvent<unknown>, value: number) => {
    setPage(value);
  };

  const getStatusInfo = (statusName: string) => {
    return statusMapping[statusName] || { color: "default", borderColor: 'grey.500' };
  };

  if (loading) return <LoadingSpinner message="Đang tải các ticket của bạn..." />;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1">My Support Tickets</Typography>
        <Button
          component={RouterLink}
          to="/tickets/new"
          variant="contained"
          startIcon={<AddIcon />}
        >
          Tạo Ticket Mới
        </Button>
      </Box>

      {tickets.length === 0 ? (
        <Paper sx={{ textAlign: 'center', p: 4 }}>
          <Typography variant="h6">Bạn chưa tạo ticket nào.</Typography>
          <Button component={RouterLink} to="/tickets/new" sx={{ mt: 2 }}>
            Tạo ticket đầu tiên của bạn
          </Button>
        </Paper>
      ) : (
        <>
            <Stack spacing={2}>
            {tickets.map((t) => (
                <Card 
                key={t.ticketId} 
                sx={{ borderLeft: 5, borderColor: getStatusInfo(t.status.name).borderColor }}
                >
                <CardActionArea onClick={() => navigate(`/tickets/${t.ticketId}`)}>
                    <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <Typography variant="h6" component="h3" sx={{ mb: 1 }}>{t.title}</Typography>
                        <Chip
                        label={t.priority}
                        color={priorityMapping[t.priority] || 'default'}
                        size="small"
                        />
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        #{t.ticketId} &bull; Cập nhật lần cuối: {new Date(t.updatedAt).toLocaleString()}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Chip
                        label={t.status.name}
                        color={getStatusInfo(t.status.name).color}
                        variant="outlined"
                        size="small"
                        />
                        <Typography variant="body2" color="text.secondary">
                        Giao cho: {t.assignee?.displayName || 'Chưa gán'}
                        </Typography>
                    </Box>
                    </CardContent>
                </CardActionArea>
                </Card>
            ))}
            </Stack>

            {totalCount > PAGE_SIZE && (
                <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3, pb: 2 }}>
                    <Pagination
                        count={Math.ceil(totalCount / PAGE_SIZE)}
                        page={page}
                        onChange={handlePageChange}
                        color="primary"
                    />
                </Box>
            )}
        </>
      )}
    </Box>
  );
};

export default MyTicketsPage;
```

```tsx
// technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx
import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from 'contexts/AuthContext';
import { getTickets } from '../api/ticketService';
import { Ticket, TicketFilterParams } from 'types/entities';
import { TicketStatus } from 'types/enums';
import LoadingSpinner from 'components/LoadingSpinner';
import TicketCard from '../components/TicketCard';
import { Box, Typography, Paper, TextField, Select, MenuItem, FormControl, InputLabel, Button, Stack, Tabs, Tab, Pagination } from '@mui/material';

type TabValue = 'assigned' | 'active' | 'onHold' | 'archive' | 'unassigned' | 'all';

const TICKET_PAGE_SIZE = 10;

const TicketQueuePage: React.FC = () => {
  const { user, hasPermission } = useAuth();
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchFilters, setSearchFilters] = useState<{ searchQuery?: string, priority?: string, statuses?: string[] }>({});
  
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const isAgent = user?.roles.includes('Agent');
  const isTicketManager = hasPermission('tickets:assign_to_group');

  const agentTabs: { label: string; value: TabValue }[] = [
    { label: 'Assigned to Me', value: 'assigned' },
    { label: 'Active in My Groups', value: 'active' },
    { label: 'On Hold in My Groups', value: 'onHold' },
    { label: 'Archived in My Groups', value: 'archive' },
  ];

  const managerTabs: { label: string; value: TabValue }[] = [
    { label: 'Unassigned', value: 'unassigned' },
    { label: 'All Tickets', value: 'all' },
  ];

  const availableTabs = [
    ...(isAgent ? agentTabs : []),
    ...(isTicketManager ? managerTabs : []),
  ];

  const [activeTab, setActiveTab] = useState<TabValue>(availableTabs[0]?.value || 'assigned');

  const fetchTickets = useCallback(() => {
    setLoading(true);

    let tabParams: TicketFilterParams = {};
    switch (activeTab) {
      case 'assigned':
        tabParams = { myTicket: true, statuses: [TicketStatus.Open, TicketStatus.InProgress, TicketStatus.OnHold] };
        break;
      case 'active':
        tabParams = { myGroupTicket: true, statuses: [TicketStatus.Open, TicketStatus.InProgress] };
        break;
      case 'onHold':
        tabParams = { myGroupTicket: true, statuses: [TicketStatus.OnHold] };
        break;
      case 'archive':
        tabParams = { myGroupTicket: true, statuses: [TicketStatus.Resolved, TicketStatus.Closed] };
        break;
      case 'unassigned':
        tabParams = { unassignedToGroupOnly: true };
        break;
      case 'all':
        tabParams = {};
        break;
    }

    const finalParams: TicketFilterParams = {
      ...searchFilters,
      ...tabParams,
      pageNumber: page,
      pageSize: TICKET_PAGE_SIZE,
    };

    getTickets(finalParams)
      .then(res => {
          if (res.succeeded) {
              setTickets(res.data.items);
              setTotalCount(res.data.totalCount);
          } else {
              setTickets([]);
              setTotalCount(0);
          }
      })
      .catch(err => {
        console.error("Error fetching tickets:", err);
        setTickets([]);
        setTotalCount(0);
      })
      .finally(() => setLoading(false));
  }, [activeTab, searchFilters, page]);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement> | any) => {
    const { name, value } = e.target;
    setPage(1); // Reset page on filter change
    setSearchFilters(prev => ({ ...prev, [name]: value === "" ? undefined : value }));
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: TabValue) => {
    setActiveTab(newValue);
    setSearchFilters({});
    setPage(1);
  };

  const handleSearch = () => {
    if (page !== 1) {
        setPage(1);
    } else {
        fetchTickets();
    }
  };

  const resetFilters = () => {
    setSearchFilters({});
    setPage(1);
  };

  const handlePageChange = (event: React.ChangeEvent<unknown>, value: number) => {
    setPage(value);
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>Ticket Queue</Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable">
          {availableTabs.map(tab => (
            <Tab key={tab.value} label={tab.label} value={tab.value} />
          ))}
        </Tabs>
      </Box>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center">
          <TextField 
            name="searchQuery" 
            label="Search by Keyword/ID" 
            variant="outlined"
            size="small"
            fullWidth
            value={searchFilters.searchQuery || ''} 
            onChange={handleFilterChange} 
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
          />
          <FormControl size="small" fullWidth>
            <InputLabel>Priority</InputLabel>
            <Select name="priority" value={searchFilters.priority || ''} label="Priority" onChange={handleFilterChange}>
              <MenuItem value="">All Priorities</MenuItem>
              <MenuItem value="Low">Low</MenuItem>
              <MenuItem value="Medium">Medium</MenuItem>
              <MenuItem value="High">High</MenuItem>
            </Select>
          </FormControl>

          {isTicketManager && activeTab === 'all' && (
             <FormControl size="small" fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                name="statuses"
                multiple
                value={searchFilters.statuses || []}
                label="Status"
                onChange={handleFilterChange}
                renderValue={(selected) => (selected as string[]).join(', ')}
              >
                {Object.values(TicketStatus).map(s => (
                  <MenuItem key={s} value={s}>{s}</MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          <Button onClick={handleSearch} variant="contained">Search</Button>
          <Button onClick={resetFilters} variant="outlined">Reset</Button>
        </Stack>
      </Paper>

      {loading ? (
        <LoadingSpinner message="Loading ticket queue..." />
      ) : (
        <>
            <Stack spacing={2}>
            {tickets.length > 0 ? (
                tickets.map(t => <TicketCard key={t.ticketId} ticket={t} />)
            ) : (
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                <Typography>No tickets found matching your criteria.</Typography>
                </Paper>
            )}
            </Stack>

            {totalCount > TICKET_PAGE_SIZE && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3, pb: 2 }}>
                <Pagination
                count={Math.ceil(totalCount / TICKET_PAGE_SIZE)}
                page={page}
                onChange={handlePageChange}
                color="primary"
                />
            </Box>
            )}
        </>
      )}
    </Box>
  );
};

export default TicketQueuePage;
```