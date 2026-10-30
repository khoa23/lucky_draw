using lucky_draw.Services;
using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    public class ParticipantsViewModel
    {
        public required ParticipantStats Stats { get; init; }
        public required ParticipantPage Page { get; init; }
        public required ParticipantFilter Filter { get; init; }
        public string? Query { get; init; }
        public int PageSize { get; init; }
        public int PageNumber { get; init; }
        public long? TotalPages { get; init; } // null khi số kết quả tìm kiếm quá lớn (không đếm hết)
    }

    // Danh sách khách hàng tham gia quay số (minh bạch số người đủ điều kiện)
    public class ParticipantsController : Controller
    {
        private static readonly int[] PageSizes = { 50, 100, 200 };
        private const int MaxQueryLength = 30;

        private readonly ParticipantService _participants;

        public ParticipantsController(ParticipantService participants)
        {
            _participants = participants;
        }

        // after/before: khóa của dòng cuối/đầu trang hiện tại (Id, hoặc mã khách hàng khi đang tìm kiếm)
        [HttpGet]
        public async Task<IActionResult> Index(string? filter, string? q, int size = 50, string? after = null, string? before = null,
            bool last = false, int page = 1)
        {
            var parsedFilter = filter?.ToLowerInvariant() switch
            {
                "eligible" => ParticipantFilter.Eligible,
                "won" => ParticipantFilter.Won,
                _ => ParticipantFilter.All
            };
            if (!PageSizes.Contains(size)) size = PageSizes[0];
            // Mã khách hàng chỉ gồm chữ, số, gạch ngang: bỏ mọi ký tự khác (kể cả ký tự đại diện % _ [ của LIKE)
            q = string.IsNullOrWhiteSpace(q) ? null : new string(q.Trim().Where(ch => char.IsAsciiLetterOrDigit(ch) || ch == '-').ToArray());
            if (string.IsNullOrEmpty(q)) q = null;
            else if (q.Length > MaxQueryLength) q = q[..MaxQueryLength];

            var result = await _participants.GetPageAsync(parsedFilter, q, size, after, before, last);
            var stats = await _participants.GetStatsAsync();

            long? totalPages = result.MatchCountCapped ? null : Math.Max(1, (result.MatchCount + size - 1) / size);
            var pageNumber = last && totalPages != null ? (int)totalPages.Value : Math.Max(1, page);

            return View(new ParticipantsViewModel
            {
                Stats = stats,
                Page = result,
                Filter = parsedFilter,
                Query = q,
                PageSize = size,
                PageNumber = pageNumber,
                TotalPages = totalPages
            });
        }
    }
}
