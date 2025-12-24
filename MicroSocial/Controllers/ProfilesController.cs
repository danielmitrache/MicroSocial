using MicroSocial.Data;
using MicroSocial.Models;
using MicroSocial.Models.ViewModels;
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
            
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                     if (currentUser.Id == user.Id)
                     {
                        ViewBag.IsCurrentUser = true;
                     }
                     else
                     {
                         ViewBag.IsFollowing = user.Followers.Any(f => f.FollowingUserId == currentUser.Id);
                     }
                }
            }

            if (user.IsPrivate == true && !ViewBag.IsCurrentUser && !ViewBag.IsFollowing)
            {
                user.Posts = new List<Post>(); // Hide posts
                ViewBag.IsPrivateProfile = true;
            }

            return View(user);
        }

        // GET: Profiles/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Description = user.Description,
                IsPrivate = user.IsPrivate ?? false,
                ProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        // POST: Profiles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Description = model.Description;
            user.IsPrivate = model.IsPrivate;

            if (model.ProfileImage != null)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string fileName = Path.GetFileNameWithoutExtension(model.ProfileImage.FileName);
                string extension = Path.GetExtension(model.ProfileImage.FileName);
                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                string path = Path.Combine(wwwRootPath + "/images/profiles/", fileName);

                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                user.ProfilePicture = "/images/profiles/" + fileName;
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Show), new { id = user.Id });
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

        // POST: Profiles/Follow/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string id)
        {
            var userToFollow = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (userToFollow == null || currentUser == null)
            {
                return NotFound();
            }

            if (userToFollow.Id == currentUser.Id)
            {
                return BadRequest("You cannot follow yourself.");
            }

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowingUserId == currentUser.Id && f.FollowedUserId == userToFollow.Id);

            if (existingFollow == null)
            {
                var follow = new Follow
                {
                    FollowingUserId = currentUser.Id,
                    FollowedUserId = userToFollow.Id
                };

                _context.Follows.Add(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Show), new { id = id });
        }

        // POST: Profiles/Unfollow/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string id)
        {
            var userToUnfollow = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (userToUnfollow == null || currentUser == null)
            {
                return NotFound();
            }

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowingUserId == currentUser.Id && f.FollowedUserId == userToUnfollow.Id);

            if (existingFollow != null)
            {
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Show), new { id = id });
        }

        public async Task<IActionResult> Followers(string id)
        {
            var user = await _context.Users.Include(u => u.Followers).ThenInclude(f => f.FollowingUser).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            
            ViewData["Title"] = "Followers";
            var followers = user.Followers.Select(f => f.FollowingUser).ToList();
            return View("UserList", followers);
        }

        public async Task<IActionResult> Following(string id)
        {
            var user = await _context.Users.Include(u => u.Following).ThenInclude(f => f.FollowedUser).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            ViewData["Title"] = "Following";
            var following = user.Following.Select(f => f.FollowedUser).ToList();
            return View("UserList", following);
        }
    }
}
