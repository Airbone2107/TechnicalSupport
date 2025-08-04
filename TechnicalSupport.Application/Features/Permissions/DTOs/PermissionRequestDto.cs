using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Application.Features.Permissions.DTOs
{
    public class PermissionRequestDto
    {
        public int Id { get; set; }
        public UserDto Requester { get; set; }
        public string RequestedPermission { get; set; }
        public string Justification { get; set; }
        public PermissionRequestStatus Status { get; set; }
        public UserDto? Processor { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessorNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 