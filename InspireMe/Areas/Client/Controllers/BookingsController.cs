using Microsoft.AspNetCore.Mvc;

namespace InspireMe.Areas.Client.Controllers
{
    public class BookingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
