using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Services
{
    public class LuckyDrawService
    {
        private readonly LuckyDrawDbContext _context;

        public LuckyDrawService(LuckyDrawDbContext context)
        {
            _context = context;
        }

        // Chọn ngẫu nhiên tuần tự - Tối ưu cho DB lớn
        public async Task<Customer> RandomSequentialAsync(int rewardId)
        {
            // 1. Chỉ lấy những người CHƯA trúng bất kỳ giải nào
            var query = _context.Customers
                .Where(x => !_context.CustomerReward.Any(cr => cr.CustomerId == x.Id));

            // 2. Lấy tổng số người đủ điều kiện
            int count = await query.CountAsync();
            if (count == 0) return null;

            // 3. Chọn một vị trí ngẫu nhiên
            int randomOffset = Random.Shared.Next(0, count);

            // 4. Lấy người tại vị trí đó (Sử dụng Skip/Take nhanh hơn OrderBy Guid cho bảng lớn)
            var customer = await query
                .OrderBy(x => x.Id)
                .Skip(randomOffset)
                .FirstOrDefaultAsync();

            return customer;
        }

        // Chọn ngẫu nhiên liên tục (trả về nhiều) - Tối ưu cho DB lớn
        public async Task<List<Customer>> RandomContinuousAsync(int rewardId, int takeCount)
        {
            if (takeCount <= 0) return new List<Customer>();

            // Lọc những người chưa trúng giải nào
            var query = _context.Customers
                .Where(x => !_context.CustomerReward.Any(cr => cr.CustomerId == x.Id));

            int count = await query.CountAsync();
            if (count == 0) return new List<Customer>();

            // Nếu số lượng yêu cầu lớn hơn số người còn lại, lấy tất cả
            if (takeCount >= count) return await query.ToListAsync();

            // Chọn ngẫu nhiên (với danh sách nhiều người, dùng OrderBy Guid vẫn linh hoạt hơn nếu số lượng vừa phải)
            // Hoặc có thể dùng giải thuật Skip ngẫu nhiên nhiều lần. Ở đây dùng OrderBy Guid cho tính ngẫu nhiên cao.
            var customers = await query
                .OrderBy(x => Guid.NewGuid())
                .Take(takeCount)
                .ToListAsync();

            return customers;
        }

        // Phương thức lưu kết quả trúng thưởng tập trung
        public async Task<CustomerReward> SaveWinnerAsync(int rewardId, Customer winner)
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            if (reward == null || winner == null) return null;

            var result = new CustomerReward
            {
                ProgramId = reward.ProgramId,
                RewardId = reward.Id,
                CustomerId = winner.Id,
                ResultCode = winner.CustomerCode,
                TimeStamp = DateTime.Now,
                NumberOfCustomer = 1
            };

            _context.CustomerReward.Add(result);
            await _context.SaveChangesAsync();
            return result;
        }
    }
}

