using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Features.Groups.DTOs
{
    public class GroupDto
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
