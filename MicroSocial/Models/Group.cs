using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MicroSocial.Models
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100, ErrorMessage = "Group name can not be more than 100 characters long.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Group description is required.")]
        [StringLength(500, ErrorMessage = "Group description can not be more than 500 characters long.")]
        public string Description { get; set; }
        public string ModeratorId { get; set; }
        [ForeignKey("ModeratorId")]
        public ApplicationUser? Moderator { get; set; }

        public ICollection<UserGroup>? UserGroups { get; set; }
        public ICollection<GroupMessage>? GroupMessages { get; set; }
    }
}