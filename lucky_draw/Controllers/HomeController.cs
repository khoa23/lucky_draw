using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ExcelDataReader.Exceptions;
using lucky_draw.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    public class HomeController : Controller
    {
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly string[] AllowedImportExtensions = { ".xlsx", ".xls", ".csv" };
        private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

        private readonly ExcelImportService _importService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ExcelImportService importService, ILogger<HomeController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Trang lỗi chung khi chạy production: không hiển thị chi tiết lỗi cho người dùng
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult Error()
        {
            return Content("Đã xảy ra lỗi. Vui lòng thử lại hoặc liên hệ quản trị viên.", "text/plain; charset=utf-8");
        }

        [HttpGet]
        public IActionResult ImportExcel()
        {
            return View();
        }

        // Cho phép upload file lớn (danh sách ~10 triệu dòng); IIS cần thêm cấu hình trong web.config
        private const long MaxImportFileSize = 2L * 1024 * 1024 * 1024; // 2 GB

        [HttpPost]
        [RequestSizeLimit(MaxImportFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxImportFileSize)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                // Chỉ nhận đúng loại file danh sách khách hàng
                if (!AllowedImportExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Chỉ hỗ trợ file .xlsx, .xls hoặc .csv.";
                    return View();
                }

                try
                {
                    _logger.LogInformation("Bắt đầu import file {FileName} ({Size} bytes) từ {Ip}",
                        Path.GetFileName(file.FileName), file.Length, HttpContext.Connection.RemoteIpAddress);
                    using var stream = file.OpenReadStream();
                    var totalRows = await _importService.ImportTemptableAsync(stream, file.FileName);
                    _logger.LogInformation("Import xong {TotalRows} dòng", totalRows);
                    ViewBag.Message = $"Import dữ liệu thành công! Đã nạp {totalRows.ToString("N0", VietnameseCulture)} dòng.";
                }
                catch (SqlException ex) when (ex.Number is 2627 or 2601)
                {
                    // Trùng khóa chính CustomerInfo (cột CustomerId) với dữ liệu đã có hoặc trùng ngay trong file.
                    // Procedure chạy trọn 1 câu lệnh nên không có dòng nào được nạp vào danh sách khách hàng.
                    var duplicate = Regex.Match(ex.Message, @"duplicate key value is \((.*?)\)").Groups[1].Value;
                    _logger.LogWarning("Import bị trùng khách hàng CustomerId = {Duplicate}", duplicate);
                    ViewBag.Error = $"Dữ liệu bị trùng: khách hàng có CustomerId = {duplicate} đã tồn tại hoặc xuất hiện nhiều lần trong file. " +
                                    "Không có dòng nào được nạp, vui lòng loại bỏ các dòng trùng rồi import lại.";
                }
                catch (ImportFormatException ex)
                {
                    // Lỗi cấu trúc file (thiếu tiêu đề / cột bắt buộc): báo nguyên văn để người dùng sửa file
                    _logger.LogWarning("File import sai cấu trúc: {Message}", ex.Message);
                    ViewBag.Error = ex.Message;
                }
                catch (ExcelReaderException ex)
                {
                    _logger.LogWarning(ex, "File import không đúng định dạng");
                    ViewBag.Error = "File không đúng định dạng Excel/CSV hoặc đã bị hỏng.";
                }
                catch (Exception ex)
                {
                    // Không hiển thị chi tiết lỗi (có thể lộ thông tin database), chỉ ghi log
                    _logger.LogError(ex, "Lỗi khi import dữ liệu");
                    ViewBag.Error = "Lỗi khi import dữ liệu. Vui lòng kiểm tra log máy chủ.";
                }
            }
            else
            {
                ViewBag.Error = "Vui lòng chọn file Excel hoặc CSV.";
            }
            return View();
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = ExcelImportService.ColumnNames;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightYellow);
            }

            // Dòng mẫu, đúng thứ tự cột ở trên. Nhập dạng chữ để giữ số 0 ở đầu mã
            string[] sample = { "0101", "Chi nhánh Hà Nội", "0000001", "0000100", "123456789", "Nguyễn Văn A", "001202000001", "Hà Nội", "1", "9704230000001234" };
            for (int i = 0; i < sample.Length; i++)
            {
                worksheet.Cell(2, i + 1).Value = sample[i];
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), XlsxContentType, "Mau_Import_KhachHang.xlsx");
        }
    }
}
