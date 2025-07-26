Chắc chắn rồi! Dưới đây là tệp `TODO.md` chi tiết để hướng dẫn bạn từng bước tái cấu trúc (refactor) dự án `TechnicalSupportApi` theo các yêu cầu đã nêu. Tệp này bao gồm tất cả các thay đổi về cấu trúc thư mục và mã nguồn cần thiết, bạn chỉ cần sao chép và dán.

````markdown
<!--_## TODO.md ##_-->

# Hướng dẫn Refactor Project TechnicalSupportApi

Tài liệu này sẽ hướng dẫn bạn từng bước tái cấu trúc lại dự án `TechnicalSupportApi` để áp dụng các phương pháp và kiến trúc hiện đại, bao gồm:

-   **Kiến trúc phân lớp theo Domain-Driven Design (DDD)**: Tách project thành các lớp `Domain`, `Application`, `Infrastructure`, và `Presentation (API)`.
-   **FluentValidation**: Sử dụng thư viện FluentValidation để kiểm tra (validate) dữ liệu đầu vào một cách chặt chẽ và có tổ chức.
-   **DTOs (Data Transfer Objects)**: Sử dụng DTO để định hình dữ liệu trả về cho client, tránh lộ cấu trúc của các Entities.
-   **Pagination**: Triển khai phân trang cho các danh sách dữ liệu.
-   **Standardized API Response**: Chuẩn hóa cấu trúc của tất cả các phản hồi từ API để client dễ dàng xử lý.

---

### Cấu trúc thư mục mới của Solution

Sau khi hoàn tất, cấu trúc solution của bạn sẽ trông như sau:

```
TechnicalSupport/
├── TechnicalSupport.sln
├── src/
│   ├── Core/
│   │   ├── TechnicalSupport.Application/
│   │   ├── TechnicalSupport.Domain/
│   │
│   ├── Infrastructure/
│   │   ├── TechnicalSupport.Infrastructure/
│   │
│   └── Presentation/
│       └── TechnicalSupportApi/
```

---

## Bước 0: Chuẩn bị và Cài đặt

### 1. Tạo cấu trúc Project mới

1.  Tạo một thư mục gốc mới cho solution, ví dụ `TechnicalSupport`.
2.  Mở terminal hoặc command prompt trong thư mục này.
3.  Tạo solution mới: `dotnet new sln --name TechnicalSupport`
4.  Tạo các project mới theo cấu trúc DDD:
    ```bash
    # Tạo các thư mục
    mkdir -p src/Core src/Infrastructure src/Presentation

    # Tạo project Domain (Class Library)
    dotnet new classlib -o src/Core/TechnicalSupport.Domain -f net9.0
    
    # Tạo project Application (Class Library)
    dotnet new classlib -o src/Core/TechnicalSupport.Application -f net9.0
    
    # Tạo project Infrastructure (Class Library)
    dotnet new classlib -o src/Infrastructure/TechnicalSupport.Infrastructure -f net9.0

    # Di chuyển project API hiện tại vào thư mục Presentation
    # (Giả sử project cũ của bạn nằm trong thư mục TechnicalSupportApi)
    mv ../TechnicalSupportApi src/Presentation/
    ```
5.  Thêm các project vào solution:
    ```bash
    dotnet sln add src/Core/TechnicalSupport.Domain/TechnicalSupport.Domain.csproj
    dotnet sln add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj
    dotnet sln add src/Infrastructure/TechnicalSupport.Infrastructure/TechnicalSupport.Infrastructure.csproj
    dotnet sln add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj
    ```

### 2. Thiết lập Project References

Thiết lập sự phụ thuộc giữa các project:
```bash
# Application phụ thuộc vào Domain
dotnet add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj reference src/Core/TechnicalSupport.Domain/TechnicalSupport.Domain.csproj

# Infrastructure phụ thuộc vào Application
dotnet add src/Infrastructure/TechnicalSupport.Infrastructure/TechnicalSupport.Infrastructure.csproj reference src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj

# API (Presentation) phụ thuộc vào Infrastructure và Application
dotnet add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj reference src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj
dotnet add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj reference src/Infrastructure/TechnicalSupport.Infrastructure/TechnicalSupport.Infrastructure.csproj
```

### 3. Cài đặt các NuGet Packages cần thiết

Chạy các lệnh sau để cài đặt các gói cần thiết cho từng project:

**TechnicalSupport.Domain:**
```bash
dotnet add src/Core/TechnicalSupport.Domain/TechnicalSupport.Domain.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

**TechnicalSupport.Application:**
```bash
dotnet add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj package MediatR
dotnet add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj package FluentValidation
dotnet add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add src/Core/TechnicalSupport.Application/TechnicalSupport.Application.csproj package Microsoft.EntityFrameworkCore
```

**TechnicalSupport.Infrastructure:**
```bash
dotnet add src/Infrastructure/TechnicalSupport.Infrastructure/TechnicalSupport.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Infrastructure/TechnicalSupport.Infrastructure/TechnicalSupport.Infrastructure.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
```

**TechnicalSupportApi:**
(Project này đã có một số gói, chúng ta sẽ thêm và đảm bảo các gói cần thiết có mặt)
```bash
dotnet add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj package FluentValidation.AspNetCore
dotnet add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj package Swashbuckle.AspNetCore # (Đã có, chỉ để chắc chắn)
dotnet add src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj package Microsoft.AspNetCore.OpenApi # (Đã có, chỉ để chắc chắn)
```
---

## Bước 1: Tái cấu trúc Lớp Domain

Lớp Domain chứa các entities, value objects, và các business rules cốt lõi.

### 1. Di chuyển và cập nhật các Entities

Di chuyển các file model từ `TechnicalSupportApi/Models` vào `src/Core/TechnicalSupport.Domain/Entities`. Đồng thời đổi namespace của chúng thành `TechnicalSupport.Domain.Entities`.

<!-- File: src/Core/TechnicalSupport.Domain/Entities/ApplicationUser.cs -->
```csharp
using Microsoft.AspNetCore.Identity;

namespace TechnicalSupport.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; }
    public string? Expertise { get; set; } // For technicians (e.g., "Hardware", "Software")
}
```

<!-- File: src/Core/TechnicalSupport.Domain/Entities/Ticket.cs -->
```csharp
using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities;

public class Ticket
{
    public int TicketId { get; private set; }

    [Required, StringLength(255)]
    public string Title { get; private set; }

    [Required]
    public string Description { get; private set; }

    [Required]
    public string CustomerId { get; private set; }
    public ApplicationUser Customer { get; private set; }

    public string? AssigneeId { get; private set; }
    public ApplicationUser? Assignee { get; private set; }

    public int? GroupId { get; private set; }
    public Group? Group { get; private set; }

    [Required]
    public int StatusId { get; private set; }
    public Status Status { get; private set; }

    [StringLength(20)]
    public string Priority { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Private constructor for EF Core
    private Ticket() { }

    public Ticket(string title, string description, string customerId, int statusId, string priority)
    {
        Title = title;
        Description = description;
        CustomerId = customerId;
        StatusId = statusId;
        Priority = priority ?? "Medium";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(int newStatusId)
    {
        StatusId = newStatusId;
        UpdatedAt = DateTime.UtcNow;
        if (newStatusId == 3) // Giả sử StatusId 3 là 'Closed'
        {
            ClosedAt = DateTime.UtcNow;
        }
    }

    public void AssignTo(string? assigneeId, int? groupId)
    {
        AssigneeId = assigneeId;
        GroupId = groupId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

<!-- File: src/Core/TechnicalSupport.Domain/Entities/Comment.cs -->
```csharp
using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities;

public class Comment
{
    public int CommentId { get; set; }

    [Required]
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; }

    [Required]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    [Required]
    public string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

<!-- File: src/Core/TechnicalSupport.Domain/Entities/Status.cs -->
```csharp
using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities;

public class Status
{
    public int StatusId { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; }
}
```

(Thực hiện tương tự cho các file `Attachment.cs`, `Group.cs`, `TechnicianGroup.cs`, chỉ cần đổi namespace)

### 2. Định nghĩa Interfaces cho Repositories và UnitOfWork

Tạo một thư mục `src/Core/TechnicalSupport.Domain/Abstractions`.

<!-- File: src/Core/TechnicalSupport.Domain/Abstractions/IUnitOfWork.cs -->
```csharp
namespace TechnicalSupport.Domain.Abstractions;

public interface IUnitOfWork : IDisposable
{
    ITicketRepository TicketRepository { get; }
    ICommentRepository CommentRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

<!-- File: src/Core/TechnicalSupport.Domain/Abstractions/ITicketRepository.cs -->
```csharp
using System.Linq.Expressions;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Domain.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default);
    IQueryable<Ticket> GetWithInclude(params Expression<Func<Ticket, object>>[] includeProperties);
    void Add(Ticket ticket);
    void Update(Ticket ticket);
    void Remove(Ticket ticket);
}
```

<!-- File: src/Core/TechnicalSupport.Domain/Abstractions/ICommentRepository.cs -->
```csharp
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Domain.Abstractions;

public interface ICommentRepository
{
    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
}
```
---

## Bước 2: Tái cấu trúc Lớp Application

Lớp này chứa logic nghiệp vụ, điều phối các domain entities và thực hiện các use case của ứng dụng.

### 1. Tạo các đối tượng chung

Tạo thư mục `src/Core/TechnicalSupport.Application/Common`.

<!-- File: src/Core/TechnicalSupport.Application/Common/ApiResponse.cs -->
```csharp
namespace TechnicalSupport.Application.Common;

public class ApiResponse<T>
{
    public bool Succeeded { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public object Errors { get; set; }
    public PaginationMetadata Pagination { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Request successful.", PaginationMetadata pagination = null)
    {
        return new ApiResponse<T> { Succeeded = true, Data = data, Message = message, Pagination = pagination };
    }

    public static ApiResponse<T> Fail(string message, object errors = null)
    {
        return new ApiResponse<T> { Succeeded = false, Message = message, Errors = errors };
    }
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
```

<!-- File: src/Core/TechnicalSupport.Application/Common/PagedList.cs -->
```csharp
using Microsoft.EntityFrameworkCore;

namespace TechnicalSupport.Application.Common;

public class PagedList<T> : List<T>
{
    public PaginationMetadata Metadata { get; }

    public PagedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        Metadata = new PaginationMetadata
        {
            TotalCount = count,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
        AddRange(items);
    }

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
```

### 2. Định nghĩa DTOs

Tạo thư mục `src/Core/TechnicalSupport.Application/DTOs`.

<!-- File: src/Core/TechnicalSupport.Application/DTOs/TicketDto.cs -->
```csharp
namespace TechnicalSupport.Application.DTOs;

public class TicketDto
{
    public int TicketId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

<!-- File: src/Core/TechnicalSupport.Application/DTOs/CommentDto.cs -->
```csharp
namespace TechnicalSupport.Application.DTOs;

public class CommentDto
{
    public int CommentId { get; set; }
    public int TicketId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

<!-- File: src/Core/TechnicalSupport.Application/DTOs/LoginResponseDto.cs -->
```csharp
namespace TechnicalSupport.Application.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; }
}
```

### 3. Triển khai CQRS với MediatR và FluentValidation

Tạo thư mục `src/Core/TechnicalSupport.Application/Features`. Chúng ta sẽ làm ví dụ cho `Tickets` và `Users`.

**Ticket Features:**

Tạo thư mục `src/Core/TechnicalSupport.Application/Features/Tickets/Commands/CreateTicket`.

<!-- File: src/Core/TechnicalSupport.Application/Features/Tickets/Commands/CreateTicket/CreateTicketCommand.cs -->
```csharp
using MediatR;
using TechnicalSupport.Application.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommand : IRequest<TicketDto>
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int StatusId { get; set; }
    public string? Priority { get; set; }
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Tickets/Commands/CreateTicket/CreateTicketCommandHandler.cs -->
```csharp
using MediatR;
using TechnicalSupport.Application.DTOs;
using TechnicalSupport.Domain.Abstractions;
using TechnicalSupport.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TechnicalSupport.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateTicketCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var ticket = new Ticket(
            request.Title,
            request.Description,
            userId,
            request.StatusId,
            request.Priority
        );

        _unitOfWork.TicketRepository.Add(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Đây là ví dụ đơn giản, trong thực tế bạn có thể cần query lại để lấy CustomerName,...
        return new TicketDto
        {
            TicketId = ticket.TicketId,
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerId = ticket.CustomerId,
            Status = ticket.Status?.Name, // Cần query lại để có thông tin này
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Tickets/Commands/CreateTicket/CreateTicketCommandValidator.cs -->
```csharp
using FluentValidation;

namespace TechnicalSupport.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.StatusId)
            .GreaterThan(0).WithMessage("StatusId must be valid.");
    }
}
```

Tạo thư mục `src/Core/TechnicalSupport.Application/Features/Tickets/Queries/GetTicketList`.

<!-- File: src/Core/TechnicalSupport.Application/Features/Tickets/Queries/GetTicketList/GetTicketListQuery.cs -->
```csharp
using MediatR;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Queries.GetTicketList;

public class GetTicketListQuery : IRequest<ApiResponse<PagedList<TicketDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Tickets/Queries/GetTicketList/GetTicketListQueryHandler.cs -->
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.DTOs;
using TechnicalSupport.Domain.Abstractions;

namespace TechnicalSupport.Application.Features.Tickets.Queries.GetTicketList;

public class GetTicketListQueryHandler : IRequestHandler<GetTicketListQuery, ApiResponse<PagedList<TicketDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTicketListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PagedList<TicketDto>>> Handle(GetTicketListQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.TicketRepository.GetWithInclude(t => t.Customer, t => t.Assignee, t => t.Status);

        var ticketDtos = query.Select(t => new TicketDto
        {
            TicketId = t.TicketId,
            Title = t.Title,
            Description = t.Description,
            CustomerId = t.CustomerId,
            CustomerName = t.Customer.DisplayName,
            AssigneeId = t.AssigneeId,
            AssigneeName = t.Assignee.DisplayName,
            Status = t.Status.Name,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });

        var pagedList = await PagedList<TicketDto>.CreateAsync(ticketDtos, request.PageNumber, request.PageSize);
        
        return ApiResponse<PagedList<TicketDto>>.Success(pagedList, "Tickets retrieved successfully.", pagedList.Metadata);
    }
}
```

**User Features (Auth):**
Tương tự, tạo các command cho `Register` và `Login`.

<!-- File: src/Core/TechnicalSupport.Application/Features/Users/Commands/RegisterUser/RegisterUserCommand.cs -->
```csharp
using MediatR;

namespace TechnicalSupport.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<bool>
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string DisplayName { get; set; }
    public string? Expertise { get; set; }
    public string? Role { get; set; }
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Users/Commands/RegisterUser/RegisterUserCommandHandler.cs -->
```csharp
using MediatR;
using Microsoft.AspNetCore.Identity;
using TechnicalSupport.Domain.Entities;
using System.Linq;

namespace TechnicalSupport.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Expertise = request.Expertise
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            throw new Exception(string.Join("\n", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, request.Role ?? "Client");
        return true;
    }
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Users/Commands/RegisterUser/RegisterUserCommandValidator.cs -->
```csharp
using FluentValidation;

namespace TechnicalSupport.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.DisplayName).NotEmpty();
        RuleFor(x => x.Role).Must(role => role == "Client" || role == "Technician" || role == "Manager" || role == null)
            .WithMessage("Role must be 'Client', 'Technician', or 'Manager'.");
    }
}
```

---
## Bước 3: Tái cấu trúc Lớp Infrastructure

Lớp này chứa các triển khai cụ thể của các abstraction, như database access.

### 1. Di chuyển và cập nhật DbContext

1.  Di chuyển file `ApplicationDbContext.cs` và thư mục `Migrations` từ `TechnicalSupportApi/Data` sang `src/Infrastructure/TechnicalSupport.Infrastructure/Persistence`.
2.  Cập nhật namespace.

<!-- File: src/Infrastructure/TechnicalSupport.Infrastructure/Persistence/ApplicationDbContext.cs -->
```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<TechnicianGroup> TechnicianGroups { get; set; }
    public DbSet<Status> Statuses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Ticket relationships
        builder.Entity<Ticket>()
            .HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Ticket>()
            .HasOne(t => t.Group)
            .WithMany()
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Ticket>()
            .HasOne(t => t.Status)
            .WithMany()
            .HasForeignKey(t => t.StatusId);

        // Configure Comment relationships
        builder.Entity<Comment>()
            .HasOne(c => c.Ticket)
            .WithMany()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Attachment relationships
        builder.Entity<Attachment>()
            .HasOne(a => a.Ticket)
            .WithMany()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Restrict); 

        builder.Entity<Attachment>()
            .HasOne(a => a.Comment)
            .WithMany()
            .HasForeignKey(a => a.CommentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Attachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure TechnicianGroup composite key
        builder.Entity<TechnicianGroup>()
            .HasKey(tg => new { tg.UserId, tg.GroupId });

        builder.Entity<TechnicianGroup>()
            .HasOne(tg => tg.User)
            .WithMany()
            .HasForeignKey(tg => tg.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TechnicianGroup>()
            .HasOne(tg => tg.Group)
            .WithMany()
            .HasForeignKey(tg => tg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure unique constraints
        builder.Entity<Group>()
            .HasIndex(g => g.Name)
            .IsUnique();

        builder.Entity<Status>()
            .HasIndex(s => s.Name)
            .IsUnique();
        
        builder.Entity<Ticket>()
            .Property(t => t.Priority)
            .HasConversion<string>()
            .HasDefaultValue("Medium")
            .IsRequired();
    }
}
```

### 2. Triển khai Repositories và UnitOfWork

Tạo thư mục `src/Infrastructure/TechnicalSupport.Infrastructure/Persistence/Repositories`.

<!-- File: src/Infrastructure/TechnicalSupport.Infrastructure/Persistence/Repositories/TicketRepository.cs -->
```csharp
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TechnicalSupport.Domain.Abstractions;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.FindAsync(new object[] { id }, cancellationToken);
    }

    public IQueryable<Ticket> GetWithInclude(params Expression<Func<Ticket, object>>[] includeProperties)
    {
        IQueryable<Ticket> query = _context.Tickets.AsQueryable();
        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }
        return query;
    }
    
    public void Remove(Ticket ticket)
    {
        _context.Tickets.Remove(ticket);
    }

    public void Update(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
    }
}
```
(Tạo `CommentRepository.cs` tương tự)

<!-- File: src/Infrastructure/TechnicalSupport.Infrastructure/Persistence/UnitOfWork.cs -->
```csharp
using TechnicalSupport.Domain.Abstractions;
using TechnicalSupport.Infrastructure.Persistence.Repositories;

namespace TechnicalSupport.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private ITicketRepository _ticketRepository;
    private ICommentRepository _commentRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ITicketRepository TicketRepository => _ticketRepository ??= new TicketRepository(_context);
    public ICommentRepository CommentRepository => _commentRepository ??= new CommentRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

---

## Bước 4: Tái cấu trúc Lớp Presentation (API)

Lớp này chỉ chịu trách nhiệm về giao tiếp HTTP, nhận request và trả về response.

### 1. Xóa các file cũ

-   Xóa toàn bộ thư mục `TechnicalSupportApi/Models`.
-   Xóa toàn bộ thư mục `TechnicalSupportApi/Data`.

### 2. Tạo Base Controller và Middleware

<!-- File: src/Presentation/TechnicalSupportApi/Controllers/ApiBaseController.cs -->
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalSupportApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    private ISender _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetService<ISender>();
}
```

<!-- File: src/Presentation/TechnicalSupportApi/Middleware/GlobalExceptionHandlerMiddleware.cs -->
```csharp
using FluentValidation;
using System.Net;
using System.Text.Json;
using TechnicalSupport.Application.Common;

namespace TechnicalSupportApi.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode status;
        var stackTrace = string.Empty;
        string message;
        object errors = null;

        var exceptionType = exception.GetType();

        if (exceptionType == typeof(ValidationException))
        {
            message = "Validation Error";
            status = HttpStatusCode.BadRequest;
            errors = ((ValidationException)exception).Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
        }
        else if (exceptionType == typeof(UnauthorizedAccessException))
        {
            status = HttpStatusCode.Unauthorized;
            message = exception.Message;
        }
        else
        {
            status = HttpStatusCode.InternalServerError;
            message = "An unexpected error occurred.";
            // Ghi log lỗi chi tiết ở đây
            // stackTrace = exception.StackTrace; // Chỉ bật ở môi trường dev
        }

        var response = ApiResponse<string>.Fail(message, errors);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 3. Cập nhật `TechnicalSupportApi.csproj`
Đảm bảo file `.csproj` của API có các tham chiếu project đúng.
<!-- File: src/Presentation/TechnicalSupportApi/TechnicalSupportApi.csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.7" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.2.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.7">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\TechnicalSupport.Application\TechnicalSupport.Application.csproj" />
    <ProjectReference Include="..\..\Infrastructure\TechnicalSupport.Infrastructure\TechnicalSupport.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Middleware\" />
  </ItemGroup>

</Project>
```

### 4. Cập nhật `Program.cs`

Đây là bước quan trọng để kết nối tất cả các lớp lại với nhau.

<!-- File: src/Presentation/TechnicalSupportApi/Program.cs -->
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using TechnicalSupport.Application.Abstractions;
using TechnicalSupport.Application.Features.Tickets.Commands.CreateTicket;
using TechnicalSupport.Domain.Abstractions;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Services;
using TechnicalSupportApi;
using TechnicalSupportApi.Middleware;
using FluentValidation.AspNetCore;
using TechnicalSupport.Application.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// --- Bổ sung cấu hình cho các lớp ---
// 1. Cấu hình Application Layer
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTicketCommand).Assembly));
builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssembly(typeof(CreateTicketCommandValidator).Assembly));


// 2. Cấu hình Infrastructure Layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();


// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        // Chỉ định Assembly chứa Migrations
        b => b.MigrationsAssembly("TechnicalSupport.Infrastructure")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
    
// --- Giữ nguyên cấu hình cũ ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Add SignalR
builder.Services.AddSignalR();
// Add HttpContextAccessor để lấy thông tin user trong Application Layer
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Thêm Middleware xử lý lỗi toàn cục
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.Run();
```
*Lưu ý: Chúng ta đã tạo `ITokenService` và `TokenService` để tách logic tạo token ra.* Tạo file sau:
<!-- File: src/Infrastructure/TechnicalSupport.Infrastructure/Services/TokenService.cs -->
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechnicalSupport.Application.Abstractions;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("displayName", user.DisplayName ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```
Và interface của nó:
<!-- File: src/Core/TechnicalSupport.Application/Abstractions/ITokenService.cs -->
```csharp
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Application.Abstractions;

public interface ITokenService
{
    string GenerateJwtToken(ApplicationUser user, IList<string> roles);
}
```

### 5. Cập nhật Controllers

Controllers bây giờ sẽ rất gọn nhẹ.

<!-- File: src/Presentation/TechnicalSupportApi/Controllers/TicketsController.cs -->
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Application.Features.Tickets.Commands.CreateTicket;
using TechnicalSupport.Application.Features.Tickets.Queries.GetTicketList;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.DTOs;

namespace TechnicalSupportApi.Controllers;

[Authorize]
public class TicketsController : ApiBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] GetTicketListQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetTicketById), new { id = result.TicketId }, result);
    }
    
    // Thêm các endpoint khác tương tự
    // Ví dụ GetTicketById
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketById(int id)
    {
        // Bạn cần tạo GetTicketByIdQuery và Handler tương ứng
        // var query = new GetTicketByIdQuery { Id = id };
        // var result = await Mediator.Send(query);
        // return Ok(result);
        return Ok($"Endpoint for getting ticket {id} - to be implemented");
    }
}
```

<!-- File: src/Presentation/TechnicalSupportApi/Controllers/AuthController.cs -->
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Application.Abstractions;
using TechnicalSupport.Application.Features.Users.Commands.LoginUser;
using TechnicalSupport.Application.Features.Users.Commands.RegisterUser;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupportApi.Controllers;

public class AuthController : ApiBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { Message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
            return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, command.Password, false);
        if (!result.Succeeded)
            return Unauthorized("Invalid credentials");
            
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateJwtToken(user, roles);

        return Ok(new { Token = token });
    }
}
```
*Lưu ý: Chúng ta cần tạo `LoginUserCommand` và validator của nó.*
<!-- File: src/Core/TechnicalSupport.Application/Features/Users/Commands/LoginUser/LoginUserCommand.cs -->
```csharp
using MediatR;
using TechnicalSupport.Application.DTOs;

namespace TechnicalSupport.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommand : IRequest<LoginResponseDto>
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

<!-- File: src/Core/TechnicalSupport.Application/Features/Users/Commands/LoginUser/LoginUserCommandValidator.cs -->
```csharp
using FluentValidation;

namespace TechnicalSupport.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```
(Handler cho `LoginUserCommand` đã được tích hợp trực tiếp vào controller `AuthController` để sử dụng `SignInManager` và `UserManager`, đây là một cách tiếp cận phổ biến để xử lý login).

---

## Hoàn tất

Sau khi hoàn thành các bước trên, bạn đã tái cấu trúc thành công dự án của mình.
-   **Clean Architecture**: Logic được phân tách rõ ràng giữa các lớp.
-   **CQRS/MediatR**: Các use case được đóng gói gọn gàng.
-   **FluentValidation**: Việc xác thực dữ liệu được thực hiện ở lớp Application, gần với DTOs/Commands.
-   **DTOs & Standardized Response**: API trả về dữ liệu nhất quán và an toàn.

Bây giờ bạn có thể xóa các file `*.cs` cũ trong controller và chạy lại project. Đừng quên chạy lại `dotnet ef migrations add <NewMigrationName>` và `dotnet ef database update` nếu có thay đổi về model.
````