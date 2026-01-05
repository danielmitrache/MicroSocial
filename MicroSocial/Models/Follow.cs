using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocial.Models
{
    public class Follow
    {
        public string FollowedUserId { get; set; }
        [ForeignKey("FollowedUserId")]
        public ApplicationUser? FollowedUser { get; set; }

        public string FollowingUserId { get; set; }
        [ForeignKey("FollowingUserId")]
        public ApplicationUser? FollowingUser { get; set; }

        public bool Status { get; set; }
    }
}