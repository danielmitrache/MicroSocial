using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MicroSocial.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}