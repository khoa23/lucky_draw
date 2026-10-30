using System.Security.Claims;
using lucky_draw.Data;
using lucky_draw.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class LuckyDrawApiController : ControllerBase
    {
        private readonly LuckyDrawDbContext _context;
        private readonly LuckyDrawService _drawService;
        private readonly ParticipantService _participants;
        private readonly AuditLogger _audit;

        public LuckyDrawApiController(LuckyDrawDbContext context, LuckyDrawService drawService, ParticipantService participants,
            AuditLogger audit)
        {
            _context = context;
            _drawService = drawService;
            _participants = participants;
            _audit = audit;
        }

        // Số khách còn đủ điều kiện quay (chưa trúng giải nào): màn hình quay số khóa nút khi hết người
        [HttpGet("eligible")]
        public async Task<IActionResult> GetEligible()
        {
            var stats = await _participants.GetStatsAsync();
            return Ok(new { stats.Eligible });
        }

        [HttpGet("programs")]
        public async Task<IActionResult> GetPrograms()
        {
            // Màn hình quay số chỉ cần Id và tên chương trình
            var programs = await _context.Programs
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.ProgramName })
                .ToListAsync();
            return Ok(programs);
        }

        [HttpGet("prizes/{programId}")]
        public async Task<IActionResult> GetPrizes(int programId)
        {
            // Đếm số người đã trúng bằng 1 truy vấn con (dùng index IX_CustomerReward_RewardId)
            var rewards = await _context.Rewards
                .AsNoTracking()
                .Where(r => r.ProgramId == programId)
                .OrderBy(r => r.Idd)
                .Select(r => new
                {
                    r.Id,
                    r.RewardName,
                    r.NumberOfReward,
                    r.NumberLength,
                    r.RewardType,
                    TotalWins = _context.CustomerReward.Count(cr => cr.RewardId == r.Id)
                })
                .ToListAsync();

            return Ok(rewards.Select(r => new
            {
                r.Id,
                r.RewardName,
                r.NumberOfReward,
                r.NumberLength,
                r.TotalWins,
                // Loại giải "Chọn ngẫu nhiên liên tục": 1 lần bấm quay ra tất cả số giải còn lại
                SpinAll = r.RewardType == LuckyDrawService.ContinuousRandomRewardType,
                Remaining = Math.Max(0, r.NumberOfReward - r.TotalWins)
            }));
        }

        [HttpGet("winners")]
        public async Task<IActionResult> GetWinners()
        {
            // Số người trúng bị giới hạn bởi tổng số giải nên không lớn, nhưng vẫn chặn trên để an toàn
            var winners = await _context.CustomerReward
                .AsNoTracking()
                .OrderByDescending(cr => cr.TimeStamp)
                .Take(2000)
                .Select(cr => new
                {
                    cr.Id,
                    cr.RewardId,
                    RewardName = cr.Reward.RewardName,
                    cr.ResultCode,
                    CustomerName = cr.Customer != null ? cr.Customer.Name : null,
                    cr.TimeStamp
                })
                .ToListAsync();

            return Ok(winners);
        }

        [HttpPost("spin")]
        public async Task<IActionResult> Spin([FromBody] SpinRequest request, CancellationToken cancellationToken)
        {
            var result = await _drawService.SpinAsync(request.RewardId, request.RequestId, cancellationToken);
            await LogSpinAsync(request, result);

            return result.Status switch
            {
                // Chỉ trả mã + tên (không gửi CMND/SĐT ra trình duyệt vì giao diện không hiển thị)
                SpinStatus.Ok => Ok(new
                {
                    result.Winners[0].CustomerCode,
                    result.Winners[0].Name,
                    result.RewardName,
                    result.Winners
                }),
                SpinStatus.RewardNotFound => NotFound(result.Message),
                _ => BadRequest(result.Message)
            };
        }

        // Nhật ký mỗi lượt quay (Logs/spin-yyyyMMdd.log). Mã lệnh trùng với dòng TriggerSpin trong remote-*.log
        private Task LogSpinAsync(SpinRequest request, SpinResult result)
        {
            var source = string.Equals(request.Source, "remote", StringComparison.OrdinalIgnoreCase) ? "Remote" : "Web";
            // Dấu "|" là phân cách cột của file nhật ký nên trong phần chi tiết dùng ";"
            var outcome = result.Status == SpinStatus.Ok
                ? $"{result.Winners.Count} người trúng: " + string.Join(", ", result.Winners.Select(w => $"{w.CustomerCode} - {w.Name}"))
                : "Không quay được: " + result.Message;
            var detail = $"Nguồn: {source}; Giải: {result.RewardName ?? "#" + request.RewardId} (#{request.RewardId})" +
                         $"; Mã lệnh: {request.RequestId}" +
                         (result.FromCache ? "; Kết quả có sẵn (màn hình khác đã quay cùng lệnh, không quay thêm)" : "") +
                         $"; {outcome}";
            return _audit.LogAsync(AuditLogs.Spin, AuditEvents.Spin, HttpContext,
                User.FindFirst(ClaimTypes.Role)?.Value, detail);
        }
    }

    public class SpinRequest
    {
        public int RewardId { get; set; }
        public string? RequestId { get; set; }
        public string? Source { get; set; } // "web": bấm trên màn hình quay, "remote": lệnh từ Remote
    }
}

