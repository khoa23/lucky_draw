using System.Security.Cryptography;
using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace lucky_draw.Services
{
    public enum SpinStatus
    {
        Ok,
        RewardNotFound,
        NoPrizeLeft,
        NoCandidates
    }

    public record SpinWinner(string? CustomerCode, string Name);

    public record SpinResult
    {
        public SpinStatus Status { get; init; }
        public string? Message { get; init; }
        public string? RewardName { get; init; }

        // Danh sách người trúng của lượt quay (1 người, hoặc nhiều người với loại giải quay tất cả)
        public IReadOnlyList<SpinWinner> Winners { get; init; } = Array.Empty<SpinWinner>();

        // true: kết quả của lần quay trước cùng mã lệnh (màn hình khác đã quay), không phải lượt quay mới
        public bool FromCache { get; init; }

        public static SpinResult Fail(SpinStatus status, string message) => new() { Status = status, Message = message };
    }

    public class LuckyDrawService
    {
        // Loại giải "Chọn ngẫu nhiên liên tục" (bảng RewardTypes): 1 lần quay ra toàn bộ số giải còn lại
        public const byte ContinuousRandomRewardType = 3;

        // Số vòng bốc Id ngẫu nhiên trước khi chuyển sang cách dự phòng (chỉ xảy ra khi gần hết người đủ điều kiện)
        private const int MaxRandomRounds = 64;

        // Số Id ngẫu nhiên tra cứu trong 1 câu truy vấn
        private const int CandidateBatchSize = 500;

        // Chỉ cho phép 1 lượt quay tại một thời điểm => không bao giờ vượt số lượng giải hoặc trùng người trúng
        private static readonly SemaphoreSlim SpinLock = new(1, 1);

        private readonly LuckyDrawDbContext _context;
        private readonly IMemoryCache _cache;

        public LuckyDrawService(LuckyDrawDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Quay số: chọn người trúng + lưu kết quả.
        // requestId giúp nhiều màn hình cùng nhận lệnh từ Remote chỉ tạo ra 1 kết quả duy nhất (hiển thị giống nhau).
        public async Task<SpinResult> SpinAsync(int rewardId, string? requestId, CancellationToken cancellationToken = default)
        {
            var cacheKey = string.IsNullOrWhiteSpace(requestId) ? null : "spin:" + requestId;
            if (cacheKey != null && _cache.TryGetValue(cacheKey, out SpinResult? cached) && cached != null)
                return cached with { FromCache = true };

            await SpinLock.WaitAsync(cancellationToken);
            try
            {
                if (cacheKey != null && _cache.TryGetValue(cacheKey, out cached) && cached != null)
                    return cached with { FromCache = true };

                // Đã vào vùng khóa thì chạy trọn vẹn, không hủy giữa chừng khi client ngắt kết nối
                var result = await SpinCoreAsync(rewardId);

                if (cacheKey != null && result.Status == SpinStatus.Ok)
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

                return result;
            }
            finally
            {
                SpinLock.Release();
            }
        }

        private async Task<SpinResult> SpinCoreAsync(int rewardId)
        {
            var reward = await _context.Rewards.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rewardId);
            if (reward == null)
                return SpinResult.Fail(SpinStatus.RewardNotFound, "Không tìm thấy giải thưởng!");

            var winsCount = await _context.CustomerReward.CountAsync(cr => cr.RewardId == rewardId);
            var remaining = reward.NumberOfReward - winsCount;
            if (remaining <= 0)
                return SpinResult.Fail(SpinStatus.NoPrizeLeft, "Đã hết lượt quay cho giải thưởng này!");

            var takeCount = reward.RewardType == ContinuousRandomRewardType ? remaining : 1;
            var winners = await PickRandomEligibleCustomersAsync(takeCount);
            if (winners.Count == 0)
                return SpinResult.Fail(SpinStatus.NoCandidates, "Không còn khách hàng nào thỏa mãn điều kiện!");

            // Lưu tất cả người trúng trong 1 lần SaveChanges (1 transaction)
            var now = DateTime.Now;
            _context.CustomerReward.AddRange(winners.Select(winner => new CustomerReward
            {
                ProgramId = reward.ProgramId,
                RewardId = reward.Id,
                CustomerId = winner.Id,
                ResultCode = winner.CustomerCode,
                TimeStamp = now,
                NumberOfCustomer = 1
            }));
            await _context.SaveChangesAsync();

            return new SpinResult
            {
                Status = SpinStatus.Ok,
                RewardName = reward.RewardName,
                Winners = winners.Select(w => new SpinWinner(w.CustomerCode, w.Name)).ToList()
            };
        }

        // Chọn ngẫu nhiên "count" khách hàng khác nhau CHƯA trúng giải nào, phân phối đều.
        // Cách cũ (COUNT + OFFSET trên toàn bảng) phải quét hàng triệu dòng mỗi lượt quay.
        // Cách mới: bốc ngẫu nhiên các Id trong [MinId, MaxId] rồi tra theo khóa chính (index seek),
        // Id không tồn tại / đã trúng / đã bốc thì bỏ qua và bốc tiếp (rejection sampling)
        // => mọi người đủ điều kiện có cơ hội như nhau.
        private async Task<List<Customer>> PickRandomEligibleCustomersAsync(int count)
        {
            var picked = new List<Customer>(count);
            var pickedIds = new HashSet<int>();

            var minId = await _context.Customers.MinAsync(c => (int?)c.Id);
            var maxId = await _context.Customers.MaxAsync(c => (int?)c.Id);
            if (minId == null || maxId == null) return picked;

            var eligible = _context.Customers.AsNoTracking()
                .Where(c => !_context.CustomerReward.Any(cr => cr.CustomerId == c.Id));

            if (maxId.Value < int.MaxValue)
            {
                for (int round = 0; round < MaxRandomRounds && picked.Count < count; round++)
                {
                    // Bốc dư ra để bù cho các Id trượt
                    int batchSize = Math.Min(CandidateBatchSize, (count - picked.Count) * 2 + 8);
                    var candidateIds = new int[batchSize];
                    for (int i = 0; i < batchSize; i++)
                        candidateIds[i] = RandomNumberGenerator.GetInt32(minId.Value, maxId.Value + 1);

                    var lookupIds = candidateIds.Distinct().ToList();
                    var found = await eligible.Where(c => lookupIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);

                    // Nhận theo đúng thứ tự đã bốc để giữ tính ngẫu nhiên
                    foreach (var id in candidateIds)
                    {
                        if (picked.Count == count) break;
                        if (found.TryGetValue(id, out var customer) && pickedIds.Add(id))
                            picked.Add(customer);
                    }
                }
            }

            // Dự phòng khi dải Id quá thưa hoặc gần hết người đủ điều kiện: xáo trộn ngay trong SQL
            if (picked.Count < count)
            {
                var need = count - picked.Count;
                var extra = await eligible.OrderBy(c => Guid.NewGuid()).Take(need + pickedIds.Count).ToListAsync();
                picked.AddRange(extra.Where(c => pickedIds.Add(c.Id)).Take(need));
            }

            return picked;
        }
    }
}
