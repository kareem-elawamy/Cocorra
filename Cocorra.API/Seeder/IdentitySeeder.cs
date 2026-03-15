using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace Cocorra.API.Seeder
{
    public class IdentitySeeder
    {
        public static async Task SeedAsync(
      UserManager<ApplicationUser> userManager,
      RoleManager<IdentityRole<Guid>> roleManager,
      IConfiguration configuration)
        {
            string adminEmail = configuration["SeedAdmin:Email"]!;
            string adminPassword = configuration["SeedAdmin:Password"]!;

            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Status = UserStatus.Active
                };

                await userManager.CreateAsync(user, adminPassword);
            }

            await userManager.AddToRolesAsync(user, new[] { "Admin", "Coach" });
        }
    }
}