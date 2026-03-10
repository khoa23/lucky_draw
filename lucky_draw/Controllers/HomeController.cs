using lucky_draw.Services;
using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    public class HomeController : Controller
    {
        private readonly ExcelImportService _importService;

        public HomeController(ExcelImportService importService)
        {
            _importService = importService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                await _importService.ImportTemptableAsync(stream);
                ViewBag.Message = "Import thành công!";
            }
            return View();
        }
    }
}
