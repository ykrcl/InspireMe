using System.Threading.Tasks;
using InspireMe.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
namespace InspireMe.Hubs
{
    [Authorize]
    public class SiteNotificationConnection : Hub
    {
        public readonly IUserConnectionManager userConnectionManager;
        private readonly UserManager<IdentityUser> _userManager;
        public SiteNotificationConnection(IUserConnectionManager _userConnectionManager, UserManager<IdentityUser> userManager)
        {
            userConnectionManager = _userConnectionManager;
            _userManager = userManager;
        }

        public override Task OnConnectedAsync()
        {
            var user = _userManager.GetUserAsync(Context.User).Result;
            userConnectionManager.KeepUserConnection(user.Id, Context.ConnectionId);
            return base.OnConnectedAsync();
        }
    }
}
