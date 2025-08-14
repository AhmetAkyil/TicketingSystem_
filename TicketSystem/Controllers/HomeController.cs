using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

    
public async Task<IActionResult> Index(
    int assignedPage = 1,
    int createdPage = 1,
    string tab = "assigned",
    string? aq = null,   // assigned search
    string? cq = null,   // created search
    int? asf = null,     // assigned status filter (int)
    int? csf = null      // created status filter (int)
)
    {
        var email = HttpContext.Session.GetString("email");
        if (string.IsNullOrEmpty(email))
            return RedirectToAction("LoginForm", "Auth");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return RedirectToAction("LoginForm", "Auth");

        const int pageSize = 10;

        // Base sorgular
        var assignedQuery = _context.Tickets
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToUserId == user.UserId);

        var createdQuery = _context.Tickets
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.CreatedByUserId == user.UserId);

        // Arama
        if (!string.IsNullOrWhiteSpace(aq))
        {
            var p = $"%{aq.Trim()}%";
            assignedQuery = assignedQuery.Where(t =>
                EF.Functions.Like(t.Title, p) || EF.Functions.Like(t.Description, p));
        }
        if (!string.IsNullOrWhiteSpace(cq))
        {
            var p = $"%{cq.Trim()}%";
            createdQuery = createdQuery.Where(t =>
                EF.Functions.Like(t.Title, p) || EF.Functions.Like(t.Description, p));
        }

        
        if (asf.HasValue)
        {
            int s = asf.Value;
            assignedQuery = assignedQuery.Where(t => (int)t.Status == s);
        }
        if (csf.HasValue)
        {
            int s = csf.Value;
            createdQuery = createdQuery.Where(t => (int)t.Status == s);
        }

        
        int assignedCount = await assignedQuery.CountAsync();
        int createdCount = await createdQuery.CountAsync();

        int assignedTotalPages = Math.Max(1, (int)Math.Ceiling(assignedCount / (double)pageSize));
        int createdTotalPages = Math.Max(1, (int)Math.Ceiling(createdCount / (double)pageSize));

        
        assignedPage = Math.Min(Math.Max(1, assignedPage), assignedTotalPages);
        createdPage = Math.Min(Math.Max(1, createdPage), createdTotalPages);

        
        var assignedTickets = await assignedQuery
            .OrderByDescending(t => t.CreatedDate) 
            .Skip((assignedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var createdTickets = await createdQuery
            .OrderByDescending(t => t.CreatedDate)
            .Skip((createdPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        
        ViewData["Email"] = user.Email;
        ViewData["Assigned"] = assignedTickets;
        ViewData["Created"] = createdTickets;

        ViewData["AssignedPage"] = assignedPage;
        ViewData["CreatedPage"] = createdPage;
        ViewData["AssignedTotalPages"] = assignedTotalPages;
        ViewData["CreatedTotalPages"] = createdTotalPages;

        ViewData["AssignedQuery"] = aq ?? "";
        ViewData["CreatedQuery"] = cq ?? "";
        ViewData["AssignedStatusFilter"] = asf; 
        ViewData["CreatedStatusFilter"] = csf; 

        ViewData["ActiveTab"] = string.IsNullOrWhiteSpace(tab) ? "assigned" : tab;

        return View();
    }




    public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
