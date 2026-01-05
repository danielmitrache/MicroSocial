using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{
    [Authorize]
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Follows/Follow/5
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
                    FollowedUserId = userToFollow.Id,
                    Status = !userToFollow.IsPrivate.GetValueOrDefault() // True if public, False if private
                };

                _context.Follows.Add(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Show", "Profiles", new { id = id });
        }

        // POST: Follows/Unfollow/5
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

            return RedirectToAction("Show", "Profiles", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowingUserId == followerId && f.FollowedUserId == currentUser.Id);

            if (follow != null)
            {
                follow.Status = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Show", "Profiles", new { id = currentUser.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowingUserId == followerId && f.FollowedUserId == currentUser.Id);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Show", "Profiles", new { id = currentUser.Id });
        }
    }
}
