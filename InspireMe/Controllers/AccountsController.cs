using Microsoft.AspNetCore.Mvc;
using InspireMe.Data;
using Microsoft.AspNetCore.Identity;
using InspireMe.Models;
using Microsoft.Extensions.Localization;
using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using InspireMe.Identitiy;
using Dapper;

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
        
        public AccountsController(ILogger<AccountsController> logger, IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> loginManager, RoleManager<IdentityRole> roleManager, IStringLocalizer<AccountsController> localizer, IFluentEmailFactory emailFactory, IUserClaimsTable<string, IdentityUserClaim<string>> userClaimsTable)
        {
            _logger = logger;
            _emailFactory = emailFactory;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _userManager = userManager;
            _loginManager = loginManager;
            _roleManager = roleManager;
            _userClaimsTable = userClaimsTable;
        }
        public async Task<IActionResult> Index()
        {

            bool isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
                return Json(new { redirectTo = Url.Action("Index", "ControllerAction") });
            else
                return RedirectToAction("Index", "ControllerAction");
        }



        [HttpPost]
        [RedirectAuthorized]
        [ValidateAntiForgeryToken]
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
                        .Subject(_localizer["E-Posta Doğrulama"])
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
                        return Json(new { success = true, alert = _localizer["Aramıza hoşgeldiniz!"] });
                    }
                    else
                    {
                        ViewBag.message = _localizer["Aramıza hoşgeldiniz!"];
                        ViewBag.title = _localizer["Kayıt Ol!"];
                        return View("Message");
                    }

                }
                else
                {
                    ModelState.AddModelError("", string.Join(", ",
                                 result.Errors));
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
                ViewBag.message = _localizer["E-Posta başarıyla doğrulandı!"];
                ViewBag.title = _localizer["E-Posta Doğrulama"];
                return View("Message");
            }
            else
            {
                ViewBag.title = _localizer["E-Posta Doğrulama"];
                ViewBag.message = _localizer["E-Posta doğrulanırken bir hata oluştu!"];
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
                        return Json(new { success = true, alert = _localizer["Parolanız Başarıyla Değiştirildi!"], redirect = Url.Action("ChangePassword", "AccountsController") });
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
            var allfields = await _userClaimsTable.GetClaimValuesByTypeAsync("fields");
            ViewBag.allfields = allfields;
            var fields = claims.Where(x => x.Type == "field").Select(x => x.Value).ToList();
            obj.fields = String.Join(", ", claims);
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
                var claims = await _userManager.GetClaimsAsync(user);
                var fields = obj.fields.Split(',').Select(p => p.Trim()).ToList();
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
                            await _userManager.RemoveClaimAsync(user, claim);
                        }
                    }
                }
                foreach(var newfield in fields)
                {
                    var claim = new Claim("field", newfield);
                    await _userManager.AddClaimAsync(user, claim);
                }
                await _userManager.UpdateAsync(user);
                if (isAjax)
                {
                    return Json(new { success = true, alert = _localizer["Hesap Ayarları Kaydedildi!"], redirect = Url.Action("Settings", "AccountsController") });
                }
                else
                {
                    ViewBag.title = _localizer["Hesap Ayarları"];
                    ViewBag.message = _localizer["Hesap Ayarları Kaydedildi!"];
                    return View("Message");
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);

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
                            return Json(new { success = true, alert = _localizer["Hoşgeldiniz!"], redirect = Url.Action("Index", "AccountsController") });
                        }
                        else
                        {
                            ViewBag.title = _localizer["Giriş Yap"];
                            ViewBag.message = _localizer["Hoşgeldiniz!"];
                            return View("Message");
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError("Email", _localizer["Kullanıcı bulunamadı!"]);

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
                await _loginManager.SignOutAsync();
                if (isAjax)
                {
                    return Json(new { success = true, alert = _localizer["Güle Güle!"], redirect = Url.Action("Index", "AccountsController") });
                }
                else
                {
                    ViewBag.title = _localizer["Çıkış Yap"];
                    ViewBag.message = _localizer["Hoşgeldiniz!"];
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
                        .Subject(_localizer["Parola Sıfırlama"])
                        .Body(_localizer["Parolanızı bu linkten sıfırlayın:"] + " " + resetLink);

                    await email1.SendAsync();
                    if (isAjax)
                    {
                        return Json(new { success = true, alert = _localizer["Parolama sıfırlama linki e-postanıza gönderildi!"], redirect = Url.Action("Index", "AccountsController") });
                    }
                    else
                    {
                        ViewBag.message = _localizer["Parolama sıfırlama linki e-postanıza gönderildi!"];
                        return View("Message");
                    }

                }
                catch
                {
                    ModelState.AddModelError("Email", _localizer["Kullanıcı bulunamadı!"]);
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
                ViewBag.title = _localizer["Parola Sıfırlama"];
                ViewBag.message = _localizer["Kullanıcı bulunamadı!"];
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
                        return Json(new { success = true, alert = _localizer["Parolanız Başarıyla Değiştirildi!"], redirect = Url.Action("Index", "AccountsController") });
                    }
                    else
                    {
                            ViewBag.title = _localizer["Parola Sıfırlama"];
                            ViewBag.message = _localizer["Parolanız Başarıyla Değiştirildi!"];
                        return View("Message");
                    }
                }
                    else
                    {
                        ModelState.AddModelError("", _localizer["Url'de hata var!"]);
                    }
                }
                catch
                {
                    ModelState.AddModelError("", _localizer["Kullanıcı bulunamadı!"]);
                }
            }
            if (isAjax)
                return PartialView(obj);
            else
                return View(obj);

        }
    }
}
