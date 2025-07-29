using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Features.Admin.DTOs
{
    public class UpdateUserByAdminModel
    {
        public string DisplayName { get; set; }
        public List<string> Roles { get; set; }
        public bool IsLocked { get; set; }
    }
}
