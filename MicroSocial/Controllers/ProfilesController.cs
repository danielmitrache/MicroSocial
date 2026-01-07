using MicroSocial.Data;
using MicroSocial.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{
    [Authorize]
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProfilesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Profiles
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Show), new { id = user.Id });
        }

        // GET: Profiles/Show/5
        [AllowAnonymous]
        public async Task<IActionResult> Show(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Likes)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.Followers)
                    .ThenInclude(f => f.FollowingUser)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Order posts by date
            user.Posts = user.Posts.OrderByDescending(p => p.CreatedAt).ToList();

            ViewBag.IsCurrentUser = false;
            ViewBag.IsFollowing = false;
            ViewBag.RequestSent = false;
            
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                     if (currentUser.Id == user.Id)
                     {
                        ViewBag.IsCurrentUser = true;
                        // Load pending requests for the owner
                        ViewBag.PendingRequests = user.Followers
                            .Where(f => f.Status == false)
                            .Select(f => f.FollowingUser)
                            .ToList();
                     }
                     else
                     {
                         var follow = user.Followers.FirstOrDefault(f => f.FollowingUserId == currentUser.Id);
                         if (follow != null)
                         {
                             if (follow.Status)
                             {
                                 ViewBag.IsFollowing = true;
                             }
                             else
                             {
                                 ViewBag.RequestSent = true;
                             }
                         }
                     }
                }
            }

            if (user.IsPrivate == true && !ViewBag.IsCurrentUser && !ViewBag.IsFollowing && !User.IsInRole("Administrator"))
            {
                user.Posts = new List<Post>(); // Hide posts
                ViewBag.IsPrivateProfile = true;
            }

            return View(user);
        }

        // GET: Profiles/Edit
        public async Task<IActionResult> Edit(string? id)
        {
            ApplicationUser? user = null;
            var currentUser = await _userManager.GetUserAsync(User);

            if (string.IsNullOrEmpty(id))
            {
                user = currentUser;
            }
            else
            {
                if (currentUser.Id != id && !User.IsInRole("Administrator"))
                {
                    return Forbid();
                }
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            }

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Profiles/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(string id, ApplicationUser userUpdate, IFormFile? profileImage)
        {
            if (id != userUpdate.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id != id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            userToUpdate.FirstName = userUpdate.FirstName;
            userToUpdate.LastName = userUpdate.LastName;
            userToUpdate.Description = userUpdate.Description;
            userToUpdate.IsPrivate = userUpdate.IsPrivate;

            if (profileImage != null)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string fileName = Path.GetFileNameWithoutExtension(profileImage.FileName);
                string extension = Path.GetExtension(profileImage.FileName);
                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                string path = Path.Combine(wwwRootPath + "/images/profiles/", fileName);

                // Ensure directory exists
                var directory = Path.Combine(wwwRootPath, "images", "profiles");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                userToUpdate.ProfilePicture = "/images/profiles/" + fileName;
            }

            var result = await _userManager.UpdateAsync(userToUpdate);
            if (!result.Succeeded)
            {
                 foreach (var error in result.Errors)
                 {
                     ModelState.AddModelError(string.Empty, error.Description);
                 }
                 return View(userUpdate);
            }

            return RedirectToAction(nameof(Show), new { id = userToUpdate.Id });
        }

        // POST: Profiles/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Delete User
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Posts"); // Redirect to home/posts
            }

            // Handle errors (maybe redirect with error? for now just redirect posts)
            return RedirectToAction(nameof(Show), new { id = id });
        }

        // GET: Profiles/Search
        [AllowAnonymous]
        public async Task<IActionResult> Search(string searchString)
        {
            var users = from u in _context.Users
                        select u;

            if (!String.IsNullOrEmpty(searchString))
            {
                users = users.Where(s => s.UserName.Contains(searchString) || s.FirstName.Contains(searchString) || s.LastName.Contains(searchString));
            }

            return View(await users.ToListAsync());
        }

    }
}
