using InspireMe.Data;
using InspireMe.Models;
using InspireMe.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace InspireMe.Areas.Client.Controllers
{
    [Area("Client")]
    public class BookingsController : Controller
    {
        private readonly ILogger<BookingsController> _logger;
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserClaimsTable<string, IdentityUserClaim<string>> _userClaimsTable;
        private readonly IStringLocalizer<BookingsController> _localizer;
        private readonly AvailableDatesTable availableDatesTable;
        private readonly BookingsTable bookingsTable;
        public BookingsController(ILogger<BookingsController> logger, IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, IStringLocalizer<BookingsController> localizer, IUserClaimsTable<string, IdentityUserClaim<string>> userClaimsTable)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _userManager = userManager;
            _userClaimsTable = userClaimsTable;
            availableDatesTable = new AvailableDatesTable(_connectionFactory);
            bookingsTable = new BookingsTable(_connectionFactory);
        }
        public async Task<IActionResult> Index()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjax)
                return PartialView();
            else
                return View();
        }
            public async Task<IActionResult> FindaSupervisor()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var claims= (await _userManager.GetClaimsAsync(user)).Where(x=>x.Type=="field").ToList();
            if (claims.Count == 0)
            {
                var lastbookingsupervisor = (await bookingsTable.GetCustomerBookingsAsync(user.Id)).OrderBy(x => x.Id).Last().Supervisor;
                if (lastbookingsupervisor != null)
                {
                    claims = (await _userManager.GetClaimsAsync(lastbookingsupervisor)).Where(x => x.Type == "field").ToList();
                }
            }
           
                List<Tuple<IList<IdentityUser>, Claim>> supervisors = new List<Tuple<IList<IdentityUser>, Claim>>();
                foreach(var claim in claims)
                {
                    supervisors.Add(new Tuple<IList<IdentityUser>, Claim>(await _userManager.GetUsersForClaimAsync(claim),claim));
                }
                 ViewBag.Supervisors = supervisors;
            var allfields = await _userClaimsTable.GetClaimValuesByTypeAsync("field");
            ViewBag.allfields = allfields;
            if (isAjax)
                return PartialView();
            else
                return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindaSupervisor(SearchSupervisorViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var fields = obj.fields.Split(',').Select(p => p.Trim()).ToList();
            List<Tuple<IList<IdentityUser>, Claim>> supervisors = new List<Tuple<IList<IdentityUser>, Claim>>();
            foreach (var field in fields)
            {
                var claim = new Claim("field", field);
                supervisors.Add(new Tuple<IList<IdentityUser>, Claim>(await _userManager.GetUsersForClaimAsync(claim), claim));
            }
            ViewBag.Supervisors = supervisors;
            var allfields = await _userClaimsTable.GetClaimValuesByTypeAsync("field");
            ViewBag.allfields = allfields;
            if (isAjax)
                return PartialView();
            else
                return View();
        }
    }
}
