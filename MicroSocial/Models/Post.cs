using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public enum MediaType { Image, Video, None }

public class Post
{
    [Key]
    public int PostId { get; set; }

    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }

    public string Content { get; set; }
    public string MediaPath { get; set; }
    public MediaType MediaType { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Comment> Comments { get; set; }
    public ICollection<Liked> Likes { get; set; }
}