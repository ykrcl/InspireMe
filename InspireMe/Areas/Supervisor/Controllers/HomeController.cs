using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InspireMe.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Manage", new {area="Client"});
        }
    }
}
