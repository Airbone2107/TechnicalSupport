# PLAN.MD: CHIẾN LƯỢC TÍCH HỢP HỆ THỐNG PHÂN QUYỀN POLICY

Tài liệu này vạch ra kế hoạch chi tiết để triển khai và tích hợp hệ thống phân quyền dựa trên Policy từ backend vào frontend. Mục tiêu là xây dựng một hệ thống linh hoạt, an toàn và dễ bảo trì, hỗ trợ các chức năng cốt lõi như yêu cầu/cấp quyền, cấp quyền tạm thời và truy xuất dữ liệu theo đúng thẩm quyền.

## 1. Mục tiêu Chính

-   **Backend:** Xây dựng một hệ thống phân quyền mạnh mẽ, nơi các quy tắc nghiệp vụ (ai được làm gì, xem gì) được định nghĩa và thực thi một cách tập trung.
-   **Frontend:** Xây dựng giao diện người dùng (UI) "thông minh", có khả năng tự động điều chỉnh (ẩn/hiện chức năng, giới hạn hành động) dựa trên quyền hạn của người dùng được trả về từ backend.
-   **Luồng nghiệp vụ:** Hoàn thiện các luồng chức năng quan trọng:
    -   **Yêu cầu & Cấp quyền:** Agent/Manager có thể yêu cầu quyền cao hơn (VD: quyền Manager). Admin/Manager có thể duyệt và cấp/từ chối các yêu cầu đó.
    -   **Cấp quyền Tạm thời:** Admin/Manager có thể cấp quyền truy cập tạm thời vào một tài nguyên cụ thể (VD: cho phép một Agent xem một ticket không thuộc thẩm quyền của mình trong 1 giờ).
    -   **Phân quyền Dữ liệu (Data Scoping):** Hệ thống phải tự động lọc và hiển thị đúng dữ liệu cho từng vai trò (Client chỉ thấy ticket của mình, Agent thấy ticket của mình và của nhóm, Admin/Manager thấy nhiều hơn).

## 2. Phân tích Hiện trạng

### Backend
-   **Điểm mạnh:**
    -   Kiến trúc Clean Architecture đã được thiết lập tốt, phân tách rõ ràng các tầng.
    -   Hệ thống xác thực JWT và Identity đã hoạt động.
    -   Các thực thể (Entities) cần thiết như `PermissionRequest`, `TemporaryPermission`, `Group` đã tồn tại trong Domain, tạo nền tảng vững chắc.
    -   Sử dụng `AuthorizationHandler` là một phương pháp tiếp cận đúng đắn cho việc phân quyền phức tạp.
-   **Điểm cần cải thiện:**
    -   `TicketAuthorizationHandler` hiện tại còn đơn giản, cần được mở rộng để xử lý các logic phức tạp hơn (quyền trên nhóm, quyền tạm thời).
    -   `TicketService` chưa thực hiện việc lọc dữ liệu (data scoping) dựa trên vai trò của người dùng.
    -   `PermissionRequestService` chưa triển khai logic thực tế khi một yêu cầu được duyệt (chưa thực sự cấp role hay quyền tạm thời).
    -   Endpoint `GET /tickets/{id}` có thể được tối ưu để trả về thêm dữ liệu liên quan (comments, attachments) nhằm giảm số lượng lệnh gọi API từ frontend.

### Frontend
-   **Điểm mạnh:**
    -   Cấu trúc component và routing rõ ràng.
    -   `AuthContext` là nơi lý tưởng để quản lý trạng thái xác thực và phân quyền một cách tập trung.
    -   Việc sử dụng `ProtectedRoute` để bảo vệ các route là một thực hành tốt.
-   **Điểm cần cải thiện:**
    -   Hàm `hasRole` trong `AuthContext` hiện đang quá đơn giản, chưa hiểu được sự phân cấp giữa các vai trò (VD: `Manager` cũng là một `Agent` và có các quyền của Agent).
    -   `lib/jwt.ts` cần được điều chỉnh để đọc đúng claim `role` do ASP.NET Core Identity trả về.
    -   Giao diện cho các chức năng yêu cầu/duyệt quyền chưa được xây dựng.
    -   Các component chưa thực hiện việc ẩn/hiện các nút hành động (VD: nút "Xóa", "Gán việc") dựa trên quyền của người dùng.

## 3. Kế hoạch Thực thi Chi tiết

Kế hoạch sẽ được chia thành 2 giai đoạn chính, thực hiện song song hoặc tuần tự: **Củng cố Backend** và **Tích hợp Frontend**.

---

### **Giai đoạn 1: Củng cố Nền tảng Phân quyền ở Backend**

Mục tiêu của giai đoạn này là đảm bảo mọi quy tắc phân quyền được định nghĩa và thực thi một cách không thể lay chuyển ở phía server.

#### **Bước 1.1: Hoàn thiện `TicketAuthorizationHandler` và các `Handler` khác**
-   **Nhiệm vụ:** Mở rộng `TicketAuthorizationHandler` để kiểm tra các điều kiện phức tạp:
    1.  **Quyền sở hữu:** Người dùng có phải là `Customer` của ticket không?
    2.  **Quyền được giao:** Người dùng có phải là `Assignee` của ticket không?
    3.  **Quyền theo nhóm:** Ticket có thuộc một `Group` mà người dùng là thành viên không?
    4.  **Quyền tạm thời:** Kiểm tra bảng `TemporaryPermissions` xem người dùng có được cấp quyền tạm thời cho hành động (`Operation`) và tài nguyên (`Resource`) này không. Đảm bảo kiểm tra `ExpirationAt`.
-   **File ảnh hưởng:** `TechnicalSupport.Infrastructure/Authorization/TicketAuthorizationHandler.cs`, `CommentAuthorizationHandler.cs`.

#### **Bước 1.2: Triển khai Phân quyền Dữ liệu (Data Scoping) trong `TicketService`**
-   **Nhiệm vụ:** Sửa đổi phương thức `GetTicketsAsync` để tự động lọc dữ liệu dựa trên vai trò của người dùng đang thực hiện yêu cầu.
    -   **Nếu là `Client`:** Chỉ trả về các ticket có `CustomerId` là ID của người dùng.
    -   **Nếu là `Agent` (và không phải Manager/Admin):** Trả về các ticket được gán trực tiếp cho họ (`AssigneeId`) HOẶC các ticket chưa được gán nhưng thuộc các `Group` mà họ là thành viên.
    -   **Nếu là `Manager`:** Trả về các ticket của Agent, cộng với tất cả các ticket thuộc các `Group` mà Manager quản lý.
    -   **Nếu là `Admin`:** Trả về tất cả ticket.
-   **File ảnh hưởng:** `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`.

#### **Bước 1.3: Hoàn thiện Luồng Yêu cầu/Cấp quyền**
-   **Nhiệm vụ:** Triển khai logic thực sự bên trong phương thức `ApproveRequestAsync` của `PermissionRequestService`.
    -   Đọc chuỗi `RequestedPermission` (VD: `"ROLE:Manager"` hoặc `"TEMP_PERM:Ticket:123:Update:3600"`).
    -   Nếu là `ROLE`: Dùng `UserManager.AddToRoleAsync()` để thêm vai trò cho người yêu cầu.
    -   Nếu là `TEMP_PERM`: Tạo một bản ghi mới trong bảng `TemporaryPermissions` với `UserId`, `ClaimType`, `ClaimValue` và `ExpirationAt` tương ứng.
-   **File ảnh hưởng:** `TechnicalSupport.Infrastructure/Features/Permissions/PermissionRequestService.cs`.

#### **Bước 1.4: Tối ưu Endpoint và DTO**
-   **Nhiệm vụ:** Cải thiện endpoint `GET /tickets/{id}` để trả về một đối tượng `TicketDto` đầy đủ hơn, bao gồm cả danh sách `Comments` và `Attachments`.
-   **Thực hiện:**
    -   Trong `TicketService.GetTicketByIdAsync`, sử dụng `Include()` và `ThenInclude()` để tải sẵn các dữ liệu liên quan.
    -   Trong `TicketDto`, thêm các thuộc tính `public List<CommentDto> Comments { get; set; }` và `public List<AttachmentDto> Attachments { get; set; }`.
    -   Cập nhật `MappingProfile` để xử lý việc ánh xạ này.
-   **File ảnh hưởng:** `TechnicalSupport.Application/Features/Tickets/DTOs/TicketDto.cs`, `TechnicalSupport.Infrastructure/Features/Tickets/TicketService.cs`, `TechnicalSupport.Application/Mappings/MappingProfile.cs`.

---

### **Giai đoạn 2: Tích hợp và Hiện thực hóa ở Frontend**

Mục tiêu của giai đoạn này là làm cho UI phản ánh chính xác các quyền hạn mà backend đã định nghĩa, tạo ra trải nghiệm người dùng mượt mà và an toàn.

#### **Bước 2.1: Nâng cấp `AuthContext` và `jwt.ts`**
-   **Nhiệm vụ:**
    1.  Trong `lib/jwt.ts`, sửa hàm `decodeToken` để đọc chính xác claim về vai trò từ token: `decoded.role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];`.
    2.  Trong `contexts/AuthContext.tsx`, viết lại hoàn toàn hàm `hasRole` để nó hiểu được sự phân cấp quyền.
        -   Định nghĩa một đối tượng `roleHierarchy`: `{ Client: 1, Agent: 2, Manager: 3, Admin: 4 }`.
        -   Hàm `hasRole` sẽ so sánh cấp bậc của người dùng với cấp bậc yêu cầu, thay vì chỉ tìm kiếm chuỗi. Ví dụ, `hasRole('Agent')` sẽ trả về `true` cho cả người dùng có vai trò `Manager` và `Admin`.
-   **File ảnh hưởng:** `technical-support-ui/src/lib/jwt.ts`, `technical-support-ui/src/contexts/AuthContext.tsx`.

#### **Bước 2.2: Xây dựng Giao diện Quản lý Quyền**
-   **Nhiệm vụ:** Tạo các trang và component mới cho luồng yêu cầu và duyệt quyền.
    -   **Trang `RequestPermissionPage`:** Dành cho `Agent`/`Manager`. Gồm một form để nhập loại quyền muốn yêu cầu (có thể là dropdown hoặc input text) và một `textarea` cho lý do.
    -   **Trang `ReviewPermissionPage`:** Dành cho `Manager`/`Admin`. Hiển thị danh sách các yêu cầu đang chờ xử lý (`pending`). Mỗi yêu cầu có nút "Approve" và "Reject". Khi nhấn, một popup/modal hiện ra yêu cầu nhập ghi chú trước khi xác nhận.
-   **File ảnh hưởng (mới):** `features/permissions/routes/RequestPermissionPage.tsx`, `features/permissions/routes/ReviewPermissionPage.tsx`, `features/permissions/api/permissionService.ts`.

#### **Bước 2.3: Áp dụng Phân quyền Hiển thị (Conditional Rendering)**
-   **Nhiệm vụ:** Sử dụng hàm `hasRole` đã được nâng cấp trong `AuthContext` để ẩn/hiện các phần tử UI trên toàn bộ ứng dụng.
    -   **Sidebar:** Chỉ hiển thị các menu item "Manage Users", "Manage Groups", "Permission Requests" nếu `hasRole` trả về `true` cho vai trò tương ứng.
    -   **TicketDetailPage:**
        -   Chỉ hiển thị nút "Assign Ticket", "Assign to Group" nếu `hasRole('Manager')`.
        -   Chỉ hiển thị nút "Delete Ticket" nếu `hasRole('Admin')`.
        -   Dropdown thay đổi trạng thái chỉ cho phép `Agent` trở lên tương tác.
    -   **Dashboard:** Các thẻ điều hướng nhanh sẽ được hiển thị dựa trên vai trò.
-   **File ảnh hưởng:** `components/Sidebar.tsx`, `features/tickets/routes/TicketDetailPage.tsx`, `features/tickets/routes/DashboardPage.tsx`.

#### **Bước 2.4: Hoàn thiện Luồng Hiển thị Dữ liệu**
-   **Nhiệm vụ:** Đảm bảo các trang danh sách ticket gọi API và hiển thị dữ liệu một cách chính xác.
    -   **MyTicketsPage (Client):** Gọi `GET /tickets`. Backend sẽ tự động chỉ trả về ticket của Client này.
    -   **TicketQueuePage (Agent/Manager):** Gọi `GET /tickets` (có thể kèm filter). Backend sẽ tự động trả về ticket của Agent và các ticket trong nhóm của họ.
    -   Frontend không cần logic phức tạp để lọc dữ liệu, chỉ cần hiển thị những gì nhận được.
-   **File ảnh hưởng:** `features/tickets/routes/MyTicketsPage.tsx`, `features/tickets/routes/TicketQueuePage.tsx`.

## 4. Lợi ích của Chiến lược
-   **An toàn & Bảo mật:** Mọi quy tắc phân quyền được thực thi ở backend, ngăn chặn mọi nỗ lực truy cập trái phép từ client.
-   **Dễ bảo trì:** Logic nghiệp vụ được tập trung ở `Service` và `AuthorizationHandler` ở backend. Frontend chỉ đóng vai trò hiển thị, giúp việc sửa đổi, nâng cấp sau này trở nên dễ dàng hơn.
-   **Trải nghiệm Người dùng Tốt:** Giao diện tự động thích ứng với người dùng, chỉ hiển thị các chức năng và dữ liệu mà họ được phép thấy, tránh gây nhầm lẫn.
-   **Hiệu suất:** Việc lọc dữ liệu ở backend giúp giảm lượng dữ liệu truyền về client, tăng tốc độ tải trang.