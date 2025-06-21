using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ⬅️ ضروري
using Soccer_IQ.Models;
using Soccer_IQ.Repository.IRepository;

namespace Soccer_IQ.Controllers
{
    [Authorize] // ✅ كده كله محمي
    [ApiController]
    [Route("api/player-stats")]
    public class PlayerStatPredictionController : ControllerBase
    {
        private readonly IRepository<PLayerStat> _statRepo;
        private readonly PredictionService _ai;

        public PlayerStatPredictionController(
            IRepository<PLayerStat> statRepo,
            PredictionService ai)
        {
            _statRepo = statRepo;
            _ai = ai;
        }

        // ───────── 1️⃣ تنبؤ لسِجلّ واحد ─────────
        [HttpPost("{id}/predict")]
        public async Task<IActionResult> PredictOne(int id)
        {
            var stat = _statRepo.GetOne(null, s => s.Id == id, tracked: true);
            if (stat == null) return NotFound();

            await PredictAndSaveAsync(stat);
            return Ok(new
            {
                stat.Id,
                stat.PlayerId,
                stat.PredictedGoals,
                stat.PredictedAssists,
                stat.TotalPredictedContributions
            });
        }

        // ───────── 2️⃣ تنبؤ لكل السجلات ─────────
        [HttpPost("predict-all")]
        public async Task<IActionResult> PredictAll()
        {
            var stats = _statRepo.GetAll(null, null, tracked: true).ToList();
            foreach (var s in stats)
                await PredictAndSaveAsync(s);

            _statRepo.Commit();
            return Ok(new { updated = stats.Count });
        }

        // ========== الدالة المساعدة ==========
        private async Task PredictAndSaveAsync(PLayerStat stat)
        {
            stat.PredictedGoals = await _ai.PredictAsync(stat, "goals");
            stat.PredictedAssists = await _ai.PredictAsync(stat, "assists");
            _statRepo.Edit(stat);
            _statRepo.Commit();
        }
    }
}
