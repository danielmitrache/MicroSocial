using MicroSocial.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroSocial.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.Roles.Any())
                {
                    return;
                }

                context.Roles.AddRange(
                    new IdentityRole { Id = "903a112f-1ade-490e-848b-584aeba01c3f", Name = "Moderator", NormalizedName = "Moderator".ToUpper() },
                    new IdentityRole { Id = "a4691662-8c55-4360-932e-3fb302efdf25", Name = "User", NormalizedName = "User".ToUpper() }
                    );

                var hasher = new PasswordHasher<ApplicationUser>();

                context.Users.AddRange(
                    new ApplicationUser
                    {
                        Id = "b820713f-30d5-45a9-ae6a-511be5f1dbc7",
                        UserName = "moderator@test.com",
                        EmailConfirmed = true,
                        NormalizedEmail = "MODERATOR@TEST.COM",
                        Email = "moderator@test.com",
                        NormalizedUserName = "MODERATOR@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "Moderator!")
                    },
                    new ApplicationUser
                    {
                        Id = "33e0c4a9-5c45-487e-a890-7f59533da5b6",
                        UserName = "user@test.com",
                        EmailConfirmed = true,
                        NormalizedEmail = "USER@TEST.COM",
                        Email = "user@test.com",
                        NormalizedUserName = "USER@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "User1!")
                    }
                );

                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        RoleId = "903a112f-1ade-490e-848b-584aeba01c3f",
                        UserId = "b820713f-30d5-45a9-ae6a-511be5f1dbc7"
                    },

                    new IdentityUserRole<string>
                    {
                        RoleId = "a4691662-8c55-4360-932e-3fb302efdf25",
                        UserId = "33e0c4a9-5c45-487e-a890-7f59533da5b6"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
