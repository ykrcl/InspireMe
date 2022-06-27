using Microsoft.AspNetCore.Mvc;

namespace InspireMe.Areas.Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Bookings", new {area="Client"});
        }
    }
}
