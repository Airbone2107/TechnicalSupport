using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechnicalSupport.Domain.Entities
{
    public class ProblemType
    {
        public int ProblemTypeId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public int? GroupId { get; set; }

        [ForeignKey("GroupId")]
        public Group? Group { get; set; }
    }
} 