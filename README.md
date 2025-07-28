# TechnicalSupport API

## Giới thiệu

Đây là backend API cho một hệ thống Hỗ trợ Kỹ thuật (Technical Support). Dự án được xây dựng theo kiến trúc Clean Architecture, sử dụng .NET 9, Entity Framework Core và SQL Server. Hệ thống cho phép người dùng (Client) tạo các yêu cầu hỗ trợ (ticket), và các kỹ thuật viên (Technician) tiếp nhận và xử lý các yêu cầu đó.

## Công nghệ sử dụng

-   **.NET 9**: Nền tảng phát triển chính.
-   **ASP.NET Core Web API**: Xây dựng các endpoint RESTful.
-   **Entity Framework Core 9**: Làm việc với cơ sở dữ liệu.
-   **SQL Server**: Hệ quản trị cơ sở dữ liệu quan hệ.
-   **ASP.NET Core Identity & JWT**: Cho việc xác thực và ủy quyền người dùng.
-   **AutoMapper**: Ánh xạ đối tượng giữa các lớp.
-   **FluentValidation**: Xác thực dữ liệu đầu vào.
-   **SignalR**: Giao tiếp thời gian thực (real-time).
-   **Swagger (Swashbuckle)**: Tài liệu hóa và kiểm thử API, với tính năng tự động điền dữ liệu mẫu.

## Yêu cầu cài đặt

Trước khi bắt đầu, hãy đảm bảo bạn đã cài đặt các công cụ sau trên máy tính của mình:

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Git](https://git-scm.com/downloads)
-   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (ví dụ: phiên bản Express hoặc Developer).
-   Một trình soạn thảo code như [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [Visual Studio Code](https://code.visualstudio.com/).

## Hướng dẫn Cài đặt và Chạy Project

Thực hiện theo các bước sau để thiết lập và chạy project trên máy của bạn.

### Bước 1: Clone Repository

Mở terminal (Command Prompt, PowerShell, hoặc Git Bash) và chạy lệnh sau để tải project về máy:

```bash
git clone https://github.com/Airbone2107/TechnicalSupport.git
cd TechnicalSupport
```
*(Lưu ý: Thay `https://github.com/your-username/TechnicalSupport.git` bằng URL thực tế của repository của bạn).*

### Bước 2: Cấu hình Chuỗi kết nối và Chế độ Mock

Các cấu hình chính cho môi trường phát triển nằm trong file `TechnicalSupport.Api/appsettings.Development.json`.

#### 1. Chế độ Mock để Kiểm thử (Mock Mode for Testing)

Dự án này tích hợp một **"chế độ mock"** cho phép bạn kiểm thử các endpoint của Ticket mà không cần tương tác với cơ sở dữ liệu thật.

-   **Cách hoạt động**: Khi bật, các yêu cầu đến `/Tickets` sẽ được xử lý bởi một dịch vụ giả (mock service), trả về dữ liệu mẫu mà không ghi, sửa, hay xóa bất cứ thứ gì trong DB.
-   **Lưu ý**: Các endpoint xác thực (`/Auth`) **không** bị ảnh hưởng bởi chế độ này và vẫn hoạt động với database thật để bạn có thể đăng nhập và nhận token JWT hợp lệ.

Để bật/tắt chế độ này, hãy mở file `TechnicalSupport.Api/appsettings.Development.json` và thay đổi giá trị của `EnableMockMode`:

```json
{
  // ... các cấu hình khác
  "ApiSettings": {
    "EnableMockMode": true // Đặt là `true` để bật mock, `false` để dùng database thật
  }
}
```

Chế độ này rất hữu ích để nhanh chóng kiểm tra luồng API, dữ liệu trả về, hoặc khi bạn chưa muốn thiết lập database.

#### 2. Chuỗi kết nối (Connection String)

Nếu bạn tắt chế độ mock (`"EnableMockMode": false`), bạn cần cấu hình chuỗi kết nối đến SQL Server.

1.  Tìm đến phần `ConnectionStrings` trong cùng file `appsettings.Development.json`.
2.  Chỉnh sửa giá trị `DefaultConnection` để trỏ đến instance SQL Server của bạn.

**Ví dụ:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=TechnicalSupportDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Bước 3: Cập nhật Database (Khi không dùng Mock Mode)

Khi `EnableMockMode` được đặt thành `false`, dự án được cấu hình để tự động cập nhật và khởi tạo dữ liệu mẫu (seed data) cho database.

**Cách 1: Tự động (Khuyến khích cho lần chạy đầu tiên)**

1.  Đảm bảo `EnableMockMode` là `false` và bạn đã cấu hình đúng `DefaultConnection`.
2.  Chạy ứng dụng (xem **Bước 4**). Database sẽ được tự động tạo, các migration sẽ được áp dụng và dữ liệu mẫu sẽ được thêm vào.

**Cách 2: Thủ công (Sử dụng khi bạn muốn tự tạo migration)**

1.  **Cài đặt EF Core Tools:**
    ```bash
    dotnet tool install --global dotnet-ef
    ```
2.  **Tạo Migration:**
    ```bash
    dotnet ef migrations add <MigrationName> --project TechnicalSupport.Infrastructure --startup-project TechnicalSupport.Api
    ```
3.  **Cập nhật Database:**
    ```bash
    dotnet ef database update --startup-project TechnicalSupport.Api
    ```

### Bước 4: Chạy ứng dụng

Bạn có thể chạy ứng dụng bằng một trong hai cách sau:

**Cách 1: Sử dụng Visual Studio**

1.  Mở file `TechnicalSupport.sln` bằng Visual Studio.
2.  Chắc chắn rằng `TechnicalSupport.Api` được đặt làm project khởi động.
3.  Nhấn phím `F5` hoặc nút "Run".

**Cách 2: Sử dụng .NET CLI**

1.  Mở terminal tại thư mục gốc của project, di chuyển vào thư mục `TechnicalSupport.Api`.
2.  Chạy lệnh `dotnet run`.

API sẽ khởi động và lắng nghe trên các cổng được định nghĩa trong `Properties/launchSettings.json` (ví dụ: `https://localhost:7194`).

### Bước 5: Kiểm thử API với Swagger

Sau khi ứng dụng đã chạy, bạn có thể truy cập giao diện Swagger UI để kiểm thử các endpoint.

1.  Mở trình duyệt và truy cập: **`https://localhost:7194/swagger`** (thay đổi cổng nếu cần).

2.  **Tự động điền dữ liệu mẫu:**
    *   Nhờ tính năng tích hợp mới, khi bạn nhấn **"Try it out"** trên các endpoint như đăng ký, đăng nhập, hoặc tạo ticket, phần `request body` sẽ được **tự động điền với dữ liệu mẫu**. Điều này giúp bạn kiểm thử nhanh hơn mà không cần gõ lại thông tin.
    *   Bạn có thể chọn các kịch bản khác nhau từ dropdown "Examples".

3.  **Đăng nhập và Lấy Token:**
    *   Tìm đến `POST /Auth/login`, nhấn "Try it out".
    *   Ví dụ về thông tin đăng nhập sẽ được điền sẵn (tài khoản `client@example.com`).
    *   Nhấn "Execute". Phản hồi thành công sẽ chứa một JWT token.

4.  **Sao chép JWT Token:**
    *   Trong `Response body`, sao chép toàn bộ giá trị của `token`.

5.  **Ủy quyền (Authorize) trong Swagger:**
    *   Ở phía trên cùng bên phải của trang Swagger, nhấn vào nút **"Authorize"**.
    *   Trong ô `Value`, dán token theo định dạng `Bearer <your_token>`.
    *   Nhấn "Authorize", sau đó "Close".

6.  **Kiểm thử các Endpoint được bảo vệ:**
    *   Bây giờ bạn đã được xác thực. Hãy thử một endpoint cần đăng nhập, ví dụ `GET /Tickets`.
    *   Nhấn "Try it out", sau đó "Execute".
    *   Bạn sẽ thấy danh sách các ticket. Nếu đang ở **Mock Mode**, danh sách này là dữ liệu giả. Nếu không, đó là dữ liệu từ database.

### Dữ liệu mẫu (Seed Data)

Khi **không** ở chế độ mock (`"EnableMockMode": false`) và ứng dụng chạy lần đầu, các tài khoản sau sẽ được tạo tự động với mật khẩu là **`Password123!`**:

-   **Client:** `client@example.com`
-   **Technician:** `tech@example.com`

Bạn có thể dùng các tài khoản này để đăng nhập và kiểm thử các vai trò khác nhau mà không cần đăng ký.
