using InspireMe.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using InspireMe.Areas.Meeting.Models;
namespace InspireMe.Areas.Meeting.Controllers
{
    [Area("Client")]
    [Authorize(Policy = "CanConnectMeetingsHub")]
    public class HomeController : Controller
    {
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BookingsTable bookingsTable;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, IStringLocalizer<HomeController> localizer)
        {
            _userManager = userManager;
            _connectionFactory = connectionFactory;
            bookingsTable = new BookingsTable(_connectionFactory);
            _localizer = localizer;
        }
        public async Task<IActionResult> Meet(string Id)
        {
            Guid meetingid = Guid.Parse(Id);
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var meeting = await bookingsTable.FindBookingByIdAsync(meetingid);
            if (meeting != null)
            {
                if (!(meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour)&&meeting.IsEnded)
                {
                    return RedirectToAction("Index", "Accounts", new { area = "default" });
                }

            
            if (!(await _userManager.IsInRoleAsync(user, "Supervisor")))
            {
                if (!(meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour) && !meeting.IsStarted)
                {
                        return RedirectToAction("Index", "Accounts", new { area = "default" });
                }
            }
            }
            else
            {
                return RedirectToAction("Index", "Accounts", new { area = "default" });
            }
            var model = new MeetingParametersViewModel();
            model.Date = meeting.Date;
            model.MeetingId = meeting.Id.ToString();
            model.Hour = meeting.Hour;
            if(await _userManager.IsInRoleAsync(user, "Supervisor"))
            {
                model.UserName = (await bookingsTable.FindBookingByIdBindCustomerAsync(meetingid)).Customer.UserName;
            }
            else
            {
                model.UserName = (await bookingsTable.FindBookingByIdBindSupervisorAsync(meetingid)).Supervisor.UserName;
            }


            return View("Meet");
        }
    }
}
