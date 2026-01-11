using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MicroSocial.Models
{
    public class GroupMessage
    {
        [Key]
        public int GroupMessageId { get; set; }

        public int GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Message can not be empty")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}