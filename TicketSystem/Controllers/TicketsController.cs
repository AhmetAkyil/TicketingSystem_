using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
                return false;

            
            var role = _context.Users
                .AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => u.Role)
                .FirstOrDefault(); // email unique 
            return role == UserRoles.Admin;
        }
        private async Task<(long UserId, UserRoles Role)?> GetCurrentAsync()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email)) return null;

            var me = await _context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();

            return me == null ? null : (me.UserId, me.Role);
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            
            if (!IsAdmin())
                return RedirectToAction(nameof(Create));

            var tickets = await _context.Tickets
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .ToListAsync();

            return View(tickets);
        }

        // GET: Tickets/Details/id
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var me = await GetCurrentAsync();
            if (me == null) return RedirectToAction("LoginForm", "Auth");

            bool isAdmin = me.Value.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.Value.UserId || t.AssignedToUserId == me.Value.UserId))
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)           
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound(); 
            return View(ticket);
        }


        // GET: Tickets/Create
        public async Task<IActionResult> Create()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("LoginForm", "Auth");

            ViewBag.Users = new SelectList(
                await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                nameof(TicketSystem.Models.User.UserId),
                nameof(TicketSystem.Models.User.Email)
            );

            return View(new Ticket { Status = TicketStatus.Open });
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,AssignedToUserId")] Ticket ticket)
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("LoginForm", "Auth");

            var me = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
            if (me == null) return Unauthorized();

            
            if (ticket.AssignedToUserId.HasValue)
            {
                bool assigneeExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == ticket.AssignedToUserId.Value);
                if (!assigneeExists)
                    ModelState.AddModelError(nameof(ticket.AssignedToUserId), "Atanacak kullanıcı bulunamadı.");
            }

            ticket.CreatedByUserId = me.UserId;
            ticket.CreatedDate = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(
                    await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                    nameof(TicketSystem.Models.User.UserId),
                    nameof(TicketSystem.Models.User.Email),
                    ticket.AssignedToUserId
                );
                return View(ticket);
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }


        // GET: Tickets/Edit/ticketId
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("LoginForm", "Auth");

            var me = await _context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();
            if (me == null) return RedirectToAction("LoginForm", "Auth");

            bool isAdmin = me.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.UserId ))
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound(); 

            ViewBag.Users = new SelectList(
                await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                nameof(TicketSystem.Models.User.UserId),
                nameof(TicketSystem.Models.User.Email),
                ticket.AssignedToUserId
            );

            return View(ticket);
        }


        // POST: Tickets/Edit/ticketId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("TicketId,Title,Description,Status,AssignedToUserId")] Ticket form)
        {
            if (id != form.TicketId) return NotFound();

            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("LoginForm", "Auth");

            var me = await _context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();
            if (me == null) return RedirectToAction("LoginForm", "Auth");

            bool isAdmin = me.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.UserId ))
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();

            // AssignedTo doğrulaması
            if (form.AssignedToUserId.HasValue)
            {
                bool assignedExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == form.AssignedToUserId.Value);
                if (!assignedExists)
                    ModelState.AddModelError(nameof(form.AssignedToUserId), "Atanacak kullanıcı bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(
                    await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                    nameof(TicketSystem.Models.User.UserId),
                    nameof(TicketSystem.Models.User.Email),
                    form.AssignedToUserId
                );
                return View(form);
            }

            
            ticket.Title = form.Title;
            ticket.Description = form.Description;
            ticket.Status = form.Status;
            ticket.AssignedToUserId = form.AssignedToUserId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Home");
        }


        // GET: Tickets/Delete/ticketId
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.TicketId == id);
            if (ticket == null) return NotFound();

            var currentEmail = HttpContext.Session.GetString("email");
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentEmail);
            if (currentUser == null) return RedirectToAction("Login", "Auth");

            if (currentUser.Role != UserRoles.Admin && ticket.CreatedByUserId != currentUser.UserId)
                return Forbid();

            return View(ticket);
        }

        // POST: Tickets/Delete/ticketId
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var currentEmail = HttpContext.Session.GetString("email");
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentEmail);
            if (currentUser == null) return RedirectToAction("Login", "Auth");

            if (currentUser.Role != UserRoles.Admin && ticket.CreatedByUserId != currentUser.UserId)
                return Forbid();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Home");
        }

        public async Task<IActionResult> MyTickets()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("LoginForm", "Auth");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized();

            var myTickets = await _context.Tickets
                .Where(t => t.CreatedByUserId == user.UserId)
                .ToListAsync();

            return View(myTickets);
        }
    }
}
