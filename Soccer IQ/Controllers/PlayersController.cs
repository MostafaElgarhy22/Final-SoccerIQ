using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ✅ ضروري للحماية
using Soccer_IQ.Models;
using Soccer_IQ.Repository.IRepository;

namespace Soccer_IQ.Controllers
{
    [Authorize] // ✅ حماية كل الـ endpoints هنا
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly IRepository<Player> _playerRepo;

        public PlayersController(IRepository<Player> playerRepo)
        {
            _playerRepo = playerRepo;
        }

        [HttpGet]
        public IActionResult GetAllPlayers()
        {
            var players = _playerRepo.GetAll();
            return Ok(players);
        }

        [HttpGet("{id}")]
        public IActionResult GetPlayer(int id)
        {
            var player = _playerRepo.GetOne(
                includeProps: new Expression<Func<Player, object>>[]
                {
                    p => p.Club
                },
                expression: p => p.Id == id
            );

            if (player == null) return NotFound();
            return Ok(player);
        }

        [HttpPost]
        public IActionResult CreatePlayer([FromBody] Player player)
        {
            if (player == null) return BadRequest();

            _playerRepo.Create(player);
            _playerRepo.Commit();

            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }

        [HttpPut("{id}")]
        public IActionResult UpdatePlayer(int id, [FromBody] Player updatedPlayer)
        {
            if (updatedPlayer == null || id != updatedPlayer.Id)
                return BadRequest();

            var existing = _playerRepo.GetOne(null, p => p.Id == id, tracked: true);
            if (existing == null) return NotFound();

            existing.Name = updatedPlayer.Name;
            existing.PhotoUrl = updatedPlayer.PhotoUrl;
            existing.Position = updatedPlayer.Position;
            existing.Club = updatedPlayer.Club;

            _playerRepo.Edit(existing);
            _playerRepo.Commit();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePlayer(int id)
        {
            var player = _playerRepo.GetOne(null, p => p.Id == id, tracked: true);
            if (player == null) return NotFound();

            _playerRepo.Delete(player);
            _playerRepo.Commit();

            return NoContent();
        }
    }
}
