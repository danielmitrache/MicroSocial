using MicroSocial.Data;
using MicroSocial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Controllers
{

    [Authorize] 
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CleanupDuplicates()
        {
            var posts = await _context.Posts.ToListAsync();

            var duplicates = posts
                .GroupBy(p => new { p.UserId, p.Content, p.CreatedAt })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1)) // Keep the first (or random) one, remove others
                .ToList();

            if (duplicates.Any())
            {
                _context.Posts.RemoveRange(duplicates);
                await _context.SaveChangesAsync();
            }

            return Content($"Removed {duplicates.Count} duplicate posts. returning to home...");
        }
    }
}
