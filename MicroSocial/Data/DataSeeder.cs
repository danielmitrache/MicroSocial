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
                string[] roles = new string[] { "Administrator", "User" };
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
                    ("alice@test.com", "User", "Alice", "Alice"),
                    ("bob@test.com", "User", "Bob", "Bob"),
                    ("alex@test.com", "User", "Alex", "Popescu"),
                    ("mihai@test.com", "User", "Mihai", "Razvan")
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

                
                var adminUser = await userManager.FindByEmailAsync("admin@test.com");
                var aliceUser = await userManager.FindByEmailAsync("alice@test.com");
                var bobUser = await userManager.FindByEmailAsync("bob@test.com");
                var alexUser = await userManager.FindByEmailAsync("alex@test.com");
                var mihaiUser = await userManager.FindByEmailAsync("mihai@test.com");


                // Seed Posts
                if (!context.Posts.Any())
                {
                    var posts = new Post[]
                    {
                        new Post
                        {
                            UserId = adminUser.Id,
                            Content = "Bun venit la platforma MicroSocial!",
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            MediaType = MediaType.None,
                            MediaPath = ""
                        },
                        new Post
                        {
                            UserId = bobUser.Id,
                            Content = "Testez platforma!\nPostare noua!",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            MediaType = MediaType.None,
                            MediaPath = ""
                        },
                        new Post
                        {
                            UserId = alexUser.Id,
                            Content = "Testez upload-ul de poze:",
                            CreatedAt = DateTime.UtcNow.AddHours(-5),
                            MediaType = MediaType.None, // Fixed based on request, or should use media? User said "2 posts... sa le introduce". 
                            // Wait, previous code had MediaType.None. New code should use the files.
                            MediaPath = ""
                        },
                         new Post
                        {
                            UserId = mihaiUser.Id,
                            Content = "O poza de test aici!",
                            CreatedAt = DateTime.UtcNow.AddHours(-3),
                            MediaType = MediaType.Image,
                            MediaPath = "/uploads/test_poza.png"
                        },
                        new Post
                        {
                            UserId = alexUser.Id,
                            Content = "Si un video de test!",
                            CreatedAt = DateTime.UtcNow.AddHours(-1),
                            MediaType = MediaType.Video,
                            MediaPath = "/uploads/test_video.mp4"
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
                            UserId = mihaiUser.Id, 
                            Content = "Ma bucur ca sunt aici!",
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new Comment
                        {
                            PostId = posts[1].PostId,
                            UserId = adminUser.Id,
                            Content = "Bun venit!",
                            CreatedAt = DateTime.UtcNow.AddHours(-10)
                        }
                    };

                    context.Comments.AddRange(comments);
                    await context.SaveChangesAsync();
                }

                // Seed Groups
                if (!context.Groups.Any())
                {
                    var groups = new Group[]
                    {
                        new Group
                        {
                            Name = "Dezvoltatori Web",
                            Description = "Grup pentru pasionatii de ASP.NET Core si nu numai.",
                            ModeratorId = adminUser.Id
                        },
                        new Group
                        {
                            Name = "Fotografie",
                            Description = "Discutii despre fotografie, echipamente si editare.",
                            ModeratorId = alexUser.Id
                        },
                        new Group
                        {
                            Name = "Gaming",
                            Description = "Totul despre jocuri video.",
                            ModeratorId = bobUser.Id
                        }
                    };
                    context.Groups.AddRange(groups);
                    await context.SaveChangesAsync();

                    // Add Moderators as members
                    foreach(var group in groups)
                    {
                        context.UserGroups.Add(new UserGroup
                        {
                            GroupId = group.GroupId,
                            UserId = group.ModeratorId,
                            Status = true
                        });
                    }

                    // Add other members
                    var otherMembers = new UserGroup[]
                    {
                        // Web Dev (Admin mod) - Add Bob and Alex
                        new UserGroup { GroupId = groups[0].GroupId, UserId = bobUser.Id, Status = true },
                        new UserGroup { GroupId = groups[0].GroupId, UserId = alexUser.Id, Status = true },

                        // Photography (Alex mod) - Add Mihai and Alice
                        new UserGroup { GroupId = groups[1].GroupId, UserId = mihaiUser.Id, Status = true },
                        new UserGroup { GroupId = groups[1].GroupId, UserId = aliceUser.Id, Status = true },

                        // Gaming (Bob mod) - Add Alex and Mihai
                        new UserGroup { GroupId = groups[2].GroupId, UserId = alexUser.Id, Status = true },
                        new UserGroup { GroupId = groups[2].GroupId, UserId = mihaiUser.Id, Status = true }
                    };
                    context.UserGroups.AddRange(otherMembers);
                    
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
