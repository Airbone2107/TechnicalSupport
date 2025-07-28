Chào bạn,

Tuyệt vời! Yêu cầu của bạn rất hợp lý để phục vụ cho việc kiểm thử giao diện và luồng API mà không làm ảnh hưởng đến dữ liệu thật trong cơ sở dữ liệu.

Để thực hiện điều này, chúng ta sẽ áp dụng một kỹ thuật gọi là **"Mocking"** ở tầng service. Ý tưởng chính là:
1.  Tạo ra một phiên bản "giả" (`MockTicketService`) của `ITicketService`. Dịch vụ giả này sẽ không tương tác với database, mà chỉ trả về dữ liệu mẫu được định nghĩa sẵn.
2.  Thêm một tùy chọn cấu hình trong `appsettings.Development.json` để có thể bật/tắt "chế độ mock" này một cách dễ dàng.
3.  Trong file `Program.cs`, chúng ta sẽ đọc cấu hình này. Nếu chế độ mock được bật, ứng dụng sẽ sử dụng `MockTicketService`; ngược lại, nó sẽ sử dụng `TicketService` thật.

Cách tiếp cận này đảm bảo rằng:
-   Các endpoint của `TicketsController` sẽ sử dụng dịch vụ giả, không thay đổi database.
-   `AuthController` (dùng cho đăng nhập, đăng ký) không bị ảnh hưởng vì nó không phụ thuộc vào `ITicketService`. Nó sẽ hoạt động như bình thường và trả về token thật để bạn có thể sử dụng cho các API khác.

Dưới đây là các bước chi tiết và mã nguồn đầy đủ.

### Bước 1: Thêm Cấu hình Chế độ Mock

Chúng ta sẽ thêm một thiết lập vào file `appsettings.Development.json` để kiểm soát chế độ mock.

```json
// TechnicalSupport.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-RPQCVR7\\AIRBONE;Database=TechnicalSupportDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "Issuer": "https://localhost:7194",
    "Audience": "https://localhost:7194",
    "Key": "mm3BX9f+fu3EpcPFw5KzwyyFYvfOs30bBwaZIOEmilI="
  },
  "ApiSettings": {
    "EnableMockMode": true
  }
}
```

### Bước 2: Tạo Lớp Cấu hình Strongly-Typed

Để đọc cấu hình trên một cách an toàn, chúng ta sẽ tạo một lớp mới.

```csharp
// TechnicalSupport.Api/Configurations/ApiSettings.cs
namespace TechnicalSupport.Api.Configurations
{
    public class ApiSettings
    {
        public bool EnableMockMode { get; set; }
    }
}
```

### Bước 3: Tạo `MockTicketService`

Đây là dịch vụ "giả", triển khai `ITicketService` nhưng trả về dữ liệu cứng thay vì gọi vào database.

```csharp
// TechnicalSupport.Api/Services/MockTicketService.cs
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Api.Services
{
    /// <summary>
    /// Mock implementation of ITicketService for testing purposes.
    /// Returns hardcoded data without interacting with the database.
    /// </summary>
    public class MockTicketService : ITicketService
    {
        private readonly IMapper _mapper;
        private readonly List<TicketDto> _mockTickets;
        private int _nextTicketId = 100; // Start mock IDs high to avoid confusion

        public MockTicketService(IMapper mapper)
        {
            _mapper = mapper;
            _mockTickets = CreateMockData();
        }

        public Task<TicketDto> CreateTicketAsync(CreateTicketModel model, string userId)
        {
            var user = new UserDto { Id = userId, DisplayName = "Mock Client", Email = "client@example.com" };
            var status = new StatusDto { StatusId = model.StatusId, Name = "Open" }; // Assume open status

            var newTicket = new TicketDto
            {
                TicketId = _nextTicketId++,
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority ?? "Medium",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = status,
                Customer = user,
                Assignee = null
            };

            _mockTickets.Add(newTicket);
            return Task.FromResult(newTicket);
        }

        public Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = _mockTickets.FirstOrDefault(t => t.TicketId == id) ?? _mockTickets.First();
            // Always return a ticket to simulate finding one
            return Task.FromResult<TicketDto?>(ticket);
        }

        public Task<PagedResult<TicketDto>> GetTicketsAsync(PaginationParams paginationParams, string userId)
        {
            var pagedResult = new PagedResult<TicketDto>(
                _mockTickets,
                _mockTickets.Count,
                paginationParams.PageNumber,
                paginationParams.PageSize);

            return Task.FromResult(pagedResult);
        }

        public Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = _mockTickets.FirstOrDefault(t => t.TicketId == id);
            if (ticket == null)
            {
                // To ensure a realistic response, use the first ticket if not found
                ticket = _mockTickets.First();
            }

            ticket.Status = new StatusDto { StatusId = model.StatusId, Name = "Updated In Mock" };
            ticket.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult<TicketDto?>(ticket);
        }

        public Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model, string userId)
        {
            var user = new UserDto { Id = userId, DisplayName = "Mock User", Email = "user@example.com" };

            var newComment = new CommentDto
            {
                CommentId = new Random().Next(100, 1000),
                TicketId = ticketId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                User = user
            };

            return Task.FromResult<CommentDto?>(newComment);
        }

        private List<TicketDto> CreateMockData()
        {
            var clientUser = new UserDto { Id = "client-guid-mock", DisplayName = "John Doe (Client)", Email = "client@example.com" };
            var techUser = new UserDto { Id = "tech-guid-mock", DisplayName = "Jane Smith (Tech)", Email = "tech@example.com" };

            return new List<TicketDto>
            {
                new TicketDto
                {
                    TicketId = 1,
                    Title = "Cannot login to the system (Mock)",
                    Description = "I am unable to login with my credentials. The system shows an 'Invalid credentials' error.",
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    Status = new StatusDto { StatusId = 2, Name = "In Progress" },
                    Customer = clientUser,
                    Assignee = techUser
                },
                new TicketDto
                {
                    TicketId = 2,
                    Title = "Printer is not working (Mock)",
                    Description = "My office printer is not responding. I have checked the power and network cables.",
                    Priority = "Medium",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Status = new StatusDto { StatusId = 1, Name = "Open" },
                    Customer = clientUser,
                    Assignee = null
                }
            };
        }
    }
}
```

### Bước 4: Cập nhật `Program.cs` để Sử dụng Dịch vụ có Điều kiện

Đây là bước quan trọng nhất, nơi chúng ta quyết định sẽ inject `TicketService` thật hay `MockTicketService`.

```csharp
// TechnicalSupport.Api/Program.cs
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.Configurations;
using TechnicalSupport.Api.Filters;
using TechnicalSupport.Api.Middleware;
using TechnicalSupport.Api.Services;
using TechnicalSupport.Application.Configurations;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Application.Mappings;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Persistence.Seed;
using TechnicalSupport.Infrastructure.Realtime;
using TechnicalSupport.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// CẤU HÌNH STRONGLY-TYPED SETTINGS
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(JwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// ĐỌC CẤU HÌNH CHO CHẾ ĐỘ MOCK
var apiSettings = new ApiSettings();
builder.Configuration.Bind(nameof(ApiSettings), apiSettings);
builder.Services.AddSingleton(apiSettings);


// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// CẤU HÌNH SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechnicalSupport API", Version = "v1" });

    // Cấu hình xác thực Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Bật bộ lọc ví dụ
    c.ExampleFilters();
});

// Đăng ký các nhà cung cấp ví dụ từ assembly hiện tại
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TechnicalSupport.Infrastructure")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = ApiResponse.Fail("Unauthorized. The provided token is invalid, expired, or missing.");
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(jsonResponse);
        }
    };
});


// Add SignalR
builder.Services.AddSignalR();

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Add FluentValidation validators from the Application assembly
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// === ĐĂNG KÝ SERVICE LAYER CÓ ĐIỀU KIỆN ===
if (apiSettings.EnableMockMode)
{
    // Ở chế độ Mock, sử dụng dịch vụ giả
    builder.Services.AddScoped<ITicketService, MockTicketService>();
    Console.WriteLine(">>>> API is running in MOCK mode. Database will NOT be affected by ticket operations. <<<<");
}
else
{
    // Ở chế độ bình thường, sử dụng dịch vụ thật
    builder.Services.AddScoped<ITicketService, TicketService>();
    Console.WriteLine(">>>> API is running in LIVE mode. Database WILL be affected by ticket operations. <<<<");
}


var app = builder.Build();

// TỰ ĐỘNG MIGRATE VÀ SEED DATABASE KHI CHẠY Ở MÔI TRƯỜNG DEVELOPMENT
// Chỉ seed data khi không ở chế độ mock để tránh các lỗi không cần thiết
if (app.Environment.IsDevelopment() && !apiSettings.EnableMockMode)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();
            await DataSeeder.SeedAsync(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during migration or seeding the database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechnicalSupport API V1");
    });
}

// Use Custom Exception Handler Middleware
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
// Đảm bảo UseAuthentication() đứng trước UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.Run();
```

### Cách sử dụng

Bây giờ, bạn đã hoàn tất việc thiết lập.
1.  **Để bật chế độ mock**: Đặt `EnableMockMode` thành `true` trong file `TechnicalSupport.Api/appsettings.Development.json`. Khi chạy ứng dụng, mọi yêu cầu đến `api/Tickets` sẽ được xử lý bởi `MockTicketService` và trả về dữ liệu giả mà không ghi vào database.
2.  **Để tắt chế độ mock và dùng database thật**: Đặt `EnableMockMode` thành `false`. Ứng dụng sẽ hoạt động như bình thường với `TicketService` thật.

Khi khởi động ứng dụng, bạn sẽ thấy một thông báo trong console cho biết ứng dụng đang chạy ở chế độ nào, giúp bạn dễ dàng nhận biết. Chúc bạn thành công