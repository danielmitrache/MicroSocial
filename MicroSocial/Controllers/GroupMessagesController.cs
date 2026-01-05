using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{
    public class GroupMessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupMessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: GroupMessages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("GroupId,Content")] GroupMessage message)
        {
            var userId = _userManager.GetUserId(User);
            
            // Verify membership
            var isMember = await _context.UserGroups
                .AnyAsync(ug => ug.GroupId == message.GroupId && ug.UserId == userId && ug.Status == true);

            if (!isMember) return Forbid();

            ModelState.Remove(nameof(message.UserId));
            
            if (ModelState.IsValid)
            {
                message.UserId = userId;
                message.SentAt = DateTime.Now;
                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Groups", new { id = message.GroupId });
            }
            
            // If invalid, redirect back to group details with error? 
            // Ideally we'd show the error. For now, redirect.
            return RedirectToAction("Details", "Groups", new { id = message.GroupId });
        }

        // GET: GroupMessages/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.GroupMessages.FindAsync(id);
            if (message == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (message.UserId != userId) return Forbid();

            return View(message);
        }

        // POST: GroupMessages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("GroupMessageId,GroupId,Content")] GroupMessage message)
        {
            if (id != message.GroupMessageId) return NotFound();

            var existingMessage = await _context.GroupMessages.AsNoTracking().FirstOrDefaultAsync(m => m.GroupMessageId == id);
            if (existingMessage == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (existingMessage.UserId != userId) return Forbid();

            ModelState.Remove(nameof(message.UserId));
            
            if (ModelState.IsValid)
            {
                // Preserve original fields
                message.UserId = userId;
                message.SentAt = existingMessage.SentAt; // Keep original time? Or update? usually keep.

                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.GroupMessages.Any(e => e.GroupMessageId == message.GroupMessageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Groups", new { id = message.GroupId });
            }
            return View(message);
        }

        // POST: GroupMessages/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.GroupMessages.FindAsync(id);
            if (message == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // Allow author OR moderator to delete? Requirement says: "Utilizatorii pot... edita sau sterge propriile mesaje"
            // "Moderatorul poate sterge grupurile". Doesn't explicitly say moderator can delete messages, but usually yes.
            // Let's stick to "Current user can delete their own messages" for now as per explicit requirement.
            
            if (message.UserId != userId) return Forbid();

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Groups", new { id = message.GroupId });
        }
    }
}
