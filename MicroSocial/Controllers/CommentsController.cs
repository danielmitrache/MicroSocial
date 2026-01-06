using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MicroSocial.Services.IContentModerationService _moderationService;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, MicroSocial.Services.IContentModerationService moderationService)
        {
            _context = context;
            _userManager = userManager;
            _moderationService = moderationService;
        }

        // POST: Comments/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            // Content Moderation
            var moderation = await _moderationService.CheckContentAsync(content);
            if (moderation.Success && !moderation.IsSafe)
            {
                TempData["Error"] = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                 return RedirectToAction("Details", "Posts", new { id = postId }); // Redirecting to Details since AddComment is likely called from Details View directly (no specific AddComment view typically)
                 // Alternatively, if there was a separate view, we would return it.
                 // Given the snippet, it redirects to Details. So using TempData is best.
            }

            var user = await _userManager.GetUserAsync(User);
            var comment = new Comment
            {
                PostId = postId,
                UserId = user.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (comment.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            return View(comment);
        }

        // POST: Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,Content")] Comment comment)
        {
            if (id != comment.CommentId)
            {
                return NotFound();
            }

            var existingComment = await _context.Comments.FindAsync(id);
            if (existingComment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (existingComment.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(comment.Content))
            {
            if (!string.IsNullOrWhiteSpace(comment.Content))
            {
                 // Content Moderation
                var moderation = await _moderationService.CheckContentAsync(comment.Content);
                if (moderation.Success && !moderation.IsSafe)
                {
                    ModelState.AddModelError("Content", "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.");
                    return View(comment);
                }

                existingComment.Content = comment.Content;
                _context.Update(existingComment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Posts", new { id = existingComment.PostId });
            }
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (comment.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            var postId = comment.PostId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Posts", new { id = postId });
        }
    }
}
