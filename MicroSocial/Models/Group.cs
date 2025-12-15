using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Group
{
    [Key]
    public int GroupId { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public string ModeratorId { get; set; }
    [ForeignKey("ModeratorId")]
    public ApplicationUser Moderator { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; }
    public ICollection<GroupMessage> GroupMessages { get; set; }
}