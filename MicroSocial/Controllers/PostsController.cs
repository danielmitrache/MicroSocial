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
        private readonly MicroSocial.Services.IContentModerationService _moderationService;

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, MicroSocial.Services.IContentModerationService moderationService)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _moderationService = moderationService;
        }

        // GET: Posts
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var currentUserId = _userManager.GetUserId(User);
            List<string> followingIds = new List<string>();

            if (currentUserId != null)
            {
                followingIds = await _context.Follows
                    .Where(f => f.FollowingUserId == currentUserId && f.Status == true)
                    .Select(f => f.FollowedUserId)
                    .ToListAsync();
            }

            ViewBag.FollowingIds = followingIds;

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

            // Client-side filtering for complex logic if EF Core fails to translate, 
            // OR simple where clause if possible. 
            // Logic: Include if (IsPublic) OR (IsPrivate AND Followed) OR (IsMe)
            
            // Note: p.User.IsPrivate might be null, so check != true.
            // EF Core should translate followingIds.Contains correctly.
            
            if (currentUserId != null)
            {
                  postsQuery = postsQuery.Where(p => 
                    p.UserId == currentUserId || 
                    (p.User.IsPrivate != true) || 
                    (p.User.IsPrivate == true && followingIds.Contains(p.UserId))
                );
            }
            else
            {
                // Anonymous users only see public posts
                postsQuery = postsQuery.Where(p => p.User.IsPrivate != true);
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
            ModelState.Remove(nameof(post.UserId));

            ModelState.Remove(nameof(post.UserId));

            if (ModelState.IsValid)
            {
                // Content Moderation
                var moderation = await _moderationService.CheckContentAsync(post.Content);
                if (moderation.Success && !moderation.IsSafe)
                {
                    ModelState.AddModelError("Content", "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.");
                    return View(post);
                }

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
        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Content,MediaType")] Post post, IFormFile? mediaFile, bool removeMedia = false)
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

            ModelState.Remove(nameof(post.UserId));

            ModelState.Remove(nameof(post.UserId));

            if (ModelState.IsValid)
            {
                try
                {
                    // Content Moderation
                    var moderation = await _moderationService.CheckContentAsync(post.Content);
                    if (moderation.Success && !moderation.IsSafe)
                    {
                        ModelState.AddModelError("Content", "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.");
                         return View(post);
                    }

                    existingPost.Content = post.Content;

                    // Handle Media Removal
                    if (removeMedia)
                    {
                        // Optionally delete file from disk here if desired
                        existingPost.MediaPath = "";
                        existingPost.MediaType = MediaType.None;
                    }

                    // Handle New Media Upload
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

                        existingPost.MediaPath = "/uploads/" + uniqueFileName;

                        var ext = Path.GetExtension(mediaFile.FileName).ToLower();
                        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
                        {
                            existingPost.MediaType = MediaType.Image;
                        }
                        else if (new[] { ".mp4", ".webm", ".ogg" }.Contains(ext))
                        {
                            existingPost.MediaType = MediaType.Video;
                        }
                        else
                        {
                            existingPost.MediaType = MediaType.None;
                        }
                    }
                    
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
