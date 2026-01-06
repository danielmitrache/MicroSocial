using MicroSocial.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace MicroSocial.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Key]
        public string UserId { get; set; }

        [Required]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Description { get; set; }
        public bool? IsPrivate { get; set; }

        // Relationships
        public ICollection<Post>? Posts { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<GroupMessage>? GroupMessages { get; set; }

        [InverseProperty("Moderator")]
        public ICollection<Group>? ModeratedGroups { get; set; }
        public ICollection<UserGroup>? UserGroups { get; set; }
        public ICollection<Liked>? Likes { get; set; }

        public ICollection<Follow>? Followers { get; set; }
        public ICollection<Follow>? Following { get; set; }
    }
}