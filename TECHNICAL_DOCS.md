# Tài liệu Kỹ thuật - TechnicalSupport API

Tài liệu này tóm tắt các quy tắc kiến trúc và quy ước cốt lõi của dự án TechnicalSupport API. Mục tiêu là giúp các nhà phát triển và AI nhanh chóng nắm bắt và đóng góp vào dự án một cách nhất quán.

## 1. Kiến trúc Tổng quan (Clean Architecture)

Dự án tuân thủ theo kiến trúc **Clean Architecture** được chia thành 4 lớp chính. Quy tắc phụ thuộc rất nghiêm ngặt: các lớp bên ngoài chỉ có thể phụ thuộc vào các lớp bên trong.

```
+-----------------------------------+
|   TechnicalSupport.Api (API)      |
| (Controllers, Middleware, DI)     |
+-----------------------------------+
              |
              v
+-----------------------------------+
| TechnicalSupport.Infrastructure   |
| (EF Core, Services, SignalR)      |
+-----------------------------------+
              |
              v
+-----------------------------------+
|  TechnicalSupport.Application     |
| (DTOs, Interfaces, Validators)    |
+-----------------------------------+
              |
              v
+-----------------------------------+
|    TechnicalSupport.Domain        |
|      (Entities)                   |
+-----------------------------------+
```

-   **Domain**: Chứa các thực thể (Entities) cốt lõi của nghiệp vụ (`Ticket`, `ApplicationUser`, `Comment`, `Status`). Lớp này không phụ thuộc vào bất kỳ lớp nào khác.
-   **Application**: Chứa logic ứng dụng, các trường hợp sử dụng (use cases). Định nghĩa các giao diện (interfaces) mà tầng Infrastructure sẽ triển khai. Nó cũng chứa DTOs, các trình xác thực (Validators) và cấu hình AutoMapper.
-   **Infrastructure**: Triển khai các giao diện được định nghĩa trong tầng Application. Chứa các chi tiết kỹ thuật như truy cập cơ sở dữ liệu (Entity Framework Core), dịch vụ gửi email, giao tiếp thời gian thực (SignalR), v.v.
-   **Api**: Là lớp trình bày (Presentation Layer), ở đây là một ASP.NET Core Web API. Nó chứa các Controllers, Middleware, cấu hình Dependency Injection (DI) và các thiết lập cho ứng dụng web.

## 2. Quy tắc Luồng Dữ liệu và Đối tượng

Quy tắc cơ bản là **tách biệt rõ ràng giữa các mối quan tâm**.

1.  **Request Lifecycle**:
    -   Một HTTP Request đi vào **Controller** trong tầng `Api`.
    -   Controller **không chứa logic nghiệp vụ**. Nó gọi một phương thức trên một **interface** từ tầng `Application` (ví dụ: `ITicketService`).
    -   DI Container sẽ cung cấp một **implementation** của interface đó từ tầng `Infrastructure` (ví dụ: `TicketService`).
    -   Service trong `Infrastructure` sẽ tương tác với cơ sở dữ liệu thông qua `DbContext` và thực hiện các logic cần thiết.
    -   Dữ liệu được trả về theo chuỗi ngược lại.

2.  **DTOs vs. Entities**:
    -   **KHÔNG BAO GIỜ** trả về trực tiếp các đối tượng **Domain Entity** (`Ticket`, `ApplicationUser`,...) từ API.
    -   Luôn sử dụng **Data Transfer Objects (DTOs)** được định nghĩa trong tầng `Application` (ví dụ: `TicketDto`, `CommentDto`).
    -   **Input Models** (ví dụ: `CreateTicketModel`, `LoginModel`) được sử dụng để nhận dữ liệu từ client.
    -   **AutoMapper** (`Application/Mappings/MappingProfile.cs`) được sử dụng để chuyển đổi giữa Entities và DTOs.

    *Ví dụ về Mapping:*
    ```csharp
    // TechnicalSupport.Application/Mappings/MappingProfile.cs
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Maps the Ticket entity to the TicketDto
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Assignee, opt => opt.MapFrom(src => src.Assignee));
            
            // Maps the input model to the Ticket entity
            CreateMap<CreateTicketModel, Ticket>();
        }
    }
    ```

## 3. Các quy tắc trong từng tầng

### Tầng Domain
-   Chỉ chứa các lớp POCO (Plain Old CLR Object) đại diện cho các thực thể nghiệp vụ.
-   Các thuộc tính có thể được trang trí bằng các DataAnnotations cơ bản (`[Required]`, `[StringLength]`) nhưng không chứa logic phức tạp.
-   Các quan hệ giữa các thực thể được định nghĩa trong `ApplicationDbContext` tại tầng `Infrastructure`.

### Tầng Application
-   **Tổ chức theo Feature**: Các DTOs, Validators, và logic liên quan được nhóm lại trong các thư mục theo từng tính năng (ví dụ `Features/Tickets`, `Features/Authentication`).
-   **Validation**:
    -   Sử dụng **FluentValidation** cho tất cả các dữ liệu đầu vào từ client.
    -   Với mỗi DTO đầu vào (ví dụ: `CreateTicketModel`), phải có một lớp Validator tương ứng (ví dụ: `CreateTicketModelValidator`).
    -   Các Validator được tự động đăng ký và thực thi bởi `ValidationFilter` trong tầng `Api`.

    *Ví dụ Validator:*
    ```csharp
    // TechnicalSupport.Application/Features/Tickets/Validators/CreateTicketModelValidator.cs
    public class CreateTicketModelValidator : AbstractValidator<CreateTicketModel>
    {
        public CreateTicketModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(255).WithMessage("Title cannot be longer than 255 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");
        }
    }
    ```
-   **Interfaces**: Định nghĩa các "hợp đồng" cho các service (ví dụ `ITicketService`). Các Controller sẽ phụ thuộc vào các interface này.

### Tầng Infrastructure
-   **Truy cập dữ liệu**:
    -   Sử dụng **Entity Framework Core**. `ApplicationDbContext` là lớp duy nhất tương tác trực tiếp với DB.
    -   Tất cả các truy vấn cơ sở dữ liệu phải là bất đồng bộ (sử dụng `async`/`await`).
    -   Sử dụng các phương thức của LINQ để truy vấn (`Where`, `Include`, `Select`,...).
-   **Service Implementations**: Triển khai các interface từ tầng `Application`. Đây là nơi chứa logic nghiệp vụ thực sự.
-   **Seeding Data**: `DataSeeder.cs` chịu trách nhiệm khởi tạo dữ liệu mẫu (vai trò, người dùng, trạng thái, tickets) cho môi trường phát triển.

### Tầng API
-   **Controllers Phải "Mỏng" (Thin Controllers)**: Controller chỉ đóng vai trò điều phối, nhận request, gọi service tương ứng và trả về response.
-   **Chuẩn Phản hồi API (API Response Standard)**:
    -   Tất cả các endpoint **BẮT BUỘC** phải trả về một đối tượng `ApiResponse<T>` được chuẩn hóa. Điều này đảm bảo tính nhất quán cho client.
    -   Sử dụng `ApiResponse.Success(...)` cho các kết quả thành công và `ApiResponse.Fail(...)` cho các lỗi đã được dự đoán.

    *Ví dụ Response Thành công (200 OK):*
    ```json
    {
      "succeeded": true,
      "message": "Ticket created successfully.",
      "data": {
        "ticketId": 1,
        "title": "New Ticket",
        // ... other properties
      },
      "errors": null
    }
    ```
    *Ví dụ Response Lỗi (400 Bad Request):*
    ```json
    {
      "succeeded": false,
      "message": "Validation failed.",
      "data": null,
      "errors": [
        "Title is required."
      ]
    }
    ```
-   **Xử lý Lỗi Tập trung (Centralized Error Handling)**:
    -   `ExceptionHandlerMiddleware` được sử dụng để bắt tất cả các exception chưa được xử lý.
    -   Nó sẽ ghi log lỗi và trả về một response lỗi theo chuẩn `ApiResponse`. Điều này ngăn chặn việc rò rỉ chi tiết của exception ra ngoài client.
-   **Validation Tự động**:
    -   `ValidationFilter` được đăng ký toàn cục. Nó sẽ tự động chạy `FluentValidation` cho bất kỳ model nào được gửi trong body của request.
    -   Nếu model không hợp lệ, filter sẽ ngắt chuỗi xử lý và trả về lỗi 400 Bad Request với định dạng `ApiResponse` chuẩn. Controller không cần phải kiểm tra `ModelState.IsValid`.
-   **Xác thực & Ủy quyền (Auth)**:
    -   Sử dụng **JWT Bearer Token** cho việc xác thực.
    -   `AuthController` xử lý việc đăng ký và đăng nhập.
    -   Các endpoint yêu cầu xác thực được đánh dấu bằng `[Authorize]`.
    -   Vai trò (`Client`, `Technician`) được sử dụng để kiểm soát quyền truy cập.

## 4. Cấu hình và Khởi chạy

-   **Dependency Injection (DI)**: Tất cả các services, repositories, và các thành phần khác được đăng ký trong file `Program.cs`. Dự án sử dụng `AddScoped` cho các service liên quan đến request.
-   **Configuration**:
    -   Cấu hình được lưu trong `appsettings.json` và các file `appsettings.{Environment}.json`.
    -   Các cấu hình quan trọng như `JwtSettings` được bind vào các đối tượng strongly-typed để dễ dàng sử dụng và tránh lỗi chính tả.
-   **Database Migration và Seeding**:
    -   Trong môi trường `Development`, ứng dụng sẽ tự động áp dụng các migrations và seed dữ liệu khi khởi chạy (`context.Database.MigrateAsync()` và `DataSeeder.SeedAsync()`).
    -   Để tạo một migration mới, sử dụng lệnh:
        ```bash
        dotnet ef migrations add <MigrationName> --project TechnicalSupport.Infrastructure --startup-project TechnicalSupport.Api
        ```

## 5. Tóm tắt các Quy tắc Vàng

1.  **Tuân thủ Clean Architecture**: Luôn tôn trọng quy tắc phụ thuộc.
2.  **Controller phải mỏng**: Không chứa logic nghiệp vụ.
3.  **Sử dụng DTOs, không dùng Entities**: Không bao giờ lộ Entity ra API.
4.  **Dùng Interface ở Application, Implementation ở Infrastructure**: Tuân thủ Dependency Inversion.
5.  **Dùng FluentValidation cho mọi Input**: Đảm bảo dữ liệu đầu vào luôn hợp lệ.
6.  **Luôn trả về `ApiResponse<T>`**: Giữ cho API nhất quán.
7.  **Sử dụng `async/await` cho I/O**: Đảm bảo hiệu năng và khả năng mở rộng.
```