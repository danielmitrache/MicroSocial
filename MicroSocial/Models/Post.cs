using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public enum MediaType { Image, Video, None }

namespace MicroSocial.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(2000, ErrorMessage = "Content can not be more than 2000 characters long.")]
        public string Content { get; set; }
        public string? MediaPath { get; set; }
        public MediaType MediaType { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<Liked>? Likes { get; set; }
    }
}