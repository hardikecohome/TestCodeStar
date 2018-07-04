using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Domain;
using Microsoft.AspNet.Identity;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface IUsersService
    {
        IList<Claim> GetUserClaims(ApplicationUser user);
        Task<IList<Alert>> SyncAspireUser(ApplicationUser user, UserManager<ApplicationUser> userManager);

        string GetUserPassword(string userId);
        void UpdateUserPassword(string userId, string newPassword);
    }
}