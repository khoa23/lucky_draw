using lucky_draw.Data;
using lucky_draw.Models;
using lucky_draw.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers
{
    public class LuckyDrawController : Controller
    {
        private readonly LuckyDrawService _drawService;
        private readonly LuckyDrawDbContext _context;

        public LuckyDrawController(LuckyDrawService drawService, LuckyDrawDbContext context)
        {
            _drawService = drawService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // AJAX: Lấy số lượt còn lại cho từng giải
        [HttpGet]
        public async Task<IActionResult> GetRemain(int rewardId)
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            if (reward == null) return Content("0");
            var daQuay = await _context.CustomerReward.CountAsync(x => x.RewardId == rewardId);
            var soConLai = reward.NumberOfReward - daQuay;
            return Content(soConLai.ToString());
        }

        // AJAX: Quay số trả kết quả (hiển thị mã KH và tên KH nếu trúng)
        [HttpPost]
        public async Task<IActionResult> Spin(int rewardId, string mode)
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            if (reward == null) return Content("<span class='text-danger'>Không tìm thấy giải!</span>");

            // Kiểm tra số lượng còn lại
            var winsCount = await _context.CustomerReward.CountAsync(cr => cr.RewardId == rewardId);
            if (winsCount >= reward.NumberOfReward)
            {
                return Content("<span class='text-warning'>Đã hết lượt quay!</span>");
            }

            // Dùng service để chọn ngẫu nhiên trực tiếp từ DB
            var winner = await _drawService.RandomSequentialAsync(rewardId);

            if (winner == null)
                return Content("<span class='text-warning'>Không còn khách hàng nào thỏa mãn điều kiện!</span>");

            // Lưu vào bảng kết quả dùng service
            await _drawService.SaveWinnerAsync(rewardId, winner);

            // Lấy thông tin chi tiết khách hàng để trả ra
            var info = await _context.CustomerInfos.FirstOrDefaultAsync(x => x.CustomerCode == winner.CustomerCode);

            string resultHtml = $@"
        <div class='lucky-result'>
            <div>Mã số: <b>{winner.CustomerCode}</b></div>
            <div>Tên: <b>{winner.Name}</b></div>
            {(info != null ? $"<div>CMND/CCCD: <b>{info.IdNo}</b></div><div>Điện thoại: <b>{info.TelNo}</b></div>" : "")}
        </div>
    ";

            return Content(resultHtml, "text/html");
        }

    }
}

