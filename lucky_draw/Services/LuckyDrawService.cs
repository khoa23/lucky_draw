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
            // Thực hiện ORDER BY NEWID() trực tiếp dưới SQL Server để lấy 1 bản ghi ngẫu nhiên
            // Điều này tránh việc tải hàng triệu dòng vào RAM
            var customer = await _context.Customers
                .Where(x => !_context.CustomerReward.Any(cr => cr.RewardId == rewardId && cr.CustomerId == x.Id))
                .OrderBy(x => Guid.NewGuid()) 
                .FirstOrDefaultAsync();

            return customer;
        }

        // Chọn ngẫu nhiên liên tục (trả về nhiều) - Tối ưu cho DB lớn
        public async Task<List<Customer>> RandomContinuousAsync(int rewardId, int takeCount)
        {
            if (takeCount <= 0) return new List<Customer>();

            var customers = await _context.Customers
                .Where(x => !_context.CustomerReward.Any(cr => cr.RewardId == rewardId && cr.CustomerId == x.Id))
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

