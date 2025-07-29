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

    // === SỬA LỖI Ở ĐÂY: CẤU HÌNH SWAGGER ĐỂ HỖ TRỢ BEARER TOKEN ĐÚNG CHUẨN ===
    // Thay vì dùng ApiKey, chúng ta sẽ dùng Http, là loại scheme dành riêng cho các chuẩn
    // xác thực HTTP như Bearer.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        // Mô tả này sẽ hướng dẫn người dùng cách sử dụng đúng.
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, // <- THAY ĐỔI QUAN TRỌNG: từ ApiKey sang Http
        Scheme = "bearer", // <- Đặt scheme là 'bearer' (viết thường theo chuẩn)
        BearerFormat = "JWT" // <- Định dạng của token là JWT
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // ID này phải khớp với ID đã định nghĩa ở trên
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

// Thêm dòng này cùng với các đăng ký service khác
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

// Thêm dòng này cùng với các đăng ký service khác
builder.Services.AddScoped<IAdminService, AdminService>();

// TechnicalSupport.Api/Program.cs
builder.Services.AddScoped<IGroupService, GroupService>();

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