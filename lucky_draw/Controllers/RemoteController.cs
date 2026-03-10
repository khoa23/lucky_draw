using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    public class RemoteController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
