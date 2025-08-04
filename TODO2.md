Chào bạn, tôi đã xem xét các lỗi bạn cung cấp. Các lỗi này phát sinh do một số cập nhật không nhất quán trong các tệp ví dụ và cách xử lý lỗi trong Controller. Tôi đã sửa chúng.

Dưới đây là mã nguồn đầy đủ của các tệp đã được sửa lỗi. Bạn chỉ cần sao chép và dán để thay thế nội dung các tệp tương ứng.

### 1. Sửa lỗi trong `CreateTicketModelExample.cs`

Lỗi này xảy ra vì `CreateTicketModel` đã được thay đổi để sử dụng `ProblemTypeId` thay vì `StatusId`, nhưng tệp ví dụ chưa được cập nhật.

```csharp
// TechnicalSupport.Api/SwaggerExamples/Tickets/CreateTicketModelExample.cs
using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Tickets
{
    public class CreateTicketModelExample : IExamplesProvider<CreateTicketModel>
    {
        public CreateTicketModel GetExamples()
        {
            return new CreateTicketModel
            {
                Title = "Cannot connect to the network",
                Description = "My computer is showing 'No Internet Connection' despite being connected via Ethernet.",
                ProblemTypeId = 2, // 2: Lỗi Phần mềm (ví dụ)
                Priority = "High"
            };
        }
    }
}
```

### 2. Sửa lỗi trong `TicketsController.cs`

Lỗi này xảy ra do phương thức `Forbid()` không được thiết kế để nhận một đối tượng `ApiResponse`. Chúng ta cần sử dụng `StatusCode(403, ...)` để trả về mã lỗi 403 Forbidden cùng với nội dung tùy chỉnh.

```csharp
// TechnicalSupport.Api/Features/Tickets/TicketsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.SwaggerExamples.Tickets;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.Features.Tickets
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAuthenticatedUser")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets([FromQuery] TicketFilterParams filterParams)
        {
            var pagedResultDto = await _ticketService.GetTicketsAsync(filterParams);
            return Ok(ApiResponse.Success(pagedResultDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            try
            {
                var ticketDto = await _ticketService.GetTicketByIdAsync(id);
                if (ticketDto == null)
                {
                    return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                }
                return Ok(ApiResponse.Success(ticketDto));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Authorize(Policy = "CreateTickets")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var ticketDto = await _ticketService.CreateTicketAsync(model);
            return CreatedAtAction(nameof(GetTicket), new { id = ticketDto.TicketId }, ApiResponse.Success(ticketDto, "Ticket created successfully."));
        }
        
        [HttpPost("{id}/claim")]
        [Authorize(Policy = "ClaimTickets")]
        public async Task<IActionResult> ClaimTicket(int id)
        {
            try
            {
                var ticketDto = await _ticketService.ClaimTicketAsync(id);
                return Ok(ApiResponse.Success(ticketDto, "Ticket claimed successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }
        
        [HttpPost("{id}/reject-from-group")]
        [Authorize(Policy = "RejectFromGroup")]
        public async Task<IActionResult> RejectFromGroup(int id)
        {
            try
            {
                var ticketDto = await _ticketService.RejectFromGroupAsync(id);
                return Ok(ApiResponse.Success(ticketDto, "Ticket has been returned to the triage queue."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpPut("{id}/status")]
        [Authorize(Policy = "UpdateTicketStatus")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusModel model)
        {
            try
            {
                var ticketDto = await _ticketService.UpdateTicketStatusAsync(id, model);
                if (ticketDto == null) return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                return Ok(ApiResponse.Success(ticketDto, "Ticket status updated successfully."));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/comments")]
        [Authorize(Policy = "AddComments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentModel model)
        {
            try
            {
                var commentDto = await _ticketService.AddCommentAsync(id, model);
                if (commentDto == null) return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                return Ok(ApiResponse.Success(commentDto, "Comment added successfully."));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketModel model)
        {
            try
            {
                var ticketDto = await _ticketService.AssignTicketAsync(id, model);
                return Ok(ApiResponse.Success(ticketDto, "Ticket assigned successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpPut("{id}/assign-group")]
        public async Task<IActionResult> AssignTicketToGroup(int id, [FromBody] AssignGroupModel model)
        {
            try
            {
                var ticketDto = await _ticketService.AssignTicketToGroupAsync(id, model);
                return Ok(ApiResponse.Success(ticketDto, "Ticket assigned to group successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DeleteTickets")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var (success, message) = await _ticketService.DeleteTicketAsync(id);
            if (!success) return NotFound(ApiResponse.Fail(message));
            return Ok(ApiResponse.Success<object>(null, message));
        }
    }
}
```

### 3. Sửa cảnh báo trong `AuthController.cs`

Phương thức `GenerateJwtToken` không cần phải là `async` vì nó không chứa toán tử `await`. Tôi sẽ đổi nó thành một phương thức đồng bộ.

```csharp
// TechnicalSupport.Api/Features/Authentication/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.SwaggerExamples.Authentication;
using TechnicalSupport.Application.Configurations;
using TechnicalSupport.Application.Features.Authentication.DTOs;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Api.Features.Authentication
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtSettings jwtSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings;
        }

        [HttpPost("register")]
        [SwaggerRequestExample(typeof(RegisterModel), typeof(RegisterModelExample))]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName,
                Expertise = model.Expertise
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse.Fail("User registration failed.", errors));
            }

            await _userManager.AddToRoleAsync(user, model.Role ?? "Client");

            return Ok(ApiResponse.Success<object>(null, "User registered successfully."));
        }

        [HttpPost("login")]
        [SwaggerRequestExample(typeof(LoginModel), typeof(LoginModelExample))]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponse.Fail("Invalid credentials."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse.Fail("Invalid credentials."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            var response = new AuthResponseDto { Token = token };

            return Ok(ApiResponse.Success(response, "Login successful."));
        }
        
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
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

            // Thêm claim permissions dựa trên roles
            var permissions = GetPermissionsForRoles(roles);
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permissions", permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static HashSet<string> GetPermissionsForRoles(IEnumerable<string> roles)
        {
            var permissions = new HashSet<string>();

            // --- QUYỀN CƠ BẢN ---
            if (roles.Contains("Client"))
            {
                permissions.Add("tickets:create");
                permissions.Add("tickets:read_own");
                permissions.Add("tickets:add_comment");
            }

            if (roles.Contains("Agent"))
            {
                permissions.Add("tickets:read_queue");
                permissions.Add("tickets:update_status");
                permissions.Add("tickets:add_comment");
                permissions.Add("tickets:claim"); // Tự nhận ticket trong nhóm
                permissions.Add("permissions:request");
            }
            
            // --- VAI TRÒ MỚI: GROUP MANAGER ---
            if (roles.Contains("Group Manager"))
            {
                permissions.Add("tickets:read_group"); // Xem tất cả ticket trong nhóm
                permissions.Add("tickets:assign_to_member"); // Gán ticket cho thành viên trong nhóm
                permissions.Add("tickets:reject_from_group"); // Đẩy ticket ra khỏi nhóm
            }

            // --- VAI TRÒ CŨ: MANAGER (chỉ quản lý user) ---
            if (roles.Contains("Manager"))
            {
                permissions.Add("users:manage");
                permissions.Add("users:read");
                permissions.Add("groups:manage");
                permissions.Add("permissions:review");
            }
            
            // --- VAI TRÒ MỚI: TICKET MANAGER ---
            if (roles.Contains("Ticket Manager"))
            {
                permissions.Add("tickets:read_all"); // Xem tất cả các ticket
                permissions.Add("tickets:assign_to_group"); // Gán ticket cho nhóm bất kỳ
                permissions.Add("problemtypes:manage"); // Quản lý Problem Types
            }

            // --- ADMIN ---
            if (roles.Contains("Admin"))
            {
                // Kế thừa quyền của tất cả các vai trò khác
                permissions.UnionWith(GetPermissionsForRoles(new[] { "Client", "Agent", "Group Manager", "Manager", "Ticket Manager" }));

                // Thêm các quyền chỉ Admin mới có
                permissions.Add("tickets:delete");
                permissions.Add("users:delete");
            }

            return permissions;
        }
    }
}
```

Sau khi áp dụng các thay đổi này, dự án của bạn sẽ biên dịch thành công.