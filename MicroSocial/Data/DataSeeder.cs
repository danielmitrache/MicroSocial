using MicroSocial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Data
{
    public static class DataSeeder
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure roles exist
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Seed Roles
                string[] roles = new string[] { "Administrator", "Editor", "User" };
                foreach (string role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Seed Users
                var users = new (string Email, string Role, string FirstName, string LastName)[]
                {
                    ("admin@test.com", "Administrator", "Admin", "User"),
                    ("editor@test.com", "Editor", "Editor", "User"),
                    ("user@test.com", "User", "Normal", "User")
                };

                foreach (var userData in users)
                {
                    // Check if user exists
                    if (await userManager.FindByEmailAsync(userData.Email) == null)
                    {
                        var user = new ApplicationUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        EmailConfirmed = true
                    };

                        var result = await userManager.CreateAsync(user, "Password1!");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, userData.Role);
                        }
                    }
                }

                // Need to save changes to get UserIds generated/persisted if needed for relationships, 
                // but UserManager creates them immediately. 
                // We need to fetch them back to get their Ids for Posts.
                
                var adminUser = await userManager.FindByEmailAsync("admin@test.com");
                var editorUser = await userManager.FindByEmailAsync("editor@test.com");
                var normalUser = await userManager.FindByEmailAsync("user@test.com");

                // Seed Posts
                if (!context.Posts.Any())
                {
                    var posts = new Post[]
                {
                    new Post
                    {
                        UserId = adminUser.Id,
                        Content = "Welcome to MicroSocial! This is the first official post.",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        MediaType = MediaType.None,
                        MediaPath = ""
                    },
                    new Post
                    {
                        UserId = editorUser.Id,
                        Content = "Just testing out the platform. Looks great!",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        MediaType = MediaType.None,
                        MediaPath = ""
                    },
                    new Post
                    {
                        UserId = normalUser.Id,
                        Content = "Hello everyone! Happy to be here.",
                        CreatedAt = DateTime.UtcNow.AddHours(-5),
                        MediaType = MediaType.None,
                        MediaPath = ""
                    }
                };

                context.Posts.AddRange(posts);
                await context.SaveChangesAsync();

                // Seed Comments
                var comments = new Comment[]
                {
                    new Comment
                    {
                        PostId = posts[0].PostId,
                        UserId = normalUser.Id,
                        Content = "Thank you! Excited to join.",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new Comment
                    {
                        PostId = posts[1].PostId,
                        UserId = adminUser.Id,
                        Content = "Glad you like it!",
                        CreatedAt = DateTime.UtcNow.AddHours(-10)
                    }
                };

                    context.Comments.AddRange(comments);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
