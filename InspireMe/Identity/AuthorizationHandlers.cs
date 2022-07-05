using InspireMe.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace InspireMe.Identity
{
    public class MeetingConnectionsRequirement : IAuthorizationRequirement
    {
    }
    public class HasMeetingHandler : AuthorizationHandler<MeetingConnectionsRequirement>
    {
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BookingsTable bookingsTable;

        public HasMeetingHandler(IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _connectionFactory = connectionFactory;
            bookingsTable = new BookingsTable(_connectionFactory);
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       MeetingConnectionsRequirement requirement)
        {
            var user = (_userManager.GetUserAsync(context.User)).Result;
            if (!(_userManager.IsInRoleAsync(user, "Supervisor").Result))
            {
                if (bookingsTable.CheckCustomerHasMeetingAsync(user.Id).Result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else if (bookingsTable.CheckSupervisorHasMeetingAsync(user.Id).Result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            bookingsTable.Dispose();
            return Task.CompletedTask;
        }
    }
}
