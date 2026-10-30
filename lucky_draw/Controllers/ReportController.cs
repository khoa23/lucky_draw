using System.Globalization;
using System.Security.Claims;
using ClosedXML.Excel;
using lucky_draw.Data;
using lucky_draw.Hubs;
using lucky_draw.Models;
using lucky_draw.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers
{
    public class ReportController : Controller
    {
        private readonly LuckyDrawDbContext _context;
        private readonly IHubContext<LuckyDrawHub> _hub;
        private readonly ILogger<ReportController> _logger;
        private readonly AuditLogger _audit;
        public ReportController(LuckyDrawDbContext context, IHubContext<LuckyDrawHub> hub, ILogger<ReportController> logger,
            AuditLogger audit)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            return View(await LoadResultsAsync());
        }

        // Danh sách người trúng, sắp theo thứ tự giải
        private Task<List<CustomerReward>> LoadResultsAsync() =>
            _context.CustomerReward
                .AsNoTracking()
                .Include(x => x.Reward)
                .Include(x => x.Customer)
                .OrderBy(x => x.Reward.Idd)
                .ToListAsync();

        // Chuyển kết quả sang bảng lịch sử rồi xóa, chạy trong 1 transaction để không mất dữ liệu nếu lỗi giữa chừng
        private const string ArchiveSql = "INSERT INTO CustomerRewardHistory (Id, ProgramId, RewardId, CustomerId, ResultCode, TimeStamp, NumberOfCustomer) " +
                                          "SELECT Id, ProgramId, RewardId, CustomerId, ResultCode, TimeStamp, NumberOfCustomer FROM CustomerReward";

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Đọc thông tin trước khi xóa để ghi nhật ký
            var deleted = await DescribeResultsAsync(_context.CustomerReward.Where(cr => cr.Id == id));

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Database.ExecuteSqlRawAsync(ArchiveSql + " WHERE Id = {0}", id);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM CustomerReward WHERE Id = {0}", id);
            await transaction.CommitAsync();
            _logger.LogWarning("Xóa kết quả trúng thưởng {ResultId} từ {Ip}", id, HttpContext.Connection.RemoteIpAddress);
            await _audit.LogAsync(AuditLogs.Report, AuditEvents.DeleteResult, HttpContext, CurrentRole,
                deleted.Count > 0 ? deleted[0] : $"Không tìm thấy kết quả #{id} (có thể đã bị xóa trước đó)");
            await NotifyRewardsChangedAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            // Đọc thông tin trước khi xóa để ghi nhật ký
            var deletedResults = await DescribeResultsAsync(_context.CustomerReward);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Database.ExecuteSqlRawAsync(ArchiveSql);
            var deleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM CustomerReward");
            await transaction.CommitAsync();
            _logger.LogWarning("Xóa toàn bộ {Count} kết quả trúng thưởng từ {Ip}", deleted, HttpContext.Connection.RemoteIpAddress);

            // 1 dòng tổng + 1 dòng cho từng kết quả bị xóa
            await _audit.LogAsync(AuditLogs.Report, AuditEvents.DeleteAllResults, HttpContext, CurrentRole,
                $"Xóa toàn bộ {deleted} kết quả trúng thưởng");
            foreach (var item in deletedResults)
            {
                await _audit.LogAsync(AuditLogs.Report, AuditEvents.DeleteResult, HttpContext, CurrentRole, "(xóa toàn bộ) " + item);
            }
            await NotifyRewardsChangedAsync();
            return RedirectToAction("Index");
        }

        private string? CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value;

        // Mô tả từng kết quả cho nhật ký: mã kết quả, giải, mã + tên khách hàng, thời điểm trúng
        private static async Task<List<string>> DescribeResultsAsync(IQueryable<CustomerReward> query)
        {
            var rows = await query.AsNoTracking()
                .OrderBy(cr => cr.Id)
                .Select(cr => new
                {
                    cr.Id,
                    RewardName = cr.Reward.RewardName,
                    cr.ResultCode,
                    CustomerName = cr.Customer != null ? cr.Customer.Name : null,
                    cr.TimeStamp
                })
                .ToListAsync();

            return rows.Select(r => $"Kết quả #{r.Id}; Giải: {r.RewardName}; Khách hàng: {r.ResultCode} - {r.CustomerName}; " +
                                    $"Trúng lúc: {r.TimeStamp?.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)}")
                       .ToList();
        }

        // Báo cho màn hình quay số cập nhật lại số lượt còn lại mà không cần tải lại trang
        private Task NotifyRewardsChangedAsync() => _hub.Clients.All.SendAsync("RewardsChanged");

        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var results = await LoadResultsAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Winners");

            string[] headers = { "STT", "Giải thưởng", "Mã khách hàng", "Tên khách hàng", "Mã số trúng", "Thời gian" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }
            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 2).Value = item.Reward.RewardName;
                worksheet.Cell(row, 3).Value = item.Customer?.CustomerCode;
                worksheet.Cell(row, 4).Value = item.Customer?.Name;
                worksheet.Cell(row, 5).Value = item.ResultCode;
                worksheet.Cell(row, 6).Value = item.TimeStamp?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachTrungThuong_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
