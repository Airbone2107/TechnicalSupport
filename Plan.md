### Plan.md: Kế hoạch Cải tiến Giao diện Ticket Queue

Tài liệu này vạch ra kế hoạch chi tiết để tái cấu trúc và cải tiến giao diện trang **Ticket Queue**, nhằm mang lại trải nghiệm người dùng tốt hơn, phân loại ticket rõ ràng và tăng hiệu quả công việc cho Agent và Ticket Manager.

#### 1. Mục tiêu

-   **Tái cấu trúc giao diện:** Thay thế cấu trúc tab hiện tại bằng một hệ thống tab mới, linh hoạt và mạnh mẽ hơn cho User Agent và Ticket Manager.
-   **Phân luồng ticket rõ ràng:** Chia nhỏ danh sách ticket thành các tab logic (Assigned, Active, On Hold, Archive, Unassigned) giúp người dùng tập trung vào đúng công việc cần thiết.
-   **Tối ưu hóa Backend:** Nâng cấp API để hỗ trợ việc lọc theo nhiều trạng thái cùng lúc, phục vụ cho các yêu cầu phức tạp từ giao diện mới.
-   **Cải thiện trải nghiệm tìm kiếm:** Điều chỉnh thanh tìm kiếm để chỉ hiển thị các bộ lọc phù hợp với ngữ cảnh của từng tab.

#### 2. Phân tích Yêu cầu Chi tiết

##### 2.1. Đối với User Agent (Không phải Ticket Manager)

Giao diện `TicketQueuePage` sẽ hiển thị 4 tab sau:

1.  **Assigned Ticket:**
    -   **Nội dung:** Chỉ hiển thị các ticket được gán (`assigneeId`) cho chính user đang đăng nhập.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Open`, `In Progress`, hoặc `On Hold`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

2.  **Active Ticket:**
    -   **Nội dung:** Hiển thị *tất cả* các ticket thuộc các nhóm (`GroupId`) mà user là thành viên. Bao gồm cả ticket chưa gán và ticket đã gán cho người khác trong nhóm.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Open` hoặc `In Progress`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

3.  **On Hold:**
    -   **Nội dung:** Hiển thị *tất cả* các ticket thuộc các nhóm (`GroupId`) mà user là thành viên.
    -   **Trạng thái:** Chỉ lọc các ticket có trạng thái là `On Hold`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

4.  **Archive:**
    -   **Nội dung:** Hiển thị *tất cả* các ticket thuộc các nhóm (`GroupId`) mà user là thành viên.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Closed` hoặc `Resolved`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

##### 2.2. Đối với User có quyền Ticket Manager

Sẽ có một giao diện khác, loại bỏ các tab `My Queue` và `Triage Queue (Unassigned)` cũ, thay bằng cấu trúc 6 tab:

-   **4 tab của User Agent:** `Assigned Ticket`, `Active Ticket`, `On Hold`, `Archive` với logic tương tự như trên.
-   **Thêm 2 tab đặc biệt:**
    1.  **Unassigned Ticket:**
        -   **Nội dung:** Hiển thị tất cả các ticket **chưa được gán vào bất kỳ nhóm nào** (`GroupId` is NULL).
        -   **Trạng thái:** Không lọc theo trạng thái mặc định.
        -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.
    2.  **All Ticket:**
        -   **Nội dung:** Hiển thị **tất cả** các ticket trong hệ thống mà Manager có quyền xem.
        -   **Trạng thái:** Không lọc theo trạng thái mặc định.
        -   **Tìm kiếm:** Theo `Keyword`, `Priority`, và **`Status`**. Đây là tab duy nhất có bộ lọc `Status`.

#### 3. Kế hoạch Thực thi

##### Giai đoạn 1: Cập nhật Backend

Mục tiêu là làm cho API đủ mạnh mẽ và rõ ràng để phục vụ các yêu cầu lọc phức tạp từ frontend.

**Task 1.1: Mở rộng `TicketFilterParams.cs`**
-   **Mục đích:** Bổ sung các tham số mới để API có thể nhận diện các kiểu lọc phức tạp hơn.
-   **Thay đổi:**
    -   Trong `TechnicalSupport.Application/Common/TicketFilterParams.cs`:
    -   Thêm thuộc tính `public List<int>? StatusIds { get; set; }`. Thuộc tính này sẽ thay thế cho `int? StatusId` cũ để nhận một danh sách các ID trạng thái.
    -   Thêm thuộc tính `public bool? TicketForMyGroup { get; set; }`. Khi được đặt là `true`, API sẽ chỉ trả về các ticket thuộc các nhóm (`Group`) mà người dùng hiện tại là thành viên. Tham số này rất quan trọng cho các tab của Agent.
-   **File ảnh hưởng:** `TechnicalSupport.Application/Common/TicketFilterParams.cs`.

**Task 1.2: Cập nhật logic lọc trong `TicketService.cs`**
-   **Mục đích:** Triển khai logic lọc mới dựa trên `TicketFilterParams` đã được mở rộng.
-   **Thay đổi:**
    -   Trong `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`, phương thức `GetTicketsAsync`:
        -   Sửa đổi câu lệnh LINQ để kiểm tra `filterParams.StatusIds`.
        -   Nếu `filterParams.StatusIds` có giá trị và không rỗng, thêm điều kiện `query = query.Where(t => filterParams.StatusIds.Contains(t.StatusId));`.
        -   Xóa logic xử lý cho `filterParams.StatusId` (thuộc tính cũ).
        -   **Thêm logic xử lý cho `filterParams.TicketForMyGroup`:**
            -   Kiểm tra nếu `filterParams.TicketForMyGroup` là `true`.
            -   Nếu đúng, lấy `userId` của người dùng đang thực hiện request.
            -   Truy vấn bảng `TechnicianGroups` để lấy danh sách các `groupId` mà người dùng này thuộc về.
            -   Thêm điều kiện lọc vào câu lệnh chính: `query = query.Where(t => t.GroupId.HasValue && userGroupIds.Contains(t.GroupId.Value));`.
-   **File ảnh hưởng:** `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`.

**Task 1.3: Điều chỉnh Controller `TicketsController.cs`**
-   **Mục đích:** Đảm bảo controller có thể nhận đúng mảng `statusIds` từ query string.
-   **Thay đổi:**
    -   Trong `TechnicalSupport.Api/Features/Tickets/TicketsController.cs`, phương thức `GetTickets`:
        -   Đảm bảo `[FromQuery] TicketFilterParams filterParams` có thể bind chính xác mảng `statusIds` từ URL dạng `?statusIds=1&statusIds=2`. ASP.NET Core hỗ trợ việc này tự động. Không cần thay đổi code ở đây nhưng cần nhận thức được.
-   **File ảnh hưởng:** `TechnicalSupport.Api/Features/Tickets/TicketsController.cs`.

##### Giai đoạn 2: Tái cấu trúc Frontend

Đây là phần việc chính, thay đổi hoàn toàn giao diện và logic của trang `TicketQueuePage`.

**Task 2.1: Tái cấu trúc `TicketQueuePage.tsx`**
-   **Mục đích:** Xóa bỏ cấu trúc cũ và thiết lập nền tảng cho hệ thống tab mới.
-   **Thay đổi:**
    -   Xóa logic liên quan đến các tab "My Queue" và "Triage Queue".
    -   Sử dụng `useState` để quản lý tab đang hoạt động (ví dụ: `const [activeTab, setActiveTab] = useState('assigned');`).
    -   Sử dụng component `<Tabs>` và `<Tab>` của Material-UI để dựng giao diện các tab.
-   **File ảnh hưởng:** `technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`.

**Task 2.2: Hiển thị Tab có điều kiện theo vai trò**
-   **Mục đích:** Hiển thị đúng bộ tab cho User Agent và Ticket Manager.
-   **Thay đổi:**
    -   Trong `TicketQueuePage.tsx`, sử dụng hook `useAuth()` để lấy quyền của người dùng.
    -   Dùng biến `const isTicketManager = hasPermission('tickets:assign_to_group');` để xác định vai trò.
    -   Render các tab `Unassigned Ticket` và `All Ticket` chỉ khi `isTicketManager` là `true`.

**Task 2.3: Implement Logic gọi API cho từng Tab**
-   **Mục đích:** Đảm bảo mỗi tab gọi API `getTickets` với tham số chính xác.
-   **Thay đổi:**
    -   Sử dụng `useEffect` để gọi lại `fetchTickets` mỗi khi `activeTab` hoặc `filters` thay đổi.
    -   Bên trong `fetchTickets`, xây dựng đối tượng `params: TicketFilterParams` dựa trên `activeTab`:
        -   `if (activeTab === 'assigned')`: `params = { assigneeId: user.id, statusIds: [1, 2, 5] };` // Open, In Progress, On Hold
        -   `if (activeTab === 'active')`: `params = { ticketForMyGroup: true, statusIds: [1, 2] };` // Lấy ticket thuộc nhóm của tôi, trạng thái Open/In-progress
        -   `if (activeTab === 'onHold')`: `params = { ticketForMyGroup: true, statusIds: [5] };` // Lấy ticket thuộc nhóm của tôi, trạng thái On Hold
        -   `if (activeTab === 'archive')`: `params = { ticketForMyGroup: true, statusIds: [3, 4] };` // Lấy ticket thuộc nhóm của tôi, trạng thái Resolved/Closed
        -   `if (activeTab === 'unassigned' && isTicketManager)`: `params = { unassignedToGroupOnly: true };` // Lấy ticket chưa vào nhóm nào
        -   `if (activeTab === 'all' && isTicketManager)`: `params = {};`
    -   Kết hợp `params` từ tab với `params` từ thanh tìm kiếm (keyword, priority) trước khi gọi API.
-   **File ảnh hưởng:** `technical-support-ui/src/features/tickets/api/ticketService.ts`, `technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`.

**Task 2.4: Điều chỉnh Thanh tìm kiếm**
-   **Mục đích:** Ẩn/hiện bộ lọc `Status` theo yêu cầu.
-   **Thay đổi:**
    -   Trong `TicketQueuePage.tsx`, bọc `<FormControl>` của `Select Status` trong một điều kiện render.
    -   Chỉ hiển thị bộ lọc này khi `isTicketManager && activeTab === 'all'`.
    -   Đảm bảo rằng khi chuyển từ tab "All Ticket" sang tab khác, giá trị của `statusId` trong state `filters` được xóa bỏ.