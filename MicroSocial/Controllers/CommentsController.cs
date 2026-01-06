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

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                existingComment.Content = comment.Content;
                _context.Update(existingComment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Posts", new { id = existingComment.PostId });
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
