using System.ComponentModel.DataAnnotations.Schema;

public class Liked
{
    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }

    public int PostId { get; set; }
    [ForeignKey("PostId")]
    public Post Post { get; set; }
}