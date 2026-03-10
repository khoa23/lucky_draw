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
        public IActionResult GetRemain(int rewardId)
        {
            var reward = _context.Rewards.Find(rewardId);
            if (reward == null) return Content("0");
            var daQuay = _context.CustomerReward.Count(x => x.RewardId == rewardId);
            var soConLai = reward.NumberOfReward - daQuay;
            return Content(soConLai.ToString());
        }

        // AJAX: Quay số trả kết quả (hiển thị mã KH và tên KH nếu trúng)
        [HttpPost]
        public IActionResult Spin(int rewardId, string mode)
        {
            var reward = _context.Rewards.Find(rewardId);
            if (reward == null) return Content("<span class='text-danger'>Không tìm thấy giải!</span>");

            var candidates = _context.Customers
                .Where(c => !_context.CustomerReward.Any(cr => cr.RewardId == rewardId && cr.CustomerId == c.Id))
                .ToList();

            if (candidates.Count == 0)
                return Content("<span class='text-warning'>Đã hết lượt quay!</span>");

            // Chế độ tuần tự hoặc liên tục đều chọn ngẫu nhiên 1 khách (tùy bạn có thể thay đổi)
            var rnd = new Random();
            var winner = candidates[rnd.Next(candidates.Count)];

            // Lưu vào bảng kết quả
            _context.CustomerReward.Add(new lucky_draw.Models.CustomerReward
            {
                ProgramId = reward.ProgramId,
                RewardId = rewardId,
                CustomerId = winner.Id,
                ResultCode = winner.CustomerCode,
                TimeStamp = DateTime.Now,
                NumberOfCustomer = 1
            });
            _context.SaveChanges();

            // Lấy thông tin chi tiết khách hàng để trả ra
            var info = _context.CustomerInfos.FirstOrDefault(x => x.CustomerCode == winner.CustomerCode);

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
