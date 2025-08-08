namespace TechnicalSupport.Application.Features.Admin.DTOs
{
    public class UpdateUserRolesModel
    {
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Mật khẩu của người dùng hiện tại (admin/manager) để xác thực hành động.
        /// </summary>
        public string CurrentPassword { get; set; }
    }
} 