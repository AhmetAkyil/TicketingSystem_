using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Services;

namespace TicketSystem.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRecaptchaService _recaptcha;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IRecaptchaService recaptcha, IConfiguration config)
        {
            _context = context;
            _recaptcha = recaptcha;
            _config = config;
        }

        [HttpGet("login")]
        public IActionResult LoginForm()
        {
            ViewBag.SiteKey = _config["GoogleReCaptcha:SiteKey"];
            return View("Login"); // Views/Auth/Login.cshtml
        }

        [EnableRateLimiting("LoginPolicy")]

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm(Name = "g-recaptcha-response")] string recaptchaToken)
        {
            ViewBag.SiteKey = _config["GoogleReCaptcha:SiteKey"];

            // reCAPTCHA kontrolü
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var captchaOk = await _recaptcha.VerifyAsync(recaptchaToken, remoteIp);
            if (!captchaOk)
            {
                ViewData["Error"] = "Lütfen robot doğrulamasını tamamlayın.";
                return View("Login");
            }

            Console.WriteLine($"Login denemesi: {email} / {password}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".TicketSystem.Session"); 

            
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("role", user.Role.ToString());

            
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("login-open")]
        public async Task<IActionResult> LoginOpen([FromForm] string email, [FromForm] string password)
        {
            Console.WriteLine($"[LoginOpen] Deneme: {email} / {password}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("role", user.Role.ToString());
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("login-insecure")]
        public async Task<IActionResult> LoginInsecure([FromForm] string email, [FromForm] string password)
        {
            var sql = $"SELECT * FROM Users WHERE Email = '{email}' AND Password = '{password}'";
            var userList = await _context.Users.FromSqlRaw(sql).ToListAsync();
            var user = userList.OrderByDescending(x => x.UserId).FirstOrDefault();

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("role", user.Role.ToString());
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".TicketSystem.Session"); 
            return RedirectToAction("LoginForm", "Auth");
        }

    }
}
