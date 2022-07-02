using Microsoft.AspNetCore.Mvc;
using InspireMe.Data;
using Microsoft.AspNetCore.Identity;
using InspireMe.Models;
using Microsoft.Extensions.Localization;
using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using InspireMe.Identity;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using InspireMe.Hubs;

namespace InspireMe.Controllers
{

    public class RedirectAuthorized : TypeFilterAttribute
    {
        public RedirectAuthorized() : base(typeof(RedirectAuthorizedFilter))
        {
           
        }
    }
    public class RedirectAuthorizedFilter : IAuthorizationFilter
    {
       

        public RedirectAuthorizedFilter()
        {
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            SignInManager<IdentityUser> _loginManager = (SignInManager<IdentityUser>)context.HttpContext.RequestServices.GetService(typeof(SignInManager<IdentityUser>));
            if (_loginManager.IsSignedIn(context.HttpContext.User))
            {
                context.Result = new RedirectToActionResult("Index","Accounts", new {});
            }
        }
    }
    public class AccountsController : Controller
    {

        private readonly ILogger<AccountsController> _logger;
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _loginManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<AccountsController> _localizer;
        private readonly IFluentEmailFactory _emailFactory;
        private readonly IUserClaimsTable<string, IdentityUserClaim<string>> _userClaimsTable;
        private readonly IHubContext<SiteNotificationConnection> _NotificationhubContext;
        private readonly IUserConnectionManager _userConnectionManager;

        public AccountsController(ILogger<AccountsController> logger, IUserConnectionManager userConnectionManager, IHubContext<SiteNotificationConnection> NotificationhubContext, IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> loginManager, RoleManager<IdentityRole> roleManager, IStringLocalizer<AccountsController> localizer, IFluentEmailFactory emailFactory, IUserClaimsTable<string, IdentityUserClaim<string>> userClaimsTable)
        {
            _logger = logger;
            _emailFactory = emailFactory;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _userManager = userManager;
            _loginManager = loginManager;
            _roleManager = roleManager;
            _userClaimsTable = userClaimsTable;
            _userConnectionManager = userConnectionManager;
            _NotificationhubContext = NotificationhubContext;
        }
        
        
        
        public async Task<IActionResult> Index()
        {

            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var controller = "Accounts";
            var action = "Login";
            var area = "default";
            if (_loginManager.IsSignedIn(HttpContext.User))
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                if(await _userManager.IsInRoleAsync(user, "Supervisor"))
                {
                    action = "Index";
                    controller = "Manage";
                    area = "Supervisor";
                }
                else
                {
                    action = "Index";
                    controller = "Bookings";
                    area = "Client";
                }
            }
           
                return RedirectToAction(action, controller, new { area = area });
        }



        
        [RedirectAuthorized]
        public async Task<IActionResult> Register()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjax)
                return PartialView();
            else
                return View();

        }


        [HttpPost]
        [RedirectAuthorized]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                IdentityUser user = new IdentityUser();
                user.UserName = obj.UserName;
                user.PhoneNumber = obj.PhoneNumber;
                user.Email = obj.Email;
                IdentityResult result = await _userManager.CreateAsync(user, obj.Password);
                if (result.Succeeded)
                {
                    string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    string? confirmationLink = Url.Action("ConfirmEmail",
                          "Accounts", new
                          {
                              userid = user.Id,
                              token = confirmationToken
                          },
                           protocol: HttpContext.Request.Scheme);
                    var email1 = _emailFactory
                        .Create()
                        .To(user.Email)
                        .Subject(_localizer["E-Posta Doğrulama"].Value)
                        .Body(_localizer["E-Postanızı bu linkten doğrulayın:"] + " " + confirmationLink);

                    await email1.SendAsync();
                    if (obj.IsSupervisor == true)
                    { 
                        if (!_roleManager.RoleExistsAsync("Supervisor").Result)
                        {
                            IdentityRole role = new IdentityRole();
                            role.Name = "Supervisor";
                            IdentityResult roleResult = await _roleManager.CreateAsync(role);
                            if (!roleResult.Succeeded)
                            {
                                _logger.LogError("RoleCreationError", new { result = roleResult });
                                ModelState.AddModelError("",
                                 "Error while creating role!");
                                if (isAjax)
                                    return PartialView(obj);
                                else
                                    return View(obj);
                            }
                        }
                        await _userManager.AddToRoleAsync(user, "Supervisor");
                    }
                    else
                    {
                        if (!_roleManager.RoleExistsAsync("Customer").Result)
                        {
                            IdentityRole role = new IdentityRole();
                            role.Name = "Customer";
                            IdentityResult roleResult = await _roleManager.CreateAsync(role);
                            if (!roleResult.Succeeded)
                            {
                                ModelState.AddModelError("",
                                 "Error while creating role!");
                                if (isAjax)
                                    return PartialView(obj);
                                else
                                    return View(obj);
                            }
                        }
                        await _userManager.AddToRoleAsync(user, "Customer");
                    }
                    if (isAjax)
                    {
                        return Json(new { success = true, redirect = Url.Action("Index","Accounts"), alert = _localizer["Aramıza hoşgeldiniz!"].Value });
                    }
                    else
                    {
                        ViewBag.message = _localizer["Aramıza hoşgeldiniz!"].Value;
                        ViewBag.title = _localizer["Kayıt Ol!"].Value;
                        return View("Message");
                    }

                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    if (isAjax)
                        return PartialView(obj);
                    else
                        return View(obj);
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        public async Task<IActionResult> ConfirmEmail(string userid, string token)
        {
            IdentityUser user = await _userManager.FindByIdAsync(userid);
            IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                ViewBag.message = _localizer["E-Posta başarıyla doğrulandı!"].Value;
                ViewBag.title = _localizer["E-Posta Doğrulama"].Value;
                return View("Message");
            }
            else
            {
                ViewBag.title = _localizer["E-Posta Doğrulama"].Value;
                ViewBag.message = _localizer["E-Posta doğrulanırken bir hata oluştu!"].Value;
                return View("Message");
            }
        }
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return PartialView();
            else
                return View();

        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var result = await _userManager.ChangePasswordAsync(user, obj.OldPassword, obj.Password);
                if (result.Succeeded)
                {
                    if (isAjax)
                    {
                        return Json(new { success = true, alert = _localizer["Parolanız Başarıyla Değiştirildi!"], redirect = Url.Action("ChangePassword", "Accounts") });
                    }
                    else
                    {
                        ViewBag.title = _localizer["Parola Değiştir"];
                        ViewBag.message = _localizer["Parolanız Başarıyla Değiştirildi!"];
                        return View("Message");
                    }
                }
                else
                {
                    foreach (var error in result.Errors){
                        ModelState.AddModelError("", error.Description);
                    }    
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);

        }


        [Authorize]
        public async Task<IActionResult> Settings()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var obj = new SettingsViewModel();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var claims = await _userManager.GetClaimsAsync(user);
            var allfields = await _userClaimsTable.GetClaimValuesByTypeAsync("field");
            ViewBag.allfields = allfields;
            var fields = claims.Where(x => x.Type == "field").Select(x => x.Value).ToList();
            obj.fields = String.Join(", ", fields);
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var claims = (await _userManager.GetClaimsAsync(user)).ToList();
                var fields = obj.fields.Split(',').Select(p => p.Trim()).ToList();
                var deletedclaims = new List<Claim>();
                foreach (var claim in claims)
                {
                    if (claim.Type == "field")
                    {
                        if (fields.Contains(claim.Value))
                        {
                            fields.Remove(claim.Value);
                        }
                        else
                        {
                            deletedclaims.Add(claim);
                            
                        }
                    }
                }
                await _userManager.RemoveClaimsAsync(user, deletedclaims);
                foreach (var newfield in fields)
                {
                    if (newfield != "") { 
                    var claim = new Claim("field", newfield);
                    await _userManager.AddClaimAsync(user, claim);
                    }
                }
                await _userManager.UpdateAsync(user);
                if (isAjax)
                {
                    return Json(new { success = true, alert = _localizer["Hesap Ayarları Kaydedildi!"].Value, redirect = Url.Action("Settings", "Accounts") });
                }
                else
                {
                    ViewBag.title = _localizer["Hesap Ayarları"].Value;
                    ViewBag.message = _localizer["Hesap Ayarları Kaydedildi!"].Value;
                    return View("Message");
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);

        }
        public async Task<IActionResult> loginstatus()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return ViewComponent("LoginStatus");
            else
                return RedirectToAction("Index", "Accounts");

        }
        [RedirectAuthorized]
        public async Task<IActionResult> Login()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return PartialView();
            else
                return View();

        }
        [HttpPost]
        [RedirectAuthorized]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByEmailAsync(obj.Email);
                    var result = await _loginManager.PasswordSignInAsync(user, obj.Password, obj.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = true, updatelogin=true, alert = _localizer["Hoşgeldiniz!"].Value, redirect = Url.Action("Index", "Accounts") });
                        }
                        else
                        {
                            ViewBag.title = _localizer["Giriş Yap"].Value;
                            ViewBag.message = _localizer["Hoşgeldiniz!"].Value;
                            return View("Message");
                        }
                    }
                    else
                    {
                       
                            ModelState.AddModelError("", _localizer["E-Posta veya Parola Yanlış!!!"].Value);
                        
                    }
                }
                catch
                {
                    ModelState.AddModelError("Email", _localizer["Kullanıcı bulunamadı!"].Value);

                }

            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        public async Task<IActionResult> LogOut()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (HttpContext.Request.Method.ToLower() == "get")
            {
                if (isAjax)
                    return PartialView();
                else
                    return View();
            }
            else {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                await _loginManager.SignOutAsync();
                try { 
                var connections = _userConnectionManager.GetUserConnections(user.Id);
                foreach (var connection in connections)
                {
                    await _NotificationhubContext.Clients.Client(connection).SendAsync("EndConnection", true);
                }
                }
                catch { }
                if (isAjax)
                {
                    return Json(new { success = true, updatelogin = true, alert = _localizer["Güle Güle!"].Value, redirect = Url.Action("Index", "Accounts") });
                }
                else
                {
                    ViewBag.title = _localizer["Çıkış Yap"].Value;
                    ViewBag.message = _localizer["Hoşgeldiniz!"].Value;
                    return View("Message");
                }
            }
        }
        [RedirectAuthorized]
        public async Task<IActionResult> ForgotPassword()
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return PartialView();
            else
                return View();

        }
        [HttpPost]
        [RedirectAuthorized]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByEmailAsync(obj.Email);
                    string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    string? resetLink = Url.Action("ResetPassword",
                          "Accounts", new
                          {
                              userid = user.Id,
                              token = resetToken
                          },
                           protocol: HttpContext.Request.Scheme);
                    var email1 = _emailFactory
                        .Create()
                        .To(user.Email)
                        .Subject(_localizer["Parola Sıfırlama"].Value)
                        .Body(_localizer["Parolanızı bu linkten sıfırlayın:"].Value + " " + resetLink);

                    await email1.SendAsync();
                    if (isAjax)
                    {
                        return Json(new { success = true, alert = _localizer["Parolama sıfırlama linki e-postanıza gönderildi!"].Value, redirect = Url.Action("Index", "Accounts") });
                    }
                    else
                    {
                        ViewBag.message = _localizer["Parolama sıfırlama linki e-postanıza gönderildi!"].Value;
                        return View("Message");
                    }

                }
                catch
                {
                    ModelState.AddModelError("Email", _localizer["Kullanıcı bulunamadı!"].Value);
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);
        }

        public async Task<IActionResult> ResetPassword(string userid, string token)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            try { 
                IdentityUser user = await _userManager.FindByIdAsync(userid);
                var obj = new ResetPasswordViewModel();
                obj.ResetToken = token;
                obj.UserId = userid;
                if (isAjax)
                    return PartialView(obj);
                else
                    return View(obj);
            }
            catch
            {
                ViewBag.title = _localizer["Parola Sıfırlama"].Value;
                ViewBag.message = _localizer["Kullanıcı bulunamadı!"].Value;
                    return View("Message");   
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel obj)
        {
            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (ModelState.IsValid)
            {
                try { 
                IdentityUser user = await _userManager.FindByIdAsync(obj.UserId);
                var result = await _userManager.ResetPasswordAsync(user, obj.ResetToken, obj.Password);
                if (result.Succeeded)
                {
                    if (isAjax)
                    {
                        return Json(new { success = true, alert = _localizer["Parolanız Başarıyla Değiştirildi!"].Value, redirect = Url.Action("Index", "Accounts") });
                    }
                    else
                    {
                            ViewBag.title = _localizer["Parola Sıfırlama"].Value;
                            ViewBag.message = _localizer["Parolanız Başarıyla Değiştirildi!"].Value;
                        return View("Message");
                    }
                }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError("", _localizer["Kullanıcı bulunamadı!"].Value);
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);

        }
    }
}
