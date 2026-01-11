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
        [Authorize]
        public async Task<IActionResult> Create(GroupMessage message)
        {
            var userId = _userManager.GetUserId(User);
            
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
            if (message.UserId != userId && !User.IsInRole("Administrator")) return Forbid();

            return View(message);
        }

        // POST: GroupMessages/Edit/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(int id, GroupMessage message)
        {
            if (id != message.GroupMessageId) return NotFound();

            var existingMessage = await _context.GroupMessages.AsNoTracking().FirstOrDefaultAsync(m => m.GroupMessageId == id);
            if (existingMessage == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (existingMessage.UserId != userId && !User.IsInRole("Administrator")) return Forbid();

            ModelState.Remove(nameof(message.UserId));
            
            if (ModelState.IsValid)
            {
                // Preserve original fields
                message.UserId = existingMessage.UserId;
                message.SentAt = existingMessage.SentAt; 

                _context.Update(message);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Groups", new { id = message.GroupId });
            }
            return View(message);
        }

        // POST: GroupMessages/Delete/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.GroupMessages.FindAsync(id);
            if (message == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            
            if (message.UserId != userId && !User.IsInRole("Administrator")) return Forbid();

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Groups", new { id = message.GroupId });
        }
    }
}
