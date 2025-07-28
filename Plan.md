# Kế hoạch Phát triển - TechnicalSupport API

## 1. Mục tiêu

Tài liệu này nhằm mục đích cung cấp một lộ trình rõ ràng cho các nhà phát triển tiếp theo của dự án TechnicalSupport API. Nó bao gồm hai phần chính:
1.  **Tổng quan các Endpoints và Tính năng đã hoàn thành**: Liệt kê và giải thích các chức năng đã có sẵn trong hệ thống.
2.  **Kế hoạch phát triển các tính năng mới (TODO)**: Đề xuất và mô tả chi tiết các API cần được xây dựng để hoàn thiện sản phẩm.

## 2. Tổng quan và Các quy ước cần tuân thủ

Trước khi bắt đầu, vui lòng đọc kỹ file `TECHNICAL_DOCS.md` để nắm rõ các quy tắc về kiến trúc và luồng dữ liệu của dự án. Dưới đây là tóm tắt nhanh:

-   **Kiến trúc**: Clean Architecture (Domain -> Application -> Infrastructure -> Api).
-   **Chuẩn Response**: Mọi endpoint **BẮT BUỘC** phải trả về đối tượng `ApiResponse<T>`. Sử dụng `ApiResponse.Success(data, message)` cho thành công và `ApiResponse.Fail(message, errors)` cho thất bại.
-   **Validation**: Sử dụng **FluentValidation**. Mọi model đầu vào từ client phải có một Validator tương ứng. `ValidationFilter` sẽ tự động xử lý và trả về lỗi 400 nếu dữ liệu không hợp lệ.
-   **Xác thực**: Sử dụng **JWT Bearer Token**. Các endpoint yêu cầu đăng nhập cần được đánh dấu bằng `[Authorize]`.

---

## 3. Endpoints & Tính năng đã Hoàn thành

Dưới đây là danh sách các API đã được triển khai và đang hoạt động.

### 3.1. Module Xác thực (`/Auth`)

Controller chịu trách nhiệm đăng ký và đăng nhập người dùng.

#### `POST /Auth/register`
-   **Mô tả**: Đăng ký một tài khoản người dùng mới.
-   **Xác thực**: Không yêu cầu.
-   **Request Body**:
    ```csharp
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public string? Expertise { get; set; } // Dành cho Technician
        public string? Role { get; set; } // "Client" hoặc "Technician"
    }
    ```
-   **Response (Thành công 200 OK)**:
    ```json
    {
      "succeeded": true,
      "message": "User registered successfully.",
      "data": null,
      "errors": null
    }
    ```

#### `POST /Auth/login`
-   **Mô tả**: Đăng nhập vào hệ thống và nhận về JWT token.
-   **Xác thực**: Không yêu cầu.
-   **Request Body**:
    ```csharp
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    ```
-   **Response (Thành công 200 OK)**:
    ```json
    {
      "succeeded": true,
      "message": "Login successful.",
      "data": {
        "token": "your_jwt_token_here"
      },
      "errors": null
    }
    ```

### 3.2. Module Quản lý Ticket (`/Tickets`)

Controller quản lý các yêu cầu hỗ trợ (tickets).

#### `GET /Tickets`
-   **Mô tả**: Lấy danh sách ticket có phân trang.
-   **Xác thực**: Yêu cầu (Role: `Client` hoặc `Technician`).
-   **Logic phân quyền**:
    -   Nếu là `Client`, chỉ thấy các ticket do mình tạo.
    -   Nếu là `Technician`, thấy các ticket được gán cho mình hoặc chưa được gán cho ai.
-   **Query Parameters**: `pageNumber` (số trang), `pageSize` (số lượng mỗi trang).
-   **Response (Thành công 200 OK)**: `ApiResponse<PagedResult<TicketDto>>`.

#### `GET /Tickets/{id}`
-   **Mô tả**: Lấy thông tin chi tiết của một ticket theo `id`.
-   **Xác thực**: Yêu cầu.
-   **Response (Thành công 200 OK)**: `ApiResponse<TicketDto>`.

#### `POST /Tickets`
-   **Mô tả**: Người dùng (Client) tạo một ticket mới.
-   **Xác thực**: Yêu cầu (Role: `Client`).
-   **Request Body**:
    ```csharp
    public class CreateTicketModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int StatusId { get; set; } // Thường là "Open"
        public string? Priority { get; set; }
    }
    ```
-   **Response (Thành công 201 Created)**: `ApiResponse<TicketDto>`.

#### `PUT /Tickets/{id}/status`
-   **Mô tả**: Cập nhật trạng thái của một ticket.
-   **Xác thực**: Yêu cầu.
-   **Request Body**:
    ```csharp
    public class UpdateStatusModel
    {
        public int StatusId { get; set; }
    }
    ```
-   **Response (Thành công 200 OK)**: `ApiResponse<TicketDto>`.

#### `POST /Tickets/{id}/comments`
-   **Mô tả**: Thêm một bình luận mới vào ticket.
-   **Xác thực**: Yêu cầu.
-   **Request Body**:
    ```csharp
    public class AddCommentModel
    {
        public string Content { get; set; }
    }
    ```
-   **Response (Thành công 200 OK)**: `ApiResponse<CommentDto>`.

---

## 4. Kế hoạch Phát triển (TODO)

Đây là danh sách các tính năng và API cần được xây dựng tiếp theo.

### 4.1. Cải thiện Quản lý Ticket

#### **Task 1.1: Gán Ticket cho Technician**

-   **Endpoint**: `PUT /tickets/{id}/assign`
-   **Mô tả**: Cho phép `Admin` hoặc `Technician` gán một ticket cho một `Technician` cụ thể (hoặc tự gán cho chính mình).
-   **Controller**: `TicketsController.cs`
-   **Phân quyền**: `[Authorize(Roles = "Admin,Technician")]`
-   **DTOs cần tạo/sử dụng**:
    -   Tạo `AssignTicketModel.cs` trong `TechnicalSupport.Application/Features/Tickets/DTOs`:
      ```csharp
      namespace TechnicalSupport.Application.Features.Tickets.DTOs
      {
          public class AssignTicketModel
          {
              // ID của Technician sẽ được gán
              public string AssigneeId { get; set; }
          }
      }
      ```
    -   Tạo `AssignTicketModelValidator.cs`.
-   **Method trong `ITicketService`**:
    ```csharp
    Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model, string currentUserId);
    ```
-   **Method trong `TicketsController`**:
    ```csharp
    [HttpPut("{id}/assign")]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ticketDto = await _ticketService.AssignTicketAsync(id, model, currentUserId);
        if (ticketDto == null)
        {
            return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
        }
        return Ok(ApiResponse.Success(ticketDto, "Ticket assigned successfully."));
    }
    ```

#### **Task 1.2: Mở rộng Lọc & Tìm kiếm Tickets**

-   **Endpoint**: `GET /Tickets` (mở rộng)
-   **Mô tả**: Thêm các tham số lọc và tìm kiếm vào endpoint lấy danh sách ticket.
-   **Controller**: `TicketsController.cs`
-   **Phân quyền**: Giữ nguyên.
-   **Các tham số mới cần thêm vào `PaginationParams` hoặc một class mới `TicketFilterParams`**:
    -   `statusId` (lọc theo trạng thái)
    -   `priority` (lọc theo độ ưu tiên)
    -   `assigneeId` (lọc theo technician được gán)
    -   `searchQuery` (tìm kiếm theo từ khóa trong `Title` và `Description`)
-   **Công việc**:
    1.  Cập nhật class `PaginationParams` hoặc tạo class `TicketFilterParams` mới.
    2.  Cập nhật method `GetTicketsAsync` trong `TicketService` để xử lý các tham số lọc này, thêm các mệnh đề `.Where()` tương ứng vào câu truy vấn IQueryable.

### 4.2. Quản lý File đính kèm (Attachments)

#### **Task 2.1: Lấy danh sách & Upload File**
-   **Mô tả**: Cho phép người dùng upload file đính kèm cho ticket và xem danh sách các file đã upload.
-   **Controller mới**: `AttachmentsController.cs`
-   **Endpoint 1**: `POST /tickets/{ticketId}/attachments`
    -   **Mô tả**: Upload một hoặc nhiều file.
    -   **Phân quyền**: `[Authorize]` (người dùng phải có quyền truy cập ticket).
    -   **Request**: `IFormFile` (sử dụng `multipart/form-data`).
    -   **Logic**:
        1.  Kiểm tra quyền của người dùng với `ticketId`.
        2.  Lưu file vào một thư mục an toàn trên server (ví dụ: `wwwroot/attachments/{ticketId}/`).
        3.  Tạo một bản ghi mới trong bảng `Attachments`.
-   **Endpoint 2**: `GET /tickets/{ticketId}/attachments`
    -   **Mô tả**: Lấy danh sách các file đính kèm của một ticket.
    -   **Phân quyền**: `[Authorize]`.
    -   **Response**: `ApiResponse<List<AttachmentDto>>` (cần tạo `AttachmentDto`).

#### **Task 2.2: Tải và Xóa File**
-   **Controller**: `AttachmentsController.cs`
-   **Endpoint 3**: `GET /attachments/{attachmentId}`
    -   **Mô tả**: Tải về một file.
    -   **Phân quyền**: `[Authorize]`.
    -   **Logic**: Đọc file từ đường dẫn đã lưu và trả về `FileResult`.
-   **Endpoint 4**: `DELETE /attachments/{attachmentId}`
    -   **Mô tả**: Xóa một file đính kèm.
    -   **Phân quyền**: `[Authorize]` (chỉ người upload hoặc Admin).
    -   **Logic**: Xóa file vật lý và xóa bản ghi trong DB.

### 4.3. Quản lý Người dùng và Nhóm (Dành cho Admin)

#### **Task 3.1: Tạo Admin Controller**
-   Tạo controller mới: `AdminController.cs` với `[Route("admin")]` và `[Authorize(Roles = "Admin")]`.
-   Tạo một service mới `IAdminService` và implementation `AdminService.cs`.

#### **Task 3.2: API Quản lý Người dùng**
-   **Endpoint 1**: `GET /admin/users`
    -   **Mô tả**: Lấy danh sách tất cả người dùng với phân trang.
    -   **Response**: `ApiResponse<PagedResult<UserDetailDto>>`.
-   **Endpoint 2**: `GET /admin/users/{userId}`
    -   **Mô tả**: Lấy chi tiết một người dùng.
    -   **Response**: `ApiResponse<UserDetailDto>`.
-   **Endpoint 3**: `PUT /admin/users/{userId}`
    -   **Mô tả**: Cập nhật thông tin người dùng (DisplayName, Roles, Lockout...).
    -   **Request Body**: `UpdateUserByAdminModel`.

#### **Task 3.3: API Quản lý Nhóm (Groups)**
-   **Mô tả**: Các API để tạo, sửa, xóa nhóm và quản lý thành viên trong nhóm.
-   **Controller mới**: `GroupsController.cs` (`[Route("groups")]`, `[Authorize(Roles = "Admin")]`).
-   **Endpoint 1**: `POST /groups` (Tạo nhóm mới).
-   **Endpoint 2**: `GET /groups` (Lấy danh sách nhóm).
-   **Endpoint 3**: `POST /groups/{groupId}/members` (Thêm technician vào nhóm).
-   **Endpoint 4**: `DELETE /groups/{groupId}/members/{userId}` (Xóa technician khỏi nhóm).