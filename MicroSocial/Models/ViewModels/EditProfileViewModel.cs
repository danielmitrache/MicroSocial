using System.ComponentModel.DataAnnotations;

namespace MicroSocial.Models.ViewModels
{
    public class EditProfileViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        [Display(Name = "Bio")]
        public string? Description { get; set; }
        
        [Display(Name = "Private Profile")]
        public bool IsPrivate { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImage { get; set; }

        public string? ProfilePicture { get; set; } // For display
    }
}
