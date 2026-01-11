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
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? FirstName { get; set; }
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }
        [Display(Name = "Private Profile")]
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