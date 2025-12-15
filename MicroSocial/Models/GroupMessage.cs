using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class GroupMessage
{
    [Key]
    public int GroupMessageId { get; set; }

    public int GroupId { get; set; }
    [ForeignKey("GroupId")]
    public Group Group { get; set; }

    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }

    public string Content { get; set; }
    public DateTime SentAt { get; set; }
}