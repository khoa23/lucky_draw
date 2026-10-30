using lucky_draw.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace lucky_draw.Services
{
    public enum ParticipantFilter
    {
        All,
        Eligible, // Chưa trúng giải nào => còn được quay
        Won
    }

    public record ParticipantStats(long Total, int Won)
    {
        public long Eligible => Math.Max(0, Total - Won);
    }

    public record ParticipantRow(int Id, string? CustomerCode, string Name, string? Branch, string? RewardName, DateTime? WonAt);

    public record ParticipantPage(
        IReadOnlyList<ParticipantRow> Rows,
        bool HasPrevious,
        bool HasNext,
        long MatchCount,
        bool MatchCountCapped);

    // Số liệu và danh sách khách hàng tham gia, tối ưu cho bảng Customers hàng chục triệu dòng
    public class ParticipantService
    {
        private const string TotalCacheKey = "participants:total";

        // Đếm chính xác tối đa đến mức này khi tìm kiếm, nhiều hơn thì hiển thị "hơn ..." để không quét cả bảng
        public const int MaxExactSearchCount = 10_000;

        private readonly LuckyDrawDbContext _context;
        private readonly IMemoryCache _cache;

        public ParticipantService(LuckyDrawDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Gọi sau khi import để đếm lại tổng số khách
        public static void InvalidateTotal(IMemoryCache cache) => cache.Remove(TotalCacheKey);

        public async Task<ParticipantStats> GetStatsAsync()
        {
            // Đếm 10 triệu dòng mất khoảng 1 giây => chỉ đếm 1 lần rồi lưu tạm (danh sách chỉ đổi khi import)
            var total = await _cache.GetOrCreateAsync(TotalCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _context.Customers.LongCountAsync();
            });

            // Số người đã trúng nhỏ (bằng tổng số giải) => đếm trực tiếp mỗi lần, luôn chính xác
            var won = await _context.CustomerReward
                .Where(cr => cr.CustomerId != null)
                .Select(cr => cr.CustomerId)
                .Distinct()
                .CountAsync();

            return new ParticipantStats(total, won);
        }

        // Phân trang keyset (WHERE khóa > ... / khóa < ...): mọi trang đều nhanh như nhau,
        // không như OFFSET càng về cuối càng chậm vì phải bỏ qua hàng triệu dòng.
        // - Không tìm kiếm: sắp và phân trang theo Id (khóa chính).
        // - Có tìm kiếm theo mã: sắp và phân trang theo CustomerCode để SQL Server tra thẳng index IX_Customers_CustomerCode
        //   (sắp theo Id thì SQL phải quét cả bảng 10 triệu dòng để lọc mã). Mã khách hàng là duy nhất trong Customers.
        public async Task<ParticipantPage> GetPageAsync(
            ParticipantFilter filter, string? codePrefix, int pageSize, string? after, string? before, bool lastPage)
        {
            var byCode = !string.IsNullOrEmpty(codePrefix);
            int? afterId = null, beforeId = null;
            if (!byCode)
            {
                if (int.TryParse(after, out var a)) afterId = a;
                if (int.TryParse(before, out var b)) beforeId = b;
            }

            var query = _context.Customers.AsNoTracking();
            query = filter switch
            {
                ParticipantFilter.Eligible => query.Where(c => !_context.CustomerReward.Any(cr => cr.CustomerId == c.Id)),
                ParticipantFilter.Won => query.Where(c => _context.CustomerReward.Any(cr => cr.CustomerId == c.Id)),
                _ => query
            };
            if (!string.IsNullOrEmpty(codePrefix))
            {
                // Chỉ dùng "LIKE @p" để SQL Server tra index IX_Customers_CustomerCode (~1 ms thay vì ~1,5 giây):
                // - Không dùng StartsWith(): EF sinh thêm ESCAPE N'' (Unicode) làm mất tác dụng index trên cột varchar.
                // - Không thêm "CustomerCode != null": SQL Server sẽ seek trên toàn bộ dải khác NULL rồi mới lọc LIKE.
                //   LIKE vốn không khớp NULL nên điều kiện này thừa.
                // codePrefix đã được lọc chỉ còn chữ/số/gạch ngang nên không chứa ký tự đại diện, không cần ESCAPE.
                var pattern = codePrefix + "%";
                query = query.Where(c => EF.Functions.Like(c.CustomerCode!, pattern));
            }

            List<CustomerKey> rows;
            bool hasPrevious, hasNext;
            var goBackward = (byCode ? before != null : beforeId != null) || lastPage;
            if (goBackward)
            {
                // Lùi trang / trang cuối: lấy ngược rồi đảo lại
                IQueryable<Models.Customer> backward;
                if (byCode)
                {
                    backward = before != null ? query.Where(c => c.CustomerCode!.CompareTo(before) < 0) : query;
                    backward = backward.OrderByDescending(c => c.CustomerCode);
                }
                else
                {
                    backward = beforeId != null ? query.Where(c => c.Id < beforeId) : query;
                    backward = backward.OrderByDescending(c => c.Id);
                }
                rows = await backward.Take(pageSize + 1)
                    .Select(c => new CustomerKey(c.Id, c.CustomerCode, c.Name)).ToListAsync();
                hasPrevious = rows.Count > pageSize;
                rows = rows.Take(pageSize).Reverse().ToList();
                hasNext = !lastPage;
            }
            else
            {
                IQueryable<Models.Customer> forward;
                if (byCode)
                {
                    forward = after != null ? query.Where(c => c.CustomerCode!.CompareTo(after) > 0) : query;
                    forward = forward.OrderBy(c => c.CustomerCode);
                }
                else
                {
                    forward = afterId != null ? query.Where(c => c.Id > afterId) : query;
                    forward = forward.OrderBy(c => c.Id);
                }
                rows = await forward.Take(pageSize + 1)
                    .Select(c => new CustomerKey(c.Id, c.CustomerCode, c.Name)).ToListAsync();
                hasNext = rows.Count > pageSize;
                rows = rows.Take(pageSize).ToList();
                hasPrevious = byCode ? after != null : afterId != null;
            }

            // Thông tin bổ sung chỉ cho các dòng của trang hiện tại (tối đa vài trăm dòng)
            var ids = rows.Select(r => r.Id).ToList();
            var codes = rows.Where(r => r.CustomerCode != null).Select(r => r.CustomerCode!).Distinct().ToList();
            var wins = await _context.CustomerReward.AsNoTracking()
                .Where(cr => cr.CustomerId != null && ids.Contains(cr.CustomerId.Value))
                .Select(cr => new { CustomerId = cr.CustomerId!.Value, cr.Reward.RewardName, cr.TimeStamp })
                .ToListAsync();
            var winByCustomer = wins.GroupBy(w => w.CustomerId).ToDictionary(g => g.Key, g => g.First());
            var branches = await _context.CustomerInfos.AsNoTracking()
                .Where(ci => codes.Contains(ci.CustomerCode))
                .Select(ci => new { ci.CustomerCode, ci.BranchName })
                .ToListAsync();
            var branchByCode = branches.GroupBy(b => b.CustomerCode).ToDictionary(g => g.Key, g => g.First().BranchName);

            var result = rows.Select(r =>
            {
                winByCustomer.TryGetValue(r.Id, out var win);
                string? branch = null;
                if (r.CustomerCode != null) branchByCode.TryGetValue(r.CustomerCode, out branch);
                return new ParticipantRow(r.Id, r.CustomerCode, r.Name, branch, win?.RewardName, win?.TimeStamp);
            }).ToList();

            var (matchCount, capped) = await CountMatchesAsync(query, filter, codePrefix);
            return new ParticipantPage(result, hasPrevious, hasNext, matchCount, capped);
        }

        private async Task<(long Count, bool Capped)> CountMatchesAsync(
            IQueryable<Models.Customer> query, ParticipantFilter filter, string? codePrefix)
        {
            if (string.IsNullOrEmpty(codePrefix))
            {
                // Không tìm kiếm: lấy từ số liệu đã lưu tạm, không quét bảng
                var stats = await GetStatsAsync();
                return filter switch
                {
                    ParticipantFilter.Eligible => (stats.Eligible, false),
                    ParticipantFilter.Won => (stats.Won, false),
                    _ => (stats.Total, false)
                };
            }

            // Tìm kiếm: chỉ đếm tới MaxExactSearchCount + 1 dòng
            var count = await query.Take(MaxExactSearchCount + 1).CountAsync();
            return count > MaxExactSearchCount ? (MaxExactSearchCount, true) : (count, false);
        }

        private record CustomerKey(int Id, string? CustomerCode, string Name);
    }
}
