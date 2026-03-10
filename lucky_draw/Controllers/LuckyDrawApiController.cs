using System;
using lucky_draw.Data;
using lucky_draw.Models;
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

        public LuckyDrawApiController(LuckyDrawDbContext context, LuckyDrawService drawService)
        {
            _context = context;
            _drawService = drawService;
        }

        [HttpGet("programs")]
        public async Task<IActionResult> GetPrograms()
        {
            var programs = await _context.Programs.ToListAsync();
            return Ok(programs);
        }

        [HttpGet("prizes/{programId}")]
        public async Task<IActionResult> GetPrizes(int programId)
        {
            // Tối ưu hóa việc đếm bằng cách dùng truy vấn đếm trực tiếp
            var rewards = await _context.Rewards
                .Where(r => r.ProgramId == programId)
                .OrderBy(r => r.Idd)
                .Select(r => new
                {
                    r.Id,
                    r.RewardName,
                    r.NumberOfReward,
                    TotalWins = _context.CustomerReward.Count(cr => cr.RewardId == r.Id),
                    Remaining = r.NumberOfReward - _context.CustomerReward.Count(cr => cr.RewardId == r.Id)
                })
                .ToListAsync();

            return Ok(rewards);
        }

        [HttpGet("winners")]
        public async Task<IActionResult> GetWinners()
        {
            // Chỉ lấy top 100 người trúng mới nhất để tránh load quá nhiều
            var winners = await _context.CustomerReward
                .Include(cr => cr.Reward)
                .OrderByDescending(cr => cr.TimeStamp)
                .Take(100) 
                .Select(cr => new
                {
                    cr.Id,
                    cr.RewardId,
                    RewardName = cr.Reward.RewardName,
                    cr.ResultCode,
                    CustomerName = _context.Customers.Where(c => c.Id == cr.CustomerId).Select(c => c.Name).FirstOrDefault(),
                    cr.TimeStamp
                })
                .ToListAsync();

            return Ok(winners);
        }

        [HttpPost("spin")]
        public async Task<IActionResult> Spin([FromBody] SpinRequest request)
        {
            var reward = await _context.Rewards.FindAsync(request.RewardId);
            if (reward == null) return NotFound("Reward not found");

            var winsCount = await _context.CustomerReward.CountAsync(cr => cr.RewardId == request.RewardId);
            if (winsCount >= reward.NumberOfReward)
            {
                return BadRequest("No more prizes available for this reward.");
            }

            // Dùng service đã tối ưu cho DB lớn
            var winner = await _drawService.RandomSequentialAsync(request.RewardId);

            if (winner == null)
                return BadRequest("No candidates left.");

            // Lưu kết quả
            await _drawService.SaveWinnerAsync(request.RewardId, winner);

            var info = await _context.CustomerInfos.FirstOrDefaultAsync(x => x.CustomerCode == winner.CustomerCode);

            return Ok(new
            {
                winner.CustomerCode,
                winner.Name,
                IdNo = info?.IdNo,
                TelNo = info?.TelNo,
                RewardName = reward.RewardName
            });
        }
    }

    public class SpinRequest
    {
        public int RewardId { get; set; }
    }
}

