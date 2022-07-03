using InspireMe.Data;
using InspireMe.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using InspireMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using InspireMe.Hubs;
using FluentEmail.Core;

namespace InspireMe.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    
    [Authorize(Roles ="Supervisor")]
    public class ManageController : Controller
    {
        private readonly ILogger<ManageController> _logger;
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserClaimsTable<string, IdentityUserClaim<string>> _userClaimsTable;
        private readonly IStringLocalizer<ManageController> _localizer;
        private readonly AvailableDatesTable availableDatesTable;
        private readonly BookingsTable bookingsTable;
        private readonly IHubContext<SiteNotificationConnection> _NotificationhubContext;
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly IFluentEmailFactory _emailFactory;
        public ManageController(ILogger<ManageController> logger, IFluentEmailFactory emailFactory, IUserConnectionManager userConnectionManager, IDatabaseConnectionFactory connectionFactory, IHubContext<SiteNotificationConnection> NotificationhubContext, UserManager<IdentityUser> userManager, IStringLocalizer<ManageController> localizer, IUserClaimsTable<string, IdentityUserClaim<string>> userClaimsTable)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _userManager = userManager;
            _NotificationhubContext = NotificationhubContext;
            _userClaimsTable = userClaimsTable;
            availableDatesTable = new AvailableDatesTable(_connectionFactory);
            bookingsTable = new BookingsTable(_connectionFactory);
            _userConnectionManager = userConnectionManager;
            _emailFactory = emailFactory;
        }
        public IActionResult Index()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjax)
                return PartialView();
            else
                return View();
        }

        public async Task<IActionResult> ListMeetings()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var bookings = (await bookingsTable.GetCustomerBookingsAsync(user.Id)).OrderBy(x => x.Date).ThenBy(x => x.Hour).ToList();
            if (isAjax)
                return PartialView(bookings);
            else
                return View(bookings);
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
                        try { 
                            var notifuser = (await bookingsTable.FindBookingByIdBindCustomerAsync(obj.Id)).Customer;
                            var connections = _userConnectionManager.GetUserConnections(notifuser.Id);
                            foreach (var connection in connections)
                            {
                                await _NotificationhubContext.Clients.Client(connection).SendAsync("ShowNotification", user.UserName + ": " + _localizer["Toplantı İsteğiniz Onaylandı."].Value);
                            }
                            var email1 = _emailFactory
                        .Create()
                        .To(notifuser.Email)
                        .Subject(_localizer["Toplantı Doğrulandı"].Value)
                        .Body(user.UserName +" "+ _localizer["Toplantı isteğinizi onayladı"].Value );

                             email1.SendAsync();
                        }
                        catch { }
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
                    var notifuser = (await bookingsTable.FindBookingByIdBindCustomerAsync(obj.Id)).Customer;
                    var booking = await bookingsTable.DeleteAsync(obj.Id, user.Id);
                    if (booking)
                    {
                        try
                        {
                            var connections = _userConnectionManager.GetUserConnections(notifuser.Id);
                            foreach (var connection in connections)
                            {
                                await _NotificationhubContext.Clients.Client(connection).SendAsync("ShowNotification", user.UserName + ": " + _localizer["Toplantı İsteğiniz Reddedildi."].Value);
                            }
                            var email1 = _emailFactory
                        .Create()
                        .To(notifuser.Email)
                        .Subject(_localizer["Toplantı Reddedildi"].Value)
                        .Body(user.UserName + " " + _localizer["Toplantı isteğinizi reddetti."].Value);
                            await email1.SendAsync();
                        }
                        catch { }
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
                if (ModelState.ErrorCount == 0) { 
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
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
        protected override void Dispose(bool disposing)
        {
            // need to alway test if disposing pass else reallocations could occur during Finalize pass
            // also good practice to test resource was created
            if (disposing)
            {
                bookingsTable.Dispose();
                availableDatesTable.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
