using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MicroSocial.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.Moderator)
                .ToListAsync();
            return View(groups);
        }

        // GET: Groups/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Name,Description")] Group group)
        {
            ModelState.Remove(nameof(group.ModeratorId));
            ModelState.Remove(nameof(group.Moderator));
            
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                group.ModeratorId = userId;
                
                _context.Add(group);
                await _context.SaveChangesAsync();

                // Add moderator as a member immediately
                var userGroup = new UserGroup
                {
                    UserId = userId,
                    GroupId = group.GroupId,
                    Status = true // Automatically approved
                };
                _context.Add(userGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }

        // GET: Groups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.Moderator)
                .Include(g => g.GroupMessages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.User)
                .Include(g => g.UserGroups)
                    .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(m => m.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            ViewBag.CurrentUserId = userId;
            
            var userGroup = group.UserGroups.FirstOrDefault(ug => ug.UserId == userId);
            
            ViewBag.IsMember = userGroup != null && userGroup.Status;
            ViewBag.HasPendingRequest = userGroup != null && !userGroup.Status;
            ViewBag.IsModerator = group.ModeratorId == userId;

            return View(group);
        }

        // POST: Groups/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Join(int id)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups.FindAsync(id);
            
            if (group == null) return NotFound();

            var existingLink = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == id);

            if (existingLink == null)
            {
                var userGroup = new UserGroup
                {
                    UserId = userId,
                    GroupId = id,
                    Status = false // Pending approval
                };
                _context.Add(userGroup);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Request sent successfully!";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Groups/Leave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Leave(int id)
        {
            var userId = _userManager.GetUserId(User);
            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == id);

            if (userGroup != null)
            {
                // Can't leave if you are the moderator? Or does it delete the group?
                // Logic: Moderator can leave only if they delete the group (handled in Delete) or verify logic.
                // For now, if moderator tries to leave, we might block it or warn.
                // Requirement says: "Moderator can delete groups they created". 
                // "User can leave group".
                // If moderator leaves, group is orphan. Let's assume moderator should use Delete.
                
                var group = await _context.Groups.FindAsync(id);
                if (group.ModeratorId == userId)
                {
                    TempData["Error"] = "Moderators cannot leave the group. Delete the group instead.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _context.UserGroups.Remove(userGroup);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Groups/AcceptRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AcceptRequest(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null || group.ModeratorId != currentUserId)
            {
                 return Forbid();
            }

            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
            
            if (userGroup != null)
            {
                userGroup.Status = true;
                _context.Update(userGroup);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Groups/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RejectRequest(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null || group.ModeratorId != currentUserId)
            {
                return Forbid();
            }

            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup != null)
            {
                _context.UserGroups.Remove(userGroup);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            var userId = _userManager.GetUserId(User);

            if (group == null) return NotFound();
            
            if (group.ModeratorId != userId && !User.IsInRole("Admin")) // Assuming Admin exists or just moderator
            {
                return Forbid();
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
