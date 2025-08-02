Chắc chắn rồi! Dựa trên tất cả các phân tích và ý tưởng chúng ta đã thảo luận, tôi đã tổng hợp lại thành một file `Plan.md` hoàn chỉnh.

Kế hoạch này không chỉ vạch ra các tính năng cần làm mà còn định hình một "bộ luật" về kiến trúc phân quyền, giúp bạn và đội ngũ có một lộ trình rõ ràng, nhất quán để xây dựng một hệ thống phân quyền toàn diện, an toàn và có khả năng mở rộng.

Đây là file kế hoạch chi tiết:

```markdown
# Kế hoạch Nâng cấp Hệ thống Phân quyền - TechnicalSupport API

## 1. Tầm nhìn và Mục tiêu

Tài liệu này đặt ra một lộ trình chiến lược để tái cấu trúc và hoàn thiện hệ thống phân quyền của TechnicalSupport API. Mục tiêu không chỉ là thêm tính năng, mà là xây dựng một nền tảng phân quyền **an toàn, linh hoạt, dễ bảo trì và có khả năng mở rộng**, tuân thủ các tiêu chuẩn ngành.

**Kết quả cuối cùng:** Một hệ thống mà ở đó việc quản lý quyền truy cập trở nên rõ ràng, tập trung và có thể tự động hóa, giảm thiểu rủi ro bảo mật và dễ dàng tích hợp với các hệ thống khác trong tương lai.

## 2. Các Nguyên tắc Thiết kế (Bộ luật chung)

Mọi thay đổi và triển khai mới trong hệ thống phân quyền **BẮT BUỘC** phải tuân thủ các nguyên tắc cốt lõi sau:

1.  **Tách biệt Vai trò và Quyền:**
    *   **Role (Vai trò):** Định nghĩa *Người dùng là ai* (VD: Client, Agent, Admin). Chỉ dùng để nhóm người dùng.
    *   **Policy (Chính sách):** Định nghĩa *Người dùng được làm gì* (VD: `CanManageUsers`). Là đơn vị kiểm tra quyền chính.

2.  **Ưu tiên Chính sách (Policy-Over-Role):**
    *   Luôn sử dụng `[Authorize(Policy = "...")]` trong các Controller.
    *   **CẤM** sử dụng `[Authorize(Roles = "...")]` để tránh việc hard-code vai trò và làm logic phân mảnh.

3.  **Phân quyền dựa trên Ngữ cảnh Tài nguyên (Resource-Based Authorization):**
    *   Quyền truy cập phải được xác định dựa trên mối quan hệ của người dùng với tài nguyên (VD: *người tạo*, *người được gán*).
    *   Logic này phải được đóng gói trong các lớp `AuthorizationHandler` chuyên biệt, không được viết trực tiếp trong service hoặc controller.

4.  **Nguyên tắc Đặc quyền Tối thiểu (Least Privilege):**
    *   Người dùng chỉ được cấp những quyền tối thiểu cần thiết để thực hiện công việc. Mặc định là không có quyền.

## 3. Mô hình Phân quyền Tiêu chuẩn

### 3.1. Các Vai trò (Roles)

| Tên Vai trò | Mô tả |
| :--- | :--- |
| **`Client`** | Khách hàng, người tạo ticket. |
| **`Agent`** | Nhân viên hỗ trợ, người xử lý ticket. |
| **`Manager`** | Quản lý nhóm, có quyền giám sát và điều phối. |
| **`Admin`** | Quản trị viên hệ thống, có toàn quyền. |

### 3.2. Các Chính sách (Policies)

Các policy sau sẽ được định nghĩa tập trung trong `Program.cs`.

| Tên Policy | Mô tả | Yêu cầu Vai trò |
| :--- | :--- | :--- |
| `RequireAuthenticatedUser` | Yêu cầu người dùng phải đăng nhập. | Bất kỳ vai trò nào đã xác thực. |
| `CanCreateTicket` | Có thể tạo ticket mới. | `Client` |
| `CanManageOwnTickets` | Có thể quản lý các ticket do mình tạo. | `Client` |
| `CanProcessTickets` | Có thể xử lý các ticket được gán. | `Agent`, `Manager`, `Admin` |
| `CanAssignTickets` | Có thể gán ticket cho người khác. | `Manager`, `Admin` |
| `CanManageUsers` | Có thể quản lý người dùng (thêm, sửa, xóa). | `Admin` |
| `CanManageGroups` | Có thể quản lý nhóm và thành viên. | `Manager`, `Admin` |
| `CanAccessReporting` | Có thể truy cập các báo cáo, thống kê. | `Manager`, `Admin` |

## 4. Lộ trình Triển khai (TODO)

### Giai đoạn 1: Đặt nền móng và Tái cấu trúc

**Mục tiêu:** Chuyển đổi hệ thống hiện tại sang mô hình Policy-Based và Resource-Based.

#### **Task 1.1: Định nghĩa Policies và Operations**
1.  **Tạo Policies:** Trong `Program.cs`, định nghĩa tất cả các policies đã nêu trong mục `3.2`.
2.  **Tạo Operations:** Tạo các lớp tĩnh để định nghĩa các hành động trên tài nguyên:
    *   `TicketOperations.cs` (Create, Read, Update, Delete, Assign, ChangeStatus, AddComment, UploadFile).
    *   `CommentOperations.cs` (Read, Update, Delete).
    *   `UserOperations.cs` (ListAll, ReadProfile, UpdateProfile, ChangeRole, DeleteUser).
3.  **Cập nhật Controllers:** Thay thế toàn bộ `[Authorize(Roles = "...")]` bằng `[Authorize(Policy = "...")]` tương ứng.

#### **Task 1.2: Xây dựng Authorization Handlers**
1.  **Tạo `TicketAuthorizationHandler.cs`:**
    *   Triển khai logic kiểm tra quyền chi tiết cho `Ticket` dựa trên vai trò và mối quan hệ (người tạo, người được gán, thành viên nhóm).
    *   Ví dụ: Kiểm tra `TicketOperations.Update` -> `Admin` được, `Manager` được nếu ticket thuộc nhóm mình, `Agent` được nếu ticket gán cho mình.
2.  **Tạo `CommentAuthorizationHandler.cs`:**
    *   Triển khai logic cho `Comment` (VD: chỉ người tạo hoặc admin mới được sửa/xóa).
3.  **Đăng ký Handlers:** Đăng ký các handler trên vào DI container.

#### **Task 1.3: Tái cấu trúc Services**
1.  **Tiêm `IAuthorizationService`:** Tiêm `IAuthorizationService` và `IHttpContextAccessor` vào `TicketService`, `CommentService`, `AdminService`.
2.  **Loại bỏ Logic phân quyền thủ công:**
    *   Tìm và xóa các đoạn code `if (user.IsInRole...)` hoặc `if (ticket.CustomerId == userId)` dùng để kiểm tra quyền.
    *   Thay thế bằng một lệnh gọi duy nhất: `var result = await _authorizationService.AuthorizeAsync(user, resource, operation);`.
    *   Nếu `!result.Succeeded`, ném `UnauthorizedAccessException`.

### Giai đoạn 2: Hoàn thiện Luồng công việc Ticket

**Mục tiêu:** Triển khai các tính năng còn thiếu để luồng xử lý ticket được hoàn chỉnh.

#### **Task 2.1: Gán Ticket cho Nhóm**
1.  **Tạo API:**
    *   Endpoint: `PUT /api/tickets/{id}/assign-group`
    *   DTO: `AssignGroupModel { int GroupId }`
2.  **Cập nhật `TicketService`:**
    *   Thêm phương thức `AssignTicketToGroupAsync`.
    *   Khi gán cho nhóm, `AssigneeId` phải được set về `null`.
3.  **Cập nhật Logic `GetTicketsAsync`:**
    *   `Agent`/`Manager` phải thấy các ticket được gán cho nhóm mà họ là thành viên.
    *   Logic: `WHERE t.AssigneeId == myId OR (t.AssigneeId == null AND t.GroupId IN (myGroupIds))`

#### **Task 2.2: Cho phép Agent tự nhận Ticket**
1.  **Tái sử dụng API:** `PUT /api/tickets/{id}/assign`
2.  **Cập nhật `TicketAuthorizationHandler`:**
    *   Thêm logic cho phép một `Agent` thực hiện `TicketOperations.Assign` trên một ticket **NẾU**:
        *   Ticket đó chưa được gán cho cá nhân (`AssigneeId == null`).
        *   Ticket đó thuộc một trong các nhóm của `Agent` đó.
        *   Agent đó đang cố gắng gán ticket cho chính mình.

### Giai đoạn 3: Tự động hóa và Nâng cao

**Mục tiêu:** Xây dựng chức năng yêu cầu cấp quyền để giảm tải cho Admin và tăng tính linh hoạt.

#### **Task 3.1: Tạo cơ sở dữ liệu cho Yêu cầu Quyền**
1.  **Tạo Entity `PermissionRequest`:**
    *   Bao gồm các trường: `RequesterId`, `RequestedPermission` (chuỗi định danh quyền), `ResourceId`, `ResourceType`, `Justification` (lý do), `Status` (enum `Pending`, `Approved`, `Rejected`), `ProcessorId`, `ProcessedAt`, `ProcessorNotes`.
2.  **(Tùy chọn nâng cao) Tạo Entity `TemporaryPermission`:**
    *   Bao gồm: `UserId`, `ClaimType`, `ClaimValue`, `ExpirationAt`. Dùng để lưu các quyền được cấp có thời hạn.
3.  **Tạo và áp dụng DB Migrations.**

#### **Task 3.2: Xây dựng API quản lý Yêu cầu**
1.  **Tạo `PermissionRequestsController`:**
    *   `POST /`: Gửi một yêu cầu mới.
    *   `GET /`: Admin/Manager lấy danh sách các yêu cầu đang chờ.
    *   `PUT /{id}/approve`: Phê duyệt yêu cầu.
    *   `PUT /{id}/reject`: Từ chối yêu cầu.
2.  **Tạo `PermissionRequestService`:**
    *   **Logic Phê duyệt:**
        *   Phân tích chuỗi `RequestedPermission`.
        *   Nếu là yêu cầu vai trò, gọi `UserManager` để thêm vai trò.
        *   Nếu là yêu cầu quyền tạm thời, thêm một bản ghi vào bảng `TemporaryPermission`.
        *   Cập nhật trạng thái của yêu cầu.
        *   Gửi thông báo cho người yêu cầu (qua SignalR hoặc email).

#### **Task 3.3: Tích hợp vào hệ thống phân quyền**
1.  **Cập nhật các `AuthorizationHandler`:**
    *   Sửa đổi các handler (VD: `TicketAuthorizationHandler`) để khi kiểm tra quyền, chúng không chỉ kiểm tra vai trò/chính sách mà còn phải **kiểm tra bảng `TemporaryPermission`** để xem người dùng có quyền truy cập tạm thời trên tài nguyên đó hay không.

---

Bằng cách thực hiện theo kế hoạch này, bạn sẽ không chỉ hoàn thành các tính năng cần thiết mà còn xây dựng được một hệ thống phân quyền vững chắc, chuyên nghiệp, sẵn sàng cho sự phát triển trong tương lai.

```