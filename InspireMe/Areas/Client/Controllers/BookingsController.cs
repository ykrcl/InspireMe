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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookaMeeting(string id, BookaMeetingViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid) {
                IdentityUser supervisor;
                try
                {
                    supervisor = await _userManager.FindByIdAsync(obj.UserId);
                    var user = await _userManager.GetUserAsync(HttpContext.User);
                    if (await bookingsTable.CheckAvailabilityExistsAsync(obj.UserId,user.Id , obj.Date, obj.Hour) && await availableDatesTable.CheckAvailabilityExistsAsync(obj.UserId,((int)obj.Date.DayOfWeek),obj.Hour)){
                        Booking booking = new Booking();
                        
                        booking.Hour = obj.Hour;
                        booking.Date = obj.Date;
                        booking.IsEnded = false;
                        booking.IsStarted = false;
                        booking.IsVerified = false;
                        await bookingsTable.CreateAsync(booking, user.Id, supervisor.Id);
                        if (isAjax)
                        {
                            return Json(new { success = true, redirect=Url.Action(""), alert = _localizer["Kayıt Alındı. Danışman Onaylayınca Bildirim Gönderilecektir."].Value });
                        }
                        else
                        {
                            ViewBag.message = _localizer["Kayıt Alındı. Danışman Onaylayınca Bildirim Gönderilecektir."].Value;
                            ViewBag.title = _localizer["Görüşme Ayarla"].Value;
                            return View("Message");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Date", _localizer["Seçilen Tarih ve Saat Uygun Değil!!!"]);
                    }
                }
                catch
                {
                    ModelState.AddModelError("UserId", _localizer["Danışman bulunamadı"]);
                }
                
            }
            var fullhours = await bookingsTable.GetOccupiedHoursAsync(obj.UserId);
            var availablehours = await availableDatesTable.GetUserAvailableDatesAsync(obj.UserId);
            ViewBag.fullhours = fullhours;
            ViewBag.availablehours = availablehours;
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookaMeeting(string id)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            try { 
            var fullhours = (await bookingsTable.GetOccupiedHoursAsync(id)).GroupBy(x=>x.Date).ToDictionary(x=>x.Key.ToString("dd_MM_yyyy"), x=>x.Select(f=>f.Hour).ToList());
            var availablehours = (await availableDatesTable.GetUserAvailableDatesAsync(id)).GroupBy(x=>x.Day).ToDictionary(x=>x.Key.ToString(), x=>x.Select(f=> new {
                hour = f.Hour,
                price = f.Price
            }));
            ViewBag.fullhours = fullhours;
            ViewBag.availablehours = availablehours;
            var user = await _userManager.FindByIdAsync(id);
            BookaMeetingViewModel model = new BookaMeetingViewModel();
            model.UserId = id;
            model.UserName = user.UserName;
            if (isAjax)
                return PartialView(model);
            else
                return View(model);
            }
            catch
            {
                return RedirectToAction("Index", "Bookings", new { area = "Client" });
            }
            
        }
    }
}
