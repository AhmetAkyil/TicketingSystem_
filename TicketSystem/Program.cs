using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;
using TicketSystem.Data;
using TicketSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Login attempt servisi


// RateLimiter servisi
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        string retryInfo = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? $"Retry after {retryAfter} seconds"
            : "no retry info";

        Console.WriteLine("RATE LIMIT BLOCKED for key: " + retryInfo);

        var response = context.HttpContext.Response;
        response.StatusCode = 503;
        response.ContentType = "text/html";

        await response.WriteAsync(@"
        <html>
            <head><title>Too many attempts</title></head>
            <body style='font-family: Arial; text-align: center; padding-top: 100px;'>
                <h2>503 - Limit Exceeded</h2>
                <p>Please try again in 1 minute.</p>
                <a href='/Auth/Login'>Return to login page</a>
            </body>
        </html>
    ");
    };

    options.AddPolicy("LoginPolicy", context =>
    {
        var ip = context.Connection.RemoteIpAddress;
        var ipKey = ip?.MapToIPv4().ToString() ?? "unknown";

        // Sadece log için email 
        var email = context.Request.HasFormContentType
            ? context.Request.Form["email"].ToString().ToLowerInvariant()
            : null;

        Console.WriteLine($"[RATE] PartitionKey (IP): ip:{ipKey}");
        if (!string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine($"[RATE] Login denemesi yapılan e-posta: {email}");
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ipKey}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });



    /*options.AddPolicy("LoginPolicy", context =>
    {
        var email = context.Request.HasFormContentType
            ? context.Request.Form["email"].ToString().ToLowerInvariant()
            : null;

        var ip = context.Connection.RemoteIpAddress;
        string ipString = ip?.MapToIPv4().ToString() ?? "unknown";

        var partitionKey = !string.IsNullOrWhiteSpace(email)
            ? $"login:{email}"
            : $"ip:{ip}";

        Console.WriteLine($"[RATE] LoginPolicy partitionKey: {partitionKey}");
        Console.WriteLine($"[RATE] IP Address: {ipString}");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }); */

});


builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<TicketSystem.Services.RecaptchaOptions>(
    builder.Configuration.GetSection("GoogleReCaptcha"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<TicketSystem.Services.IRecaptchaService, TicketSystem.Services.RecaptchaService>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.Name = ".TicketSystem.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;           // CSRF yüzeyini daraltır
});

var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();


/*app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,

    KnownProxies =
    {
        IPAddress.Parse("127.0.0.1"),  // IPv4 localhost
        IPAddress.Parse("::1")         // IPv6 localhost 
    }
});
*/
app.UseRouting();

app.UseSession();


app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
