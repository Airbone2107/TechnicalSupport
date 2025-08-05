Chào bạn,

Tôi đã xem qua vấn đề bạn mô tả. Lỗi này xảy ra do cơ chế so sánh đường dẫn của `NavLink` trong `react-router-dom`.

### Phân tích nguyên nhân

Khi bạn ở trang `/tickets/new`, đường dẫn này khớp với cả hai `NavLink`:
1.  **Create Ticket**: `to="/tickets/new"` (khớp chính xác)
2.  **Ticket Queue**: `to="/tickets"` (khớp một phần, vì `/tickets/new` bắt đầu bằng `/tickets`)

Do đó, cả hai mục menu đều được coi là "active" và được làm nổi bật.

### Giải pháp

Để khắc phục, chúng ta cần chỉ định cho `NavLink` của "Ticket Queue" rằng nó chỉ nên "active" khi đường dẫn khớp chính xác với `/tickets`, chứ không phải các đường dẫn con của nó. Ta sẽ thực hiện điều này bằng cách thêm thuộc tính `end` vào component `NavLink` đó.

Tôi sẽ sửa đổi file `Sidebar.tsx` để thêm logic này.

Dưới đây là nội dung đầy đủ của file đã được cập nhật. Bạn chỉ cần sao chép và dán để thay thế file cũ.

```tsx
// technical-support-ui/src/components/Sidebar.tsx
import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from 'contexts/AuthContext';
import {
  Box,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Typography,
  ListSubheader,
  Divider,
} from '@mui/material';
import AssignmentIcon from '@mui/icons-material/Assignment';
import PeopleIcon from '@mui/icons-material/People';
import GroupWorkIcon from '@mui/icons-material/GroupWork';
import CategoryIcon from '@mui/icons-material/Category';

const drawerWidth = 250;

// Sửa đổi NavItem để chấp nhận prop 'end'
const NavItem: React.FC<{ to: string; label: string; end?: boolean }> = ({ to, label, end = false }) => {
  const navLinkStyle = ({ isActive }: { isActive: boolean }) => ({
    textDecoration: 'none',
    color: isActive ? '#fff' : '#a6adb4',
    backgroundColor: isActive ? 'rgba(24, 144, 255, 0.2)' : 'transparent',
    display: 'block',
  });

  return (
    // Truyền prop 'end' vào NavLink
    <NavLink to={to} style={navLinkStyle} end={end}>
      <ListItem disablePadding>
        <ListItemButton sx={{ borderRadius: 1, mx: 1, my: 0.5 }}>
          <ListItemText primary={label} />
        </ListItemButton>
      </ListItem>
    </NavLink>
  );
};

const Sidebar: React.FC = () => {
  const { hasPermission } = useAuth();

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: drawerWidth,
          boxSizing: 'border-box',
        },
      }}
    >
      <Box sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'center', height: 64 }}>
        <img src="/hutech-logo.jpg" alt="Logo" style={{ width: 32, height: 32, borderRadius: 6, marginRight: 12 }} />
        <Typography variant="h6" component="div" sx={{ color: '#fff', fontWeight: 600 }}>
          Hỗ trợ Kỹ thuật
        </Typography>
      </Box>
      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.1)' }} />
      <List component="nav">
        <NavItem to="/dashboard" label="Dashboard" />
        
        <ListSubheader sx={{ bgcolor: 'transparent', color: '#5a6e83' }}>Tickets</ListSubheader>
        {hasPermission(['tickets:read_own']) && (
          <NavItem to="/my-tickets" label="My Tickets" />
        )}
        {hasPermission(['tickets:create']) && (
          <NavItem to="/tickets/new" label="Create Ticket" />
        )}
        {hasPermission(['tickets:read_queue', 'tickets:read_all']) && (
          // Thêm prop 'end' vào đây để chỉ khớp chính xác đường dẫn "/tickets"
          <NavItem to="/tickets" label="Ticket Queue" end />
        )}

        <ListSubheader sx={{ bgcolor: 'transparent', color: '#5a6e83' }}>Management</ListSubheader>
        {hasPermission(['permissions:request']) && (
          <NavItem to="/permissions/request" label="Request Permission" />
        )}
        {hasPermission(['groups:manage']) && (
            <NavItem to="/admin/groups" label="Manage Groups" />
        )}
        {hasPermission(['permissions:review']) && (
             <NavItem to="/admin/permissions" label="Permission Requests" />
        )}
        {hasPermission(['problemtypes:manage']) && (
             <NavItem to="/admin/problem-types" label="Problem Types" />
        )}

        {hasPermission(['users:manage']) && (
          <>
            <ListSubheader sx={{ bgcolor: 'transparent', color: '#5a6e83' }}>Administration</ListSubheader>
            <NavItem to="/admin/users" label="Manage Users" />
          </>
        )}
      </List>
    </Drawer>
  );
};

export default Sidebar;
```

Thay đổi này sẽ đảm bảo `Ticket Queue` chỉ sáng lên khi bạn ở đúng trang `/tickets`, giải quyết được vấn đề bạn gặp phải.

Nếu có bất kỳ câu hỏi nào khác, đừng ngần ngại hỏi nhé.