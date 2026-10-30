using System.Globalization;
using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers
{
    public class SettingsController : Controller
    {
        // Khớp độ dài cột trong database (Rewards.RewardName nvarchar(50), Programs.ProgramName nvarchar(255))
        private const int MaxRewardNameLength = 50;
        private const int MaxProgramNameLength = 255;
        private const int MaxRewardQuantity = 1_000_000;
        private const int MaxNumberLength = 20;

        private readonly LuckyDrawDbContext _context;
        private readonly ILogger<SettingsController> _logger;
        public SettingsController(LuckyDrawDbContext context, ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Kiểm tra dữ liệu nhập phía server (không tin vào kiểm tra trên trình duyệt)
        private static bool IsValidReward(string? name, int quantity, byte? numberLength) =>
            !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxRewardNameLength &&
            quantity >= 0 && quantity <= MaxRewardQuantity &&
            (numberLength == null || (numberLength >= 1 && numberLength <= MaxNumberLength));

        public async Task<IActionResult> Index()
        {
            IEnumerable<LuckyProgram> programs = await _context.Programs.ToListAsync();
            IEnumerable<Reward> rewards = await _context.Rewards.OrderBy(r => r.Idd).ToListAsync();
            IEnumerable<RewardType> rewardTypes = await _context.RewardTypes.ToListAsync();
            return View((programs, rewards, rewardTypes));
        }

        // Loại giải phải có trong bảng RewardTypes (không có khóa ngoại trong database nên kiểm tra ở đây)
        private async Task<bool> IsValidRewardTypeAsync(byte? rewardType) =>
            rewardType == null || await _context.RewardTypes.AnyAsync(t => t.Id == rewardType);

        [HttpPost]
        public async Task<IActionResult> UpdateProgram(int id, string name, string? dateStart)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxProgramNameLength) return BadRequest("Tên chương trình không hợp lệ.");

            // Ngày nhập theo dạng dd/MM/yyyy, đọc cố định không phụ thuộc ngôn ngữ của máy chủ
            DateTime? startDate = null;
            if (!string.IsNullOrWhiteSpace(dateStart))
            {
                if (!DateTime.TryParseExact(dateStart.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return BadRequest("Ngày bắt đầu không hợp lệ, vui lòng nhập theo dạng dd/mm/yyyy.");
                startDate = parsed;
            }

            var program = await _context.Programs.FindAsync(id);
            if (program == null) return NotFound();
            program.ProgramName = name.Trim();
            program.DateStart = startDate;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReward(int id, string name, int quantity, byte? rewardType, byte? numberLength)
        {
            if (!IsValidReward(name, quantity, numberLength) || !await IsValidRewardTypeAsync(rewardType))
                return BadRequest("Thông tin giải thưởng không hợp lệ.");
            var reward = await _context.Rewards.FindAsync(id);
            if (reward == null) return NotFound();
            _logger.LogInformation("Sửa giải {RewardId}: {OldQuantity} -> {NewQuantity} giải", id, reward.NumberOfReward, quantity);
            reward.RewardName = name.Trim();
            reward.NumberOfReward = quantity;
            reward.RewardType = rewardType;
            reward.NumberLength = numberLength;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddReward(int programId, string name, int quantity, byte? rewardType, byte? numberLength)
        {
            if (!IsValidReward(name, quantity, numberLength) || !await IsValidRewardTypeAsync(rewardType))
                return BadRequest("Thông tin giải thưởng không hợp lệ.");
            if (!await _context.Programs.AnyAsync(p => p.Id == programId)) return NotFound();
            var maxIdd = await _context.Rewards.Where(r => r.ProgramId == programId).MaxAsync(r => (byte?)r.Idd) ?? 0;
            var reward = new Reward
            {
                ProgramId = programId,
                RewardName = name.Trim(),
                NumberOfReward = quantity,
                RewardType = rewardType,
                NumberLength = numberLength,
                Idd = (byte)(maxIdd + 1),
                PrefixCode = ""
            };
            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ReorderRewards([FromBody] List<int> sortedIds)
        {
            if (sortedIds == null || sortedIds.Count == 0 || sortedIds.Count > byte.MaxValue / 2) return BadRequest();

            // Contains() được dịch thành IN (...) (đã cấu hình compatibility level trong Program.cs)
            var rewards = await _context.Rewards.Where(r => sortedIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);

            // Bước 1: tạm đưa Idd lên dải cao (+128) để tránh trùng thứ tự khi đang cập nhật dở dang
            foreach (var r in rewards.Values)
            {
                r.Idd = (byte)(r.Idd.GetValueOrDefault() + 128);
            }
            await _context.SaveChangesAsync();

            // Bước 2: gán lại thứ tự chuẩn từ 1 đến n theo đúng thứ tự kéo thả
            for (int i = 0; i < sortedIds.Count; i++)
            {
                if (rewards.TryGetValue(sortedIds[i], out var r))
                {
                    r.Idd = (byte)(i + 1);
                }
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReward(int id)
        {
            var reward = await _context.Rewards.FindAsync(id);
            if (reward == null) return NotFound();

            // Không cho xóa giải đã có người trúng: kết quả sẽ bị "mồ côi" (mất khỏi Báo cáo nhưng khách vẫn bị tính là đã trúng)
            if (await _context.CustomerReward.AnyAsync(cr => cr.RewardId == id))
                return BadRequest("Giải này đã có người trúng. Vui lòng xóa các kết quả của giải ở trang Báo cáo trước khi xóa giải.");

            _logger.LogWarning("Xóa giải {RewardId} ({RewardName}) từ {Ip}", id, reward.RewardName, HttpContext.Connection.RemoteIpAddress);
            _context.Rewards.Remove(reward);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
