using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MicroSocial.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly MicroSocial.Services.IContentModerationService _moderationService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            MicroSocial.Services.IContentModerationService moderationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _moderationService = moderationService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
                
                // Logic to set FirstName from Email
                string username = string.Empty;
                if (!string.IsNullOrEmpty(Input.Email))
                {
                    var parts = Input.Email.Split('@');
                    if (parts.Length > 0)
                    {
                        username = parts[0];
                        user.FirstName = username;
                    }
                }

                // Moderate username before allowing registration
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var usernameModeration = await _moderationService.ModerateContentAsync(username);
                    if (!usernameModeration.IsApproved)
                    {
                        ModelState.AddModelError("Input.Email", "Numele de utilizator conține termeni nepotriviți. Te rugăm să folosești o altă adresă de email.");
                        return Page();
                    }
                }

                // Handle the strange UserId property if necessary. 
                // IdentityUser sets Id automatically. If UserId is mapped to Id, it's fine.
                // If it's a separate required field without default, we might have issue.
                // Assuming it's handled or we can set it to a new Guid just in case it's a required secondary key.
                user.UserId = Guid.NewGuid().ToString();

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
