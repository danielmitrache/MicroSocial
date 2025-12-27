using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Posts
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var postsQuery = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                postsQuery = postsQuery.Where(s => s.Content.Contains(searchString) || s.User.UserName.Contains(searchString));
            }

            return View(await postsQuery.ToListAsync());
        }

        // GET: Posts/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Content,MediaType")] Post post, IFormFile? mediaFile)
        {
            // Remove properties we set manually or don't need from validation
            ModelState.Remove("MediaPath");
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Likes");
            ModelState.Remove("Comments");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                post.UserId = user.Id;
                post.CreatedAt = DateTime.UtcNow;
                
                if (mediaFile != null && mediaFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mediaFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await mediaFile.CopyToAsync(fileStream);
                    }

                    post.MediaPath = "/uploads/" + uniqueFileName;

                    var ext = Path.GetExtension(mediaFile.FileName).ToLower();
                    if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
                    {
                        post.MediaType = MediaType.Image;
                    }
                    else if (new[] { ".mp4", ".webm", ".ogg" }.Contains(ext))
                    {
                        post.MediaType = MediaType.Video;
                    }
                    else
                    {
                        post.MediaType = MediaType.None;
                    }
                }
                else
                {
                    post.MediaType = MediaType.None;
                    post.MediaPath = "";
                }

                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            return View(post);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Content,MediaType")] Post post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }

            var existingPost = await _context.Posts.FindAsync(id);
            if (existingPost == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (existingPost.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            ModelState.Remove("MediaPath");
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Likes");
            ModelState.Remove("Comments");

            if (ModelState.IsValid)
            {
                try
                {
                    existingPost.Content = post.Content;
                    
                    _context.Update(existingPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }
    }
}
