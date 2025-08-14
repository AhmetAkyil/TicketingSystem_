using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data; // senin namespace'ine göre düzelt
using System.Linq;
using System.Threading.Tasks;

namespace TicketSystem.Controllers
{
    public class KanbanController : Controller
    {
        private readonly AppDbContext _context;
        public KanbanController(AppDbContext context) => _context = context;

        private async Task<long?> GetCurrentUserIdAsync()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email)) return null;
            var user = await _context.Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Email == email);
            return user?.UserId;
        }

        // GET /Kanban/Pins -> [1,5,7]
        [HttpGet]
        public async Task<IActionResult> Pins()
        {
            var uid = await GetCurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            var ids = await _context.KanbanPins
                .AsNoTracking()
                .Where(p => p.UserId == uid)
                .Select(p => p.TicketId)
                .ToListAsync();

            return Json(ids);
        }

        // POST /Kanban/Add  (body: ticketId=123)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long ticketId)
        {
            var uid = await GetCurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            var exists = await _context.KanbanPins.AnyAsync(p => p.UserId == uid && p.TicketId == ticketId);
            if (!exists)
            {
                _context.KanbanPins.Add(new Models.KanbanPin { UserId = uid.Value, TicketId = ticketId });
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST /Kanban/Remove (body: ticketId=123)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(long ticketId)
        {
            var uid = await GetCurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            var pin = await _context.KanbanPins.FirstOrDefaultAsync(p => p.UserId == uid && p.TicketId == ticketId);
            if (pin != null)
            {
                _context.KanbanPins.Remove(pin);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST /Kanban/Save  (body: ticketIds=1&ticketIds=5&ticketIds=7) -> set replace
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] long[] ticketIds)
        {
            var uid = await GetCurrentUserIdAsync();
            if (uid == null) return Unauthorized();

            var current = await _context.KanbanPins.Where(p => p.UserId == uid).ToListAsync();

            // sil
            _context.KanbanPins.RemoveRange(current.Where(p => !ticketIds.Contains(p.TicketId)));
            // ekle
            var toAdd = ticketIds.Distinct().Except(current.Select(p => p.TicketId)).ToList();
            foreach (var tid in toAdd)
                _context.KanbanPins.Add(new Models.KanbanPin { UserId = uid.Value, TicketId = tid });

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
