Chắc chắn rồi. Tôi đã cập nhật lại `Plan.md` của bạn để bao gồm các file bị bỏ sót và làm rõ các điểm cần thiết.

Các thay đổi đã được tích hợp trực tiếp vào kế hoạch ban đầu để đảm bảo tính logic và dễ theo dõi. Tôi đã **in đậm** các phần được thêm vào hoặc chỉnh sửa.

Dưới đây là phiên bản `Plan.md` đã được cập nhật:

---

### Plan.md: Kế hoạch Cải tiến Giao diện Ticket Queue

Tài liệu này vạch ra kế hoạch chi tiết để tái cấu trúc và cải tiến giao diện trang **Ticket Queue**, nhằm mang lại trải nghiệm người dùng tốt hơn, phân loại ticket rõ ràng và tăng hiệu quả công việc cho Agent và Ticket Manager.

#### 1. Mục tiêu

-   **Tái cấu trúc giao diện:** Thay thế cấu trúc tab hiện tại bằng một hệ thống tab mới, linh hoạt và mạnh mẽ hơn cho User Agent và Ticket Manager.
-   **Phân luồng ticket rõ ràng:** Chia nhỏ danh sách ticket thành các tab logic (Assigned, Active, On Hold, Archive, Unassigned) giúp người dùng tập trung vào đúng công việc cần thiết.
-   **Tối ưu hóa Backend & Frontend:** Nâng cấp API và UI để hỗ trợ các tham số lọc tường minh, mạnh mẽ và an toàn kiểu dữ liệu hơn thông qua việc sử dụng Enum.
-   **Cải thiện trải nghiệm người dùng:** Chỉ hiển thị các tab và bộ lọc phù hợp với vai trò và ngữ cảnh của người dùng.

#### 2. Phân tích Yêu cầu Chi tiết

##### 2.1. Đối với User có vai trò `Agent`

Giao diện `TicketQueuePage` sẽ hiển thị 4 tab sau chỉ khi user có vai trò `Agent`.

1.  **Assigned Ticket:**
    -   **Nội dung:** Chỉ hiển thị các ticket được gán cho chính user đang đăng nhập.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Open`, `InProgress`, hoặc `OnHold`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

2.  **Active Ticket:**
    -   **Nội dung:** Hiển thị các ticket thuộc các nhóm (`GroupId`) mà user là thành viên.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Open` hoặc `InProgress`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

3.  **On Hold:**
    -   **Nội dung:** Hiển thị các ticket thuộc các nhóm (`GroupId`) mà user là thành viên.
    -   **Trạng thái:** Chỉ lọc các ticket có trạng thái là `OnHold`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

4.  **Archive:**
    -   **Nội dung:** Hiển thị các ticket thuộc các nhóm (`GroupId`) mà user là thành viên.
    -   **Trạng thái:** Lọc các ticket có trạng thái là `Resolved` hoặc `Closed`.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.

##### 2.2. Đối với User có quyền `Ticket Manager`

Người dùng có quyền `tickets:assign_to_group` sẽ thấy 2 tab đặc biệt sau:

1.  **Unassigned Ticket:**
    -   **Nội dung:** Hiển thị tất cả các ticket **chưa được gán vào bất kỳ nhóm nào** (`GroupId` is NULL).
    -   **Trạng thái:** Không lọc theo trạng thái mặc định.
    -   **Tìm kiếm:** Theo `Keyword` và `Priority`. Không có bộ lọc `Status`.
2.  **All Ticket:**
    -   **Nội dung:** Hiển thị **tất cả** các ticket trong hệ thống mà Manager có quyền xem.
    -   **Trạng thái:** Không lọc theo trạng thái mặc định.
    -   **Tìm kiếm:** Theo `Keyword`, `Priority`, và **`Status`**. Đây là tab duy nhất có bộ lọc `Status`.

#### 3. Kế hoạch Thực thi

##### Giai đoạn 0: Định nghĩa Enum dùng chung

Đây là bước nền tảng để đảm bảo sự nhất quán về trạng thái Ticket giữa Backend và Frontend.

**Task 0.1: Tạo Enum `TicketStatus` phía Backend**
-   **Mục đích:** Tạo một nguồn định nghĩa (source of truth) cho các trạng thái ticket trong C#, giúp loại bỏ "magic strings" và đảm bảo tính nhất quán trong logic nghiệp vụ.
-   **Thay đổi:**
    -   Tạo file mới `TechnicalSupport.Domain/Enums/TicketStatusEnum.cs`.
    -   Định nghĩa enum: `public enum TicketStatusEnum { Open, InProgress, OnHold, Resolved, Closed }`.
    -   *Lưu ý:* Enum này sẽ được dùng để so sánh trong logic nghiệp vụ (`nameof(TicketStatusEnum.Open)`), không nhất thiết phải bind trực tiếp từ request để giữ cho API linh hoạt.
-   **File ảnh hưởng:** `TechnicalSupport.Domain/Enums/TicketStatusEnum.cs` (mới).

**Task 0.2: Tạo Enum `TicketStatus` phía Frontend**
-   **Mục đích:** Cung cấp một Enum an toàn kiểu dữ liệu (type-safe) trong TypeScript để sử dụng trong các component và lời gọi API.
-   **Thay đổi:**
    -   Tạo file mới `technical-support-ui/src/types/enums.ts`.
    -   Định nghĩa enum: `export enum TicketStatus { Open = 'Open', InProgress = 'InProgress', OnHold = 'OnHold', Resolved = 'Resolved', Closed = 'Closed' }`.
-   **File ảnh hưởng:** `technical-support-ui/src/types/enums.ts` (mới).

---

##### Giai đoạn 1: Cập nhật Backend (API)

**Task 1.1: Mở rộng `TicketFilterParams.cs`**
-   **Mục đích:** Bổ sung các tham số mới, tường minh hơn để API có thể nhận diện các kiểu lọc phức tạp.
-   **Thay đổi:**
    -   Trong `TechnicalSupport.Application/Common/TicketFilterParams.cs`:
    -   Xóa `public List<int>? StatusIds { get; set; }`.
    -   Thêm `public List<string>? Statuses { get; set; }`.
    -   Xóa `public bool? TicketForMyGroup { get; set; }`.
    -   Xóa `public string? AssigneeId { get; set; }`.
    -   Thêm `public bool? MyTicket { get; set; }`.
    -   Thêm `public bool? MyGroupTicket { get; set; }`.
    -   Giữ nguyên `public bool? CreatedByMe { get; set; }`.
    -   **Giữ nguyên `public bool? unassignedToGroupOnly { get; set; }` để lọc các ticket chưa được phân vào nhóm.**
-   **File ảnh hưởng:**
    -   `TechnicalSupport.Application/Common/TicketFilterParams.cs`
    -   **File liên quan (FE):** `technical-support-ui/src/types/entities.ts` (cần được đồng bộ ở Giai đoạn 2).

**Task 1.2: Cập nhật logic lọc trong `TicketService.cs`**
-   **Mục đích:** Triển khai logic lọc mới dựa trên `TicketFilterParams` đã được mở rộng.
-   **Thay đổi:**
    -   Trong `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`, phương thức `GetTicketsAsync`:
        -   **Thêm logic để lọc theo các thuộc tính mới:** `MyTicket`, `MyGroupTicket`.
        -   **Cập nhật logic lọc trạng thái:** Sử dụng `Statuses` (List<string>) thay vì `StatusIds`. Logic so sánh chuỗi nội bộ nên sử dụng hằng số từ `TicketStatusEnum` để đảm bảo chính xác.
        -   **Xóa bỏ logic cũ:** Loại bỏ các điều kiện `if` cho `TicketForMyGroup`, `AssigneeId`.
-   **File ảnh hưởng:** `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`.

**Task 1.3: Điều chỉnh Controller `TicketsController.cs`**
-   **Mục đích:** Đảm bảo controller có thể nhận đúng mảng `statuses` từ query string.
-   **Thay đổi:** Không cần thay đổi code. Chỉ cần nhận thức rằng ASP.NET Core có thể bind URL `?statuses=Open&statuses=Resolved` vào `List<string> Statuses`.
-   **File ảnh hưởng:** `TechnicalSupport.Api/Features/Tickets/TicketsController.cs`.

---

##### Giai đoạn 2: Tái cấu trúc Frontend (UI)

**Task 2.1: Đồng bộ hóa Type Definitions**
-   **Mục đích:** Cập nhật interface TypeScript để khớp với model C# đã thay đổi. Đây là bước quan trọng để đảm bảo an toàn kiểu dữ liệu.
-   **Thay đổi:**
    -   Trong `technical-support-ui/src/types/entities.ts`:
    -   Cập nhật `interface TicketFilterParams` để bao gồm `myTicket?: boolean`, `myGroupTicket?: boolean`, `statuses?: string[]`.
    -   Loại bỏ các thuộc tính cũ (`statusIds`, `assigneeId`, `ticketForMyGroup`).
    -   **Giữ lại `unassignedToGroupOnly?: boolean` để đồng bộ với backend.**
-   **File ảnh hưởng:** `technical-support-ui/src/types/entities.ts`.

**Task 2.2: Cập nhật `ticketService.ts`**
-   **Mục đích:** Điều chỉnh hàm gọi API để sử dụng interface `TicketFilterParams` mới.
-   **Thay đổi:**
    -   Trong `technical-support-ui/src/features/tickets/api/ticketService.ts`, hàm `getTickets`:
    -   Đảm bảo hàm chấp nhận đúng tham số kiểu `TicketFilterParams` đã được cập nhật ở Task 2.1.
-   **File ảnh hưởng:** `technical-support-ui/src/features/tickets/api/ticketService.ts`.

**Task 2.3: Tái cấu trúc `TicketQueuePage.tsx`**
-   **Mục đích:** Xóa bỏ cấu trúc cũ và thiết lập nền tảng cho hệ thống tab mới dựa trên vai trò.
-   **Thay đổi:**
    -   Xóa logic liên quan đến các tab "My Queue" và "Triage Queue" cũ.
    -   Sử dụng `useState` để quản lý tab đang hoạt động.
    -   Sử dụng component `<Tabs>` và `<Tab>` của Material-UI để dựng giao diện các tab.
-   **File ảnh hưởng:** `technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`.

**Task 2.4: Hiển thị Tab có điều kiện theo vai trò**
-   **Mục đích:** Hiển thị đúng bộ tab cho từng loại người dùng.
-   **Thay đổi:**
    -   Trong `TicketQueuePage.tsx`, sử dụng hook `useAuth()` để lấy vai trò và quyền.
    -   Dùng biến `const isAgent = user.roles.includes('Agent');`
    -   Dùng biến `const isTicketManager = hasPermission('tickets:assign_to_group');`
    -   Render 4 tab của Agent chỉ khi `isAgent` là `true`.
    -   Render 2 tab của Manager chỉ khi `isTicketManager` là `true`.

**Task 2.5: Implement Logic gọi API cho từng Tab**
-   **Mục đích:** Đảm bảo mỗi tab gọi API `getTickets` với tham số chính xác, sử dụng Enum đã định nghĩa.
-   **Thay đổi:**
    -   Import `TicketStatus` từ `src/types/enums.ts`.
    -   Sử dụng `useEffect` để gọi lại `fetchTickets` mỗi khi `activeTab` hoặc `filters` thay đổi.
    -   Bên trong `fetchTickets`, xây dựng đối tượng `params` dựa trên `activeTab`:
        -   `assigned`: `{ myTicket: true, statuses: [TicketStatus.Open, TicketStatus.InProgress, TicketStatus.OnHold] }`
        -   `active`: `{ myGroupTicket: true, statuses: [TicketStatus.Open, TicketStatus.InProgress] }`
        -   `onHold`: `{ myGroupTicket: true, statuses: [TicketStatus.OnHold] }`
        -   `archive`: `{ myGroupTicket: true, statuses: [TicketStatus.Resolved, TicketStatus.Closed] }`
        -   `unassigned`: `{ unassignedToGroupOnly: true }`
        -   `all`: `{}`
    -   Kết hợp `params` từ tab với `params` từ thanh tìm kiếm trước khi gọi API.

**Task 2.6: Đồng bộ hóa các Component còn lại**
-   **Mục đích:** Đồng bộ các thay đổi mới trên toàn bộ các component và trang có liên quan để đảm bảo tính nhất quán.
-   **Thay đổi:**
    -   **Thanh tìm kiếm (`TicketQueuePage.tsx`):**
        -   Chỉ hiển thị bộ lọc `Status` khi `isTicketManager && activeTab === 'all'`.
        -   Danh sách các `<MenuItem>` cho bộ lọc `Status` sẽ được render động từ `Object.values(TicketStatus)` để dễ bảo trì.
        -   Đảm bảo reset bộ lọc `status` khi chuyển tab.
    -   **Trang `MyTicketsPage.tsx`:**
        -   Trong `technical-support-ui/src/features/tickets/routes/MyTicketsPage.tsx`, cập nhật lời gọi API `getTickets` để sử dụng tham số `createdByMe: true`.
    -   **`Component TicketCard.tsx` (Bổ sung):**
        -   **Mục đích:** Đảm bảo component hiển thị đúng thông tin trạng thái từ enum mới.
        -   **Thay đổi:** Cập nhật logic hiển thị màu và nhãn của trạng thái để sử dụng `TicketStatus` enum thay vì chuỗi cố định, đảm bảo nhất quán với hệ thống.
    -   **`Trang TicketDetailPage.tsx` (Bổ sung):**
        -   **Mục đích:** Đồng bộ trang chi tiết ticket với hệ thống trạng thái mới.
        -   **Thay đổi:**
            -   **Dropdown Trạng thái:** Thay vì dùng danh sách `ALL_STATUSES` cố định, render động các `<MenuItem>` từ `TicketStatus` enum.
            -   **Kiểm tra Logic:** Rà soát lại logic hiển thị các nút hành động (Actions) để đảm bảo chúng vẫn hoạt động đúng với các quyền và ngữ cảnh mới.
-   **File ảnh hưởng:**
    -   `technical-support-ui/src/features/tickets/routes/TicketQueuePage.tsx`
    -   `technical-support-ui/src/features/tickets/routes/MyTicketsPage.tsx`
    -   **`technical-support-ui/src/features/tickets/components/TicketCard.tsx`**
    -   **`technical-support-ui/src/features/tickets/routes/TicketDetailPage.tsx`**