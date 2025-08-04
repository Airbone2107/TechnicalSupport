using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities
{
    public enum PermissionRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class PermissionRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; }
        public ApplicationUser Requester { get; set; }

        [Required]
        public string RequestedPermission { get; set; } // VD: "ROLE:Manager" hoặc "TEMP_PERM:Ticket:Update:123"

        public string? ResourceId { get; set; }
        public string? ResourceType { get; set; }

        [Required]
        public string Justification { get; set; } // Lý do yêu cầu

        public PermissionRequestStatus Status { get; set; } = PermissionRequestStatus.Pending;

        public string? ProcessorId { get; set; } // ID của người xử lý (Admin/Manager)
        public ApplicationUser? Processor { get; set; }

        public DateTime? ProcessedAt { get; set; }
        public string? ProcessorNotes { get; set; } // Ghi chú của người xử lý
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 