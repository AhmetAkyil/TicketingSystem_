using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    public class CommentsController : Controller
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long ticketId, string commentText) 
        {
            
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var me = await _context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();
            if (me == null) return Unauthorized();

            
            var t = await _context.Tickets.AsNoTracking()
                .Where(x => x.TicketId == ticketId)
                .Select(x => new { x.TicketId, x.CreatedByUserId, x.AssignedToUserId })
                .FirstOrDefaultAsync();

            if (t == null) return NotFound();

            var isAdmin = me.Role == UserRoles.Admin;
            var isCreator = t.CreatedByUserId == me.UserId;
            var assignedToMe = t.AssignedToUserId != null && t.AssignedToUserId == me.UserId;

            if (!isAdmin && !isCreator && !assignedToMe)
                return Forbid();

            
            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction("Details", "Tickets", new { id = ticketId });
            }

            commentText = commentText.Trim();
            if (commentText.Length > 2000) 
                commentText = commentText.Substring(0, 2000);

            
            var comment = new Comment
            {
                TicketId = t.TicketId,
                userId = me.UserId,
                commentText = commentText,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Tickets", new { id = ticketId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var email = HttpContext.Session.GetString("email");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var me = await _context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.Role })
                .FirstOrDefaultAsync();
            if (me == null) return Unauthorized();

            var comment = await _context.Comments
                .Include(c => c.Ticket)
                .FirstOrDefaultAsync(c => c.commentId == id);

            if (comment == null) return NotFound();

            var isAdmin = me.Role == UserRoles.Admin;
            var isOwner = comment.userId == me.UserId;
            var isAssignedToMe = comment.Ticket?.AssignedToUserId != null
                             && comment.Ticket.AssignedToUserId == me.UserId;

            if (!isAdmin && !isOwner && !isAssignedToMe)
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }



        // GET: Comments/Edit/id
        public async Task<IActionResult> Edit(long id)
        {
            var comment = await _context.Comments
            .Include(c => c.Ticket)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.commentId == id);

            if (comment == null)
                return NotFound();


            var currentUserEmail = HttpContext.Session.GetString("email");
            if (comment.User.Email != currentUserEmail)
                return Forbid();

            return View(comment);
        }

        // POST: Comments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Comment updatedComment)
        {
            var comment = await _context.Comments.FindAsync(updatedComment.commentId);
            if (comment == null)
                return NotFound();

            var currentUserEmail = HttpContext.Session.GetString("email");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            if (user == null || comment.userId != user.UserId)
                return Forbid();

            comment.commentText = updatedComment.commentText;
            comment.CreatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }



    }
}
