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

        [HttpGet]
        public IActionResult ImportExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    await _importService.ImportTemptableAsync(stream);
                    ViewBag.Message = "Import dữ liệu thành công!";
                }
                catch (System.Exception ex)
                {
                    ViewBag.Error = "Lỗi khi import: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Error = "Vui lòng chọn file Excel.";
            }
            return View();
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Template");
                
                // Add Headers
                string[] headers = { "BRCD", "LCLBRNM", "FTRSERIES", "LSTSERIES", "IDXACNO", "NM", "IDNO", "DIACHI", "CUSTSEQ", "CARDNO" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.LightYellow);
                }

                // Add sample row
                worksheet.Cell(2, 1).Value = "0101";
                worksheet.Cell(2, 2).Value = "Chi nhánh Hà Nội";
                worksheet.Cell(2, 3).Value = "0000001";
                worksheet.Cell(2, 4).Value = "0000100";
                worksheet.Cell(2, 5).Value = "123456789";
                worksheet.Cell(2, 6).Value = "Nguyễn Văn A";
                worksheet.Cell(2, 7).Value = "001202000001";
                worksheet.Cell(2, 8).Value = "Hà Nội";
                worksheet.Cell(2, 9).Value = "1";
                worksheet.Cell(2, 10).Value = "9704230000001234";

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Import_KhachHang.xlsx");
                }
            }
        }
    }
}
