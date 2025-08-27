using Microsoft.AspNetCore.Identity;
using StoriArendaPro.Models.Entities;

namespace StoriArendaPro.Areas.Admin.Extensions
{
    public static class UserExtensions
    {
        public static async Task<bool> IsInRoleAsync(this User user, UserManager<User> userManager, string role)
        {
            return await userManager.IsInRoleAsync(user, role);
        }

        public static async Task<List<string>> GetRolesListAsync(this User user, UserManager<User> userManager)
        {
            return (await userManager.GetRolesAsync(user)).ToList();
        }

        public static async Task<string> GetPrimaryRoleAsync(this User user, UserManager<User> userManager)
        {
            var roles = await userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "User";
        }

        public static bool HasPassportVerified(this User user)
        {
            return !string.IsNullOrEmpty(user.PassportSeria) &&
                   !string.IsNullOrEmpty(user.PassportNumber) &&
                   !string.IsNullOrEmpty(user.Propiska);
        }
    }
}
