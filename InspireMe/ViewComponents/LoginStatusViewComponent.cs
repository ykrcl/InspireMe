using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InspireMe.ViewComponents
{
    public class LoginStatusViewComponent: ViewComponent
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _loginManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LoginStatusViewComponent(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> loginManager, RoleManager<IdentityRole> roleManager)
        {
            _loginManager = loginManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (_loginManager.IsSignedIn(HttpContext.User))
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var usertype = "Customer";
                if (await _userManager.IsInRoleAsync(user, "Supervisor"))
                {
                    usertype = "Supervisor";
                }
                ViewBag.usertype = usertype;
                return View("LoggedIn", user);
            }
            else
            {
                return View();
            }
        }

    }
}
