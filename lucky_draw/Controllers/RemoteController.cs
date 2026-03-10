using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    public class RemoteController : Controller
    {
        private readonly IConfiguration _configuration;

        public RemoteController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("RemoteAuthenticated") != "true")
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string password)
        {
            var correctPassword = _configuration["RemoteSettings:Password"];
            if (password == correctPassword)
            {
                HttpContext.Session.SetString("RemoteAuthenticated", "true");
                return RedirectToAction("Index");
            }
            ViewBag.Error = "Mật khẩu không đúng!";
            return View();
        }
    }
}
