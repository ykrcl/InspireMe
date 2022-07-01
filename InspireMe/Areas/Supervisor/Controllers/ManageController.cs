using InspireMe.Data;
using InspireMe.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using InspireMe.Models;
using Microsoft.AspNetCore.Authorization;

namespace InspireMe.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    [Authorize]
    public class ManageController : Controller
    {
        private readonly ILogger<ManageController> _logger;
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserClaimsTable<string, IdentityUserClaim<string>> _userClaimsTable;
        private readonly IStringLocalizer<ManageController> _localizer;
        private readonly AvailableDatesTable availableDatesTable;
        private readonly BookingsTable bookingsTable;
        public ManageController(ILogger<ManageController> logger, IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, IStringLocalizer<ManageController> localizer, IUserClaimsTable<string, IdentityUserClaim<string>> userClaimsTable)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _userManager = userManager;
            _userClaimsTable = userClaimsTable;
            availableDatesTable = new AvailableDatesTable(_connectionFactory);
            bookingsTable = new BookingsTable(_connectionFactory);
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> ListAvailableDates()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var availablehours = (await availableDatesTable.GetUserAvailableDatesAsync(user.Id)).OrderBy(x => x.Day).ThenBy(x => x.Hour);
            if (isAjax)
                return PartialView(availablehours);
            else
                return View(availablehours);
        }

        public async Task<IActionResult> ListRequestedBookings()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var bookings = (await bookingsTable.GetSupervisorBookingsAsync(user.Id)).Where(x=>x.IsVerified==false).OrderBy(x => x.Id);
            if (isAjax)
                return PartialView(bookings);
            else
                return View(bookings);
        }
        public async Task<IActionResult> VerifyRequestedBooking(Guid id)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var obj = new VerifyBookingViewModel();
            obj.Id = id;
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRequestedBooking(VerifyBookingViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (ModelState.IsValid)
            {
                try
                {
                    var booking = await bookingsTable.VerifyBooking(obj.Id,user.Id);
                    if (booking) {
                        if (isAjax)
                        {
                            return Json(new { success = true, redirect = Url.Action("ListRequestedBookings", "Manage", new { area = "Supervisor" }), alert = _localizer["Başarıyla Onaylandı."].Value });
                        }
                        else
                        {
                            ViewBag.message = _localizer["Başarıyla Onaylandı."].Value;
                            ViewBag.title = _localizer["Toplantı Onayla"].Value;
                            return View("Message");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", _localizer["Yanlış obje id'si."]);
                    }
                }
                catch
                {
                    ModelState.AddModelError("", _localizer["Toplantı bulunamadı."]);
                }
            }

            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        public async Task<IActionResult> DeleteRequestedBooking(Guid id)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var obj = new VerifyBookingViewModel();
            obj.Id = id;
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequestedBooking(DeleteBookingViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (ModelState.IsValid)
            {
                try
                {
                    var booking = await bookingsTable.DeleteAsync(obj.Id, user.Id);
                    if (booking)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = true, redirect = Url.Action("ListRequestedBookings", "Manage", new { area = "Supervisor" }), alert = _localizer["Başarıyla Silindi."].Value });
                        }
                        else
                        {
                            ViewBag.message = _localizer["Başarıyla Silindi"].Value;
                            ViewBag.title = _localizer["Toplantı Sil"].Value;
                            return View("Message");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", _localizer["Yanlış obje id'si."]);
                    }
                }
                catch
                {
                    ModelState.AddModelError("", _localizer["Toplantı bulunamadı."]);
                }
            }

            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
        public async Task<IActionResult> CreateAvailableDates()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return PartialView();
            else
                return View();
        }
        public async Task<IActionResult> DeleteAvailableDate(int id)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var obj = new DeleteAvailableDayViewModel() { Id = id };
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailableDate(DeleteAvailableDayViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (ModelState.IsValid)
            {
                try
                {
                    var availableday = await availableDatesTable.FindByIdAsync(obj.Id);
                    if (availableday.UserId == user.Id){
                        await availableDatesTable.DeleteAsync(availableday.Id);
                        if (isAjax)
                        {
                            return Json(new { success = true, redirect = Url.Action("ListAvailableDates", "Manage", new {area="Supervisor"}), alert = _localizer["Başarıyla Silindi."].Value });
                        }
                        else
                        {
                            ViewBag.message = _localizer["Başarıyla Silindi"].Value;
                            ViewBag.title = _localizer["Saatleri Ayarla"].Value;
                            return View("Message");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", _localizer["Yanlış saat id'si."]);
                    }
                }
                catch
                {
                    ModelState.AddModelError("", _localizer["Saat bulunamadı."]);
                }
            }

            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAvailableDates(CreateAvailableDayViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid) { 
            var user = await _userManager.GetUserAsync(HttpContext.User);
            foreach (var hour in obj.Hours)
            {
                if(hour>=0 && hour<24)
                {
                    if (!(await availableDatesTable.CheckAvailabilityExistsAsync(user.Id, obj.Day, hour))) { 
                
                var availableday = new AvailableDate();
                availableday.Day = obj.Day;
                availableday.Hour = hour;
                availableday.IsAvailable = true;
                availableday.Price = obj.Price;
                availableday.UserId = user.Id;
                await availableDatesTable.CreateAsync(availableday);
                            if (isAjax)
                            {
                                return Json(new { success = true, redirect = Url.Action("ListAvailableDates", "Manage", new { area = "Supervisor" }), alert = _localizer["Başarıyla Kaydedildi."].Value });
                            }
                            else
                            {
                                ViewBag.message = _localizer["Başarıyla Kaydedildi"].Value;
                                ViewBag.title = _localizer["Saatleri Ayarla"].Value;
                                return View("Message");
                            } 
                        }
                    else
                    {
                        ModelState.AddModelError("", _localizer["Bazı tarih ve saatler zaten mevcut. Önce silmeyi deneyin."]);
                    }
                }
                else
                {
                    ModelState.AddModelError("Hours", _localizer["Saat 0 ile 24 arasında olmalıdır."]);
                }
            }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
    }
}
