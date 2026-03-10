using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.EntityFrameworkCore;


namespace lucky_draw.Services
{
    public class LuckyDrawService
    {
        private readonly LuckyDrawDbContext _context;
        private readonly Random _rand = new();

        public LuckyDrawService(LuckyDrawDbContext context)
        {
            _context = context;
        }

        // Chọn ngẫu nhiên tuần tự
        public async Task<Customer> RandomSequentialAsync(int rewardId)
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            var candidateQuery = _context.Customers.Where(x => !_context.CustomerReward.Any(cr => cr.CustomerId == x.Id && cr.RewardId == rewardId));
            int count = await candidateQuery.CountAsync();
            if (count == 0) return null;
            int index = _rand.Next(count);
            var customer = await candidateQuery.Skip(index).FirstOrDefaultAsync();
            return customer;
        }

        // Chọn ngẫu nhiên liên tục (trả về nhiều)
        public async Task<List<Customer>> RandomContinuousAsync(int rewardId, int takeCount)
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            var candidateQuery = _context.Customers.Where(x => !_context.CustomerReward.Any(cr => cr.CustomerId == x.Id && cr.RewardId == rewardId));
            int count = await candidateQuery.CountAsync();
            var indices = Enumerable.Range(0, count).OrderBy(x => _rand.Next()).Take(takeCount).ToList();
            var customers = await candidateQuery.Skip(indices.First()).Take(takeCount).ToListAsync();
            return customers;
        }
    }
}
