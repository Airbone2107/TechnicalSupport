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
    /// <summary>
    /// Xử lý các yêu cầu đăng ký và đăng nhập người dùng.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// Khởi tạo một instance mới của AuthController.
        /// </summary>
        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtSettings jwtSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Đăng ký một tài khoản người dùng mới.
        /// </summary>
        /// <param name="model">Thông tin đăng ký của người dùng.</param>
        /// <returns>Thông báo đăng ký thành công hoặc danh sách lỗi.</returns>
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

        /// <summary>
        /// Đăng nhập vào hệ thống và nhận về một JWT token.
        /// </summary>
        /// <param name="model">Thông tin đăng nhập (email và mật khẩu).</param>
        /// <returns>Một đối tượng chứa JWT token nếu đăng nhập thành công.</returns>
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

        /// <summary>
        /// Tạo một JWT token cho người dùng đã được xác thực.
        /// </summary>
        /// <param name="user">Đối tượng người dùng.</param>
        /// <param name="roles">Danh sách vai trò của người dùng.</param>
        /// <returns>Chuỗi JWT token.</returns>
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

            // Thêm các claim về quyền hạn (permissions) dựa trên vai trò của người dùng.
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

        /// <summary>
        /// Lấy một tập hợp các quyền (permissions) dựa trên danh sách vai trò của người dùng.
        /// Đây là nơi định nghĩa tập quyền cho từng vai trò trong hệ thống.
        /// </summary>
        /// <param name="roles">Danh sách vai trò của người dùng.</param>
        /// <returns>Một HashSet chứa các chuỗi permission duy nhất.</returns>
        private static HashSet<string> GetPermissionsForRoles(IEnumerable<string> roles)
        {
            var permissions = new HashSet<string>();

            if (roles.Contains("Client"))
            {
                permissions.Add("tickets:create");
                permissions.Add("tickets:read_own");
                permissions.Add("tickets:add_comment");
            }

            if (roles.Contains("Agent"))
            {
                permissions.Add("tickets:create");
                permissions.Add("tickets:read_own");
                permissions.Add("tickets:read_queue");
                permissions.Add("tickets:update_status");
                permissions.Add("tickets:add_comment");
                permissions.Add("tickets:claim");
                permissions.Add("permissions:request");
            }

            if (roles.Contains("Group Manager"))
            {
                permissions.Add("tickets:read_group");
                permissions.Add("tickets:assign_to_member");
                permissions.Add("tickets:reject_from_group");
                permissions.Add("groups:read_own_members");
            }

            if (roles.Contains("Manager"))
            {
                permissions.Add("users:manage");
                permissions.Add("users:read");
                permissions.Add("groups:manage");
                permissions.Add("permissions:review");
            }
            
            if (roles.Contains("Ticket Manager"))
            {
                permissions.Add("tickets:create");
                permissions.Add("tickets:read_own");
                permissions.Add("tickets:add_comment");
                permissions.Add("tickets:read_queue");
                permissions.Add("tickets:read_all");
                permissions.Add("tickets:assign_to_group");
                permissions.Add("tickets:update_status");
                permissions.Add("problemtypes:manage");
            }
            
            if (roles.Contains("Admin"))
            {
                // Admin kế thừa tất cả các quyền đã định nghĩa.
                permissions.UnionWith(new string[]
                {
                    "tickets:create", "tickets:read_own", "tickets:read_queue", "tickets:update_status",
                    "tickets:add_comment", "tickets:claim", "permissions:request", "tickets:read_group",
                    "tickets:assign_to_member", "tickets:reject_from_group", "users:manage", "users:read",
                    "groups:manage", "permissions:review", "tickets:read_all", "tickets:assign_to_group",
                    "problemtypes:manage", "users:delete", "tickets:delete", "groups:read_own_members"
                });
            }

            return permissions;
        }
    }
}