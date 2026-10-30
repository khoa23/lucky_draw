using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    // Màn hình quay số; việc quay số đi qua api/luckydrawapi/spin
    public class LuckyDrawController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
