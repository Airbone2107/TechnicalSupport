// TechnicalSupport.Api/Program.cs
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.Filters;
using TechnicalSupport.Api.Middleware;
using TechnicalSupport.Api.Services;
using TechnicalSupport.Api.Swagger;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Application.Configurations;
using TechnicalSupport.Application.Mappings;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Persistence.Seed;
using TechnicalSupport.Infrastructure.Realtime;

// Import các Abstractions và Implementations từ cấu trúc mới
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Infrastructure.Features.Tickets;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Infrastructure.Features.Attachments;
using TechnicalSupport.Application.Features.Admin.Abstractions;
using TechnicalSupport.Infrastructure.Features.Admin;
using TechnicalSupport.Application.Features.Comments.Abstractions;
using TechnicalSupport.Infrastructure.Features.Comments;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Infrastructure.Features.Groups;
using TechnicalSupport.Application.Features.Permissions.Abstractions;
using TechnicalSupport.Infrastructure.Features.Permissions;
using TechnicalSupport.Infrastructure.Authorization;
// Thêm using cho ProblemType Service
using TechnicalSupport.Application.Features.ProblemTypes.Abstractions;
using TechnicalSupport.Infrastructure.Features.ProblemTypes;

var builder = WebApplication.CreateBuilder(args);

// CẤU HÌNH STRONGLY-TYPED SETTINGS
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(JwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var attachmentSettings = new AttachmentSettings();
builder.Configuration.Bind(nameof(AttachmentSettings), attachmentSettings);
builder.Services.AddSingleton(attachmentSettings);

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
    
    // THÊM BỘ LỌC HEADER TÙY CHỈNH CHO CHẾ ĐỘ TEST
    c.OperationFilter<TestModeHeaderFilter>();

    // Bật bộ lọc ví dụ
    c.ExampleFilters();
});

// Đăng ký các nhà cung cấp ví dụ từ assembly hiện tại
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // URL của React app
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // RẤT QUAN TRỌNG CHO SIGNALR
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


// === CẤU HÌNH AUTHORIZATION POLICIES ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());

    // --- Tickets ---
    options.AddPolicy("CreateTickets", policy => policy.RequireClaim("permissions", "tickets:create"));
    options.AddPolicy("ReadOwnTickets", policy => policy.RequireClaim("permissions", "tickets:read_own"));
    options.AddPolicy("ReadTicketQueue", policy => policy.RequireClaim("permissions", "tickets:read_queue", "tickets:read_all"));
    options.AddPolicy("ReadAllTickets", policy => policy.RequireClaim("permissions", "tickets:read_all"));
    options.AddPolicy("ReadGroupTickets", policy => policy.RequireClaim("permissions", "tickets:read_group"));
    options.AddPolicy("UpdateTicketStatus", policy => policy.RequireClaim("permissions", "tickets:update_status"));
    
    // Sửa lỗi: Đổi tên policy "AssignToMember", "AssignToGroup" thành một policy chung "AssignTickets"
    // hoặc giữ riêng tùy logic. Ở đây ta giữ riêng để rõ ràng nhưng thêm một policy chung cho endpoint.
    options.AddPolicy("AssignTickets", policy => policy.RequireClaim("permissions", "tickets:assign_to_member", "tickets:assign_to_group"));
    options.AddPolicy("AssignToMember", policy => policy.RequireClaim("permissions", "tickets:assign_to_member"));
    options.AddPolicy("AssignToGroup", policy => policy.RequireClaim("permissions", "tickets:assign_to_group"));

    options.AddPolicy("RejectFromGroup", policy => policy.RequireClaim("permissions", "tickets:reject_from_group"));
    options.AddPolicy("DeleteTickets", policy => policy.RequireClaim("permissions", "tickets:delete"));
    options.AddPolicy("ClaimTickets", policy => policy.RequireClaim("permissions", "tickets:claim"));
    options.AddPolicy("AddComments", policy => policy.RequireClaim("permissions", "tickets:add_comment"));
    
    // --- Users ---
    options.AddPolicy("ReadUsers", policy => policy.RequireClaim("permissions", "users:read"));
    options.AddPolicy("ManageUsers", policy => policy.RequireClaim("permissions", "users:manage"));
    options.AddPolicy("DeleteUsers", policy => policy.RequireClaim("permissions", "users:delete"));
    // THÊM POLICY MỚI
    options.AddPolicy("ManageUserRoles", policy => policy.RequireRole("Admin", "Manager"));

    // --- Groups ---
    options.AddPolicy("ManageGroups", policy => policy.RequireClaim("permissions", "groups:manage"));
    
    // --- Problem Types ---
    options.AddPolicy("ManageProblemTypes", policy => policy.RequireClaim("permissions", "problemtypes:manage"));

    // --- Permissions ---
    options.AddPolicy("RequestPermissions", policy => policy.RequireClaim("permissions", "permissions:request"));
    options.AddPolicy("ReviewPermissions", policy => policy.RequireClaim("permissions", "permissions:review"));
});


// Đăng ký Authorization Handlers và các dịch vụ cần thiết
builder.Services.AddScoped<IAuthorizationHandler, TicketAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommentAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();

// Add SignalR
builder.Services.AddSignalR();

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Add FluentValidation validators from the Application assembly
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// === ĐĂNG KÝ SERVICE LAYER ===
// Đăng ký các dịch vụ thật
builder.Services.AddScoped<TicketService>(); // Đăng ký lớp cụ thể để decorator có thể lấy
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IPermissionRequestService, PermissionRequestService>();
builder.Services.AddScoped<IProblemTypeService, ProblemTypeService>(); // Đăng ký service mới

// Sử dụng Decorator Pattern để bọc TicketService cho việc kiểm thử giao dịch
builder.Services.AddScoped<ITicketService>(provider => 
    new TransactionalTicketServiceDecorator(
        provider.GetRequiredService<TicketService>(), // Lấy service thật
        provider.GetRequiredService<ApplicationDbContext>(),
        provider.GetRequiredService<IHttpContextAccessor>()
    )
);

Console.WriteLine(">>>> API is running in LIVE mode. Use 'X-Test-Mode' header for safe testing. <<<<");


var app = builder.Build();

// TỰ ĐỘNG MIGRATE VÀ SEED DATABASE KHI CHẠY Ở MÔI TRƯỜNG DEVELOPMENT
if (app.Environment.IsDevelopment())
{
    // Đảm bảo thư mục lưu trữ tồn tại
    var attachmentsDir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", attachmentSettings.StoragePath);
    if (!Directory.Exists(attachmentsDir))
    {
        Directory.CreateDirectory(attachmentsDir);
    }
    
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Đổi tên vai trò cũ "Technician" thành "Agent"
            await DataSeeder.MigrateRoles(roleManager);
            await context.Database.MigrateAsync();

            // Sửa đổi ở đây: truyền toàn bộ service provider
            await DataSeeder.SeedAsync(services);
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

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.Run();