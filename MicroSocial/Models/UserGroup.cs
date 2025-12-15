using System.ComponentModel.DataAnnotations.Schema;

public class UserGroup
{
    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }

    public int GroupId { get; set; }
    [ForeignKey("GroupId")]
    public Group Group { get; set; }

    public bool Status { get; set; }
}