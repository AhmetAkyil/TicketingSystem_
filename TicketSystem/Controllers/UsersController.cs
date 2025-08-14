using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;
using TicketSystem.Services;

namespace TicketSystem.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email)) return false;

            var role = _context.Users
                .AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => u.Role)
                .FirstOrDefault();

            return role == UserRoles.Admin;
        }


        private IActionResult? Gate()
        {
            if (!IsAdmin())
                return Forbid(); 
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var gate = Gate(); if (gate != null) return gate;
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Details(long? id)
        {
            var gate = Gate(); if (gate != null) return gate;
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult Create()
        {
            var gate = Gate(); if (gate != null) return gate;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            var gate = Gate(); if (gate != null) return gate;

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
  
        [HttpPost]
        [Route("users/create-auto")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateAuto(string firstName, string lastName, string role)
        {
            //var gate = Gate(); if (gate != null) return gate;

            var generator = new AccountCreationService();
            var email = generator.GenerateEmail(firstName, lastName);
            var password = generator.GeneratePassword();

            if (_context.Users.Any(u => u.Email == email))
            {
                return BadRequest("Bu e-posta zaten kullanılıyor.");
            }

            var user = new User
            {
                Email = email,
                Password = password,
                Role = Enum.Parse<UserRoles>(role, ignoreCase: true)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Kullanıcı başarıyla oluşturuldu.",
                Email = email,
                TemporaryPassword = password
            });
        }

        public async Task<IActionResult> Edit(long? id)
        {
            var gate = Gate(); if (gate != null) return gate;
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("UserId,Email,Role")] User user)
        {
            var gate = Gate(); if (gate != null) return gate; 
            if (id != user.UserId) return NotFound();

            if (!ModelState.IsValid)
                return View(user);

            
            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();

            
            entity.Email = user.Email;
            entity.Role = user.Role;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Örn. Email unique ihlali
                ModelState.AddModelError("", "Güncelleme başarısız. Email zaten kullanılıyor olabilir.");
                return View(user);
            }
        }


        public async Task<IActionResult> Delete(long? id)
        {
            var gate = Gate(); if (gate != null) return gate;
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var gate = Gate(); if (gate != null) return gate;

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
