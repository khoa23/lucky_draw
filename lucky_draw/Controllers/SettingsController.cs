using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers
{
    public class SettingsController : Controller
    {
        private readonly LuckyDrawDbContext _context;
        public SettingsController(LuckyDrawDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<LuckyProgram> programs = await _context.Programs.ToListAsync();
            IEnumerable<Reward> rewards = await _context.Rewards.OrderBy(r => r.Idd).ToListAsync();
            IEnumerable<RewardType> rewardTypes = await _context.RewardTypes.ToListAsync();
            return View((programs, rewards, rewardTypes));

        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgram(int id, string name, DateTime? dateStart)
        {
            var program = await _context.Programs.FindAsync(id);
            if (program == null) return NotFound();
            program.ProgramName = name;
            program.DateStart = dateStart;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReward(int id, string name, int quantity, byte? rewardType, byte? numberLength)
        {
            var reward = await _context.Rewards.FindAsync(id);
            if (reward == null) return NotFound();
            reward.RewardName = name;
            reward.NumberOfReward = quantity;
            reward.RewardType = rewardType;
            reward.NumberLength = numberLength;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddReward(int programId, string name, int quantity, byte? rewardType, byte? numberLength)
        {
            var maxIdd = await _context.Rewards.Where(r => r.ProgramId == programId).MaxAsync(r => (byte?)r.Idd) ?? 0;
            var reward = new Reward
            {
                ProgramId = programId,
                RewardName = name,
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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ReorderRewards([FromBody] List<int> sortedIds)
        {
            if (sortedIds == null || !sortedIds.Any()) return BadRequest();

            // Tải tất cả giải thưởng và lọc trong bộ nhớ để tránh lỗi OPENJSON trên các bản SQL Server cũ
            var allRewards = await _context.Rewards.ToListAsync();
            var rewards = allRewards.Where(r => sortedIds.Contains(r.Id)).ToList();

            // Bước 1: Tạm thời đưa các giá trị Idd lên một dải cao (ví dụ +128) 
            // để tránh xung đột "trùng ID" khi đang cập nhật dở dang giữa chừng.
            foreach (var r in rewards)
            {
                // Sử dụng .GetValueOrDefault() để tránh lỗi Nullable nếu Idd đang là null trong DB
                r.Idd = (byte)(r.Idd.GetValueOrDefault() + 128);
            }
            await _context.SaveChangesAsync();

            // Bước 2: Gán lại thứ tự chuẩn từ 1 đến n
            for (int i = 0; i < sortedIds.Count; i++)
            {
                var r = rewards.FirstOrDefault(x => x.Id == sortedIds[i]);
                if (r != null)
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
            _context.Rewards.Remove(reward);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
