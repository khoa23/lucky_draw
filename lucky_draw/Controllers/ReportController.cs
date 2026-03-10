using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers
{
    public class ReportController : Controller
    {
        private readonly LuckyDrawDbContext _context;
        public ReportController(LuckyDrawDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var results = await _context.CustomerReward
                .Include(x => x.Reward)
                .Include(x => x.Customer)
                .OrderBy(x => x.Reward.Idd)
                .ToListAsync();
            return View(results);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.CustomerReward.FindAsync(id);
            if (entity != null)
            {
                _context.CustomerReward.Remove(entity);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            _context.CustomerReward.RemoveRange(_context.CustomerReward);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var results = await _context.CustomerReward
                .Include(x => x.Reward)
                .Include(x => x.Customer)
                .OrderBy(x => x.Reward.Idd)
                .ToListAsync();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Winners");
                var currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "STT";
                worksheet.Cell(currentRow, 2).Value = "Giải thưởng";
                worksheet.Cell(currentRow, 3).Value = "Mã khách hàng";
                worksheet.Cell(currentRow, 4).Value = "Tên khách hàng";
                worksheet.Cell(currentRow, 5).Value = "Mã số trúng";
                worksheet.Cell(currentRow, 6).Value = "Thời gian";

                // Styling header
                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Data
                foreach (var item in results)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = currentRow - 1;
                    worksheet.Cell(currentRow, 2).Value = item.Reward?.RewardName;
                    worksheet.Cell(currentRow, 3).Value = item.Customer?.CustomerCode;
                    worksheet.Cell(currentRow, 4).Value = item.Customer?.Name;
                    worksheet.Cell(currentRow, 5).Value = item.ResultCode;
                    worksheet.Cell(currentRow, 6).Value = item.TimeStamp?.ToString("dd/MM/yyyy HH:mm");
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DanhSachTrungThuong_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
