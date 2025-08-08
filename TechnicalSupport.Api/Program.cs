using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.Filters;
using TechnicalSupport.Api.Middleware;
using TechnicalSupport.Api.Services;
using TechnicalSupport.Api.Swagger;
using TechnicalSupport.Application.Configurations;
using TechnicalSupport.Application.Features.Admin.Abstractions;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Application.Features.Comments.Abstractions;
using TechnicalSupport.Application.Features.Groups.Abstractions;
using TechnicalSupport.Application.Features.Permissions.Abstractions;
using TechnicalSupport.Application.Features.ProblemTypes.Abstractions;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Mappings;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Authorization;
using TechnicalSupport.Infrastructure.Features.Admin;
using TechnicalSupport.Infrastructure.Features.Attachments;
using TechnicalSupport.Infrastructure.Features.Comments;
using TechnicalSupport.Infrastructure.Features.Groups;
using TechnicalSupport.Infrastructure.Features.Permissions;
using TechnicalSupport.Infrastructure.Features.ProblemTypes;
using TechnicalSupport.Infrastructure.Features.Tickets;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Infrastructure.Persistence.Seed;
using TechnicalSupport.Infrastructure.Realtime;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các lớp cấu hình strongly-typed
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(JwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var attachmentSettings = new AttachmentSettings();
builder.Configuration.Bind(nameof(AttachmentSettings), attachmentSettings);
builder.Services.AddSingleton(attachmentSettings);

// Thêm các dịch vụ vào container
builder.Services.AddControllers(options =>
{
    // Áp dụng bộ lọc validation cho tất cả các action
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// Cấu hình Swagger/OpenAPI
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

    c.OperationFilter<TestModeHeaderFilter>();
    c.ExampleFilters();
});
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // URL của React app
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Quan trọng cho SignalR
        });
});

// Cấu hình DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TechnicalSupport.Infrastructure")));

// Cấu hình ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cấu hình xác thực JWT
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
        // Cho phép SignalR xác thực qua query string, vì trình duyệt không gửi header 'Authorization' qua WebSocket.
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ticketHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
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

// Cấu hình các chính sách (policies) phân quyền
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());

    // Policies cho Tickets
    options.AddPolicy("CreateTickets", policy => policy.RequireClaim("permissions", "tickets:create"));
    options.AddPolicy("ReadOwnTickets", policy => policy.RequireClaim("permissions", "tickets:read_own"));
    options.AddPolicy("ReadTicketQueue", policy => policy.RequireClaim("permissions", "tickets:read_queue", "tickets:read_all"));
    options.AddPolicy("ReadAllTickets", policy => policy.RequireClaim("permissions", "tickets:read_all"));
    options.AddPolicy("ReadGroupTickets", policy => policy.RequireClaim("permissions", "tickets:read_group"));
    options.AddPolicy("UpdateTicketStatus", policy => policy.RequireClaim("permissions", "tickets:update_status"));
    options.AddPolicy("AssignTickets", policy => policy.RequireClaim("permissions", "tickets:assign_to_member", "tickets:assign_to_group"));
    options.AddPolicy("AssignToMember", policy => policy.RequireClaim("permissions", "tickets:assign_to_member"));
    options.AddPolicy("AssignToGroup", policy => policy.RequireClaim("permissions", "tickets:assign_to_group"));
    options.AddPolicy("RejectFromGroup", policy => policy.RequireClaim("permissions", "tickets:reject_from_group"));
    options.AddPolicy("DeleteTickets", policy => policy.RequireClaim("permissions", "tickets:delete"));
    options.AddPolicy("ClaimTickets", policy => policy.RequireClaim("permissions", "tickets:claim"));
    options.AddPolicy("AddComments", policy => policy.RequireClaim("permissions", "tickets:add_comment"));

    // Policies cho Users
    options.AddPolicy("ReadUsers", policy => policy.RequireClaim("permissions", "users:read"));
    options.AddPolicy("ManageUsers", policy => policy.RequireClaim("permissions", "users:manage"));
    options.AddPolicy("DeleteUsers", policy => policy.RequireClaim("permissions", "users:delete"));
    options.AddPolicy("ManageUserRoles", policy => policy.RequireRole("Admin", "Manager"));

    // Policies cho Groups
    options.AddPolicy("ManageGroups", policy => policy.RequireClaim("permissions", "groups:manage"));
    options.AddPolicy("ViewGroups", policy => policy.RequireClaim("permissions", "groups:manage", "tickets:assign_to_group"));
    options.AddPolicy("ReadGroupMembers", policy => policy.RequireClaim("permissions", "groups:manage", "groups:read_own_members"));

    // Policies cho Problem Types
    options.AddPolicy("ManageProblemTypes", policy => policy.RequireClaim("permissions", "problemtypes:manage"));

    // Policies cho Permissions
    options.AddPolicy("RequestPermissions", policy => policy.RequireClaim("permissions", "permissions:request"));
    options.AddPolicy("ReviewPermissions", policy => policy.RequireClaim("permissions", "permissions:review"));
});

// Đăng ký các Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, TicketAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommentAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();

// Đăng ký SignalR
builder.Services.AddSignalR();

// Đăng ký AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Đăng ký các Validators của FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// Đăng ký các lớp dịch vụ (Service Layer)
builder.Services.AddScoped<TicketService>(); // Lớp cụ thể để Decorator có thể lấy
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IPermissionRequestService, PermissionRequestService>();
builder.Services.AddScoped<IProblemTypeService, ProblemTypeService>();

// Áp dụng Decorator Pattern cho ITicketService để xử lý giao dịch trong chế độ test
builder.Services.AddScoped<ITicketService>(provider =>
    new TransactionalTicketServiceDecorator(
        provider.GetRequiredService<TicketService>(),
        provider.GetRequiredService<ApplicationDbContext>(),
        provider.GetRequiredService<IHttpContextAccessor>()
    )
);

Console.WriteLine(">>>> API is running in LIVE mode. Use 'X-Test-Mode' header for safe testing. <<<<");

var app = builder.Build();

// Tự động migrate và seed database khi chạy ở môi trường Development
if (app.Environment.IsDevelopment())
{
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

            await DataSeeder.MigrateRoles(roleManager);
            await context.Database.MigrateAsync();
            await DataSeeder.SeedAsync(services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during migration or seeding the database.");
        }
    }
}

// Cấu hình pipeline xử lý HTTP request
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechnicalSupport API V1");
    });
}

// Sử dụng Middleware xử lý ngoại lệ tùy chỉnh
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.Run();