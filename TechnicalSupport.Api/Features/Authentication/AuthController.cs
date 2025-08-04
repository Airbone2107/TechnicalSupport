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

            // --- QUYỀN CỦA CLIENT ---
            if (roles.Contains("Client"))
            {
                permissions.Add("tickets:create");
                permissions.Add("tickets:read_own");
                permissions.Add("tickets:add_comment");
            }

            // --- QUYỀN CỦA AGENT ---
            // Agent cũng là một client (có thể tự tạo ticket cho mình)
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
            
            // --- QUYỀN CỦA GROUP MANAGER ---
            // Group Manager là một Agent với quyền bổ sung
            if (roles.Contains("Group Manager"))
            {
                permissions.Add("tickets:read_group");
                permissions.Add("tickets:assign_to_member");
                permissions.Add("tickets:reject_from_group");
            }

            // --- QUYỀN CỦA USER MANAGER (VAI TRÒ CŨ: MANAGER) ---
            if (roles.Contains("Manager"))
            {
                permissions.Add("users:manage");
                permissions.Add("users:read");
                permissions.Add("groups:manage");
                permissions.Add("permissions:review");
            }
            
            // --- QUYỀN CỦA TICKET MANAGER ---
            // Ticket Manager cũng là một Agent
            if (roles.Contains("Ticket Manager"))
            {
                permissions.Add("tickets:read_all");
                permissions.Add("tickets:assign_to_group");
                permissions.Add("problemtypes:manage");
            }

            // --- ADMIN KẾ THỪA TẤT CẢ ---
            if (roles.Contains("Admin"))
            {
                permissions.UnionWith(GetPermissionsForRoles(new[] { "Client", "Agent", "Group Manager", "Manager", "Ticket Manager" }));
                // Quyền chỉ Admin mới có
                permissions.Add("users:delete");
                permissions.Add("tickets:delete");
            }

            return permissions;
        }
    }
} 