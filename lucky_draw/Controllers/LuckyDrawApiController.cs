using System;
using lucky_draw.Data;
using lucky_draw.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class LuckyDrawApiController : ControllerBase
    {
        private readonly LuckyDrawDbContext _context;

        public LuckyDrawApiController(LuckyDrawDbContext context)
        {
            _context = context;
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
            var winners = await _context.CustomerReward
                .Include(cr => cr.Reward)
                .OrderBy(cr => cr.Reward.Idd)
                .ThenByDescending(cr => cr.TimeStamp)
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

            // Improved query to avoid 'Contains' which can cause issues on older SQL Servers (JSON translation)
            var candidates = await _context.Customers
                .Where(c => !_context.CustomerReward.Any(cr => cr.RewardId == request.RewardId && cr.CustomerId == c.Id))
                .ToListAsync();

            if (candidates.Count == 0)
                return BadRequest("No candidates left.");

            var rnd = new Random();
            var winner = candidates[rnd.Next(candidates.Count)];

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
