# Ticket System – OWASP Security Demo (ASP.NET Core MVC)

This repository demonstrates **OWASP Top 10** concepts — especially **SQL Injection** and **Authentication weaknesses** — inside a simple **ASP.NET Core MVC** ticketing app.  
It intentionally contains **vulnerable endpoints** (for demos) as well as secure implementations.

The goal is to **show how vulnerabilities work, their risks, and how to fix them** in a real-world MVC application.

---

## Table of contents

- [General Flow](#general-flow)
- [Architecture](#architecture)
- [Entities](#entities-short--links)
- [Controllers](#controllers-methods--purpose)
- [Services](#services)
- [Security highlights (OWASP)](#security-highlights-owasp)
- [Critical Code Examples](#critical-code-examples)

---

## General Flow

1. **Login**
   - Secure login (`/auth/login`) → reCAPTCHA, rate limiting, EF LINQ (parameterized).
   - Weak login (`/auth/login-open`) → EF LINQ but no CAPTCHA/rate limit.
   - Vulnerable login (`/auth/login-insecure`) → Raw SQL → SQL Injection possible.
   
2. **Ticket Operations**
   - Create / Edit / Delete tickets with CSRF protection.
   - Authorization check → Only creator, assignee, or admin can edit/view.

3. **Comment Operations**
   - Add or delete comments with CSRF protection.

4. **Admin Operations**
   - List all users (role check).
   - Auto-create random users for demo purposes.

---

## Architecture

- **Controllers** – HTTP endpoints & request handling  
- **Models/Entities** – EF Core classes  
- **Data** – `AppDbContext` for EF Core  
- **Services** – reCAPTCHA verification, account creation  
- **Views** – Razor UI pages

---

## Entities 

- **User** – minimal user record (Email, Password, Role).  
  [`Models/User.cs`](TicketSystem/Models/User.cs)

- **Ticket** – title, description, status + creator/assignee relations.  
  [`Models/Ticket.cs`](TicketSystem/Models/Ticket.cs)

- **Comment** – per-ticket comments with timestamps and author.  
  [`Models/Comment.cs`](TicketSystem/Models/Comment.cs)

- **AppDbContext** – EF Core `DbContext` (DbSets, relationships).  
  [`Data/AppDbContext.cs`](TicketSystem/Data/AppDbContext.cs)

---

## Controllers (methods + purpose)

### AuthController
[`Controllers/AuthController.cs`](TicketSystem/Controllers/AuthController.cs)

- **`GET /auth/login` → `LoginForm()`** – serves login view, injects reCAPTCHA site key.
- **`POST /auth/login` – SECURE** – EF LINQ query (parameterized), Google reCAPTCHA verification, rate limiting (`[EnableRateLimiting("LoginPolicy")]`), stores session info.
- **`POST /auth/login-open` – WEAK** – EF LINQ but no CAPTCHA or rate limiting → brute-force demo.
- **`POST /auth/login-insecure` – VULNERABLE** – builds raw SQL string with `FromSqlRaw` → **SQL Injection** possible (`a@example.com' OR 1=1 --`).
- **`POST /auth/logout`** – clears session, redirects.

### UsersController
[`Controllers/UsersController.cs`](TicketSystem/Controllers/UsersController.cs)

- Admin-only user list/create (manual role check via session + DB role query).
- **`CreateAuto()`** – demo helper: generates a random user/password.

### TicketsController
[`Controllers/TicketsController.cs`](TicketSystem/Controllers/TicketsController.cs)

- **`Index()`** – lists tickets with related users.
- **`Details(id)`** – shows ticket details (auth check for admin/creator/assignee).
- **`Create()` GET** – shows form (requires login).
- **`Create(...)` POST** – `[ValidateAntiForgeryToken]` CSRF protection, sets creator from session.
- **`Edit(...)` / `Delete(...)`** – CSRF-protected, only authorized users can modify.

### CommentsController
[`Controllers/CommentsController.cs`](TicketSystem/Controllers/CommentsController.cs)

- **`Add`** – adds comment (CSRF-protected).
- **`Delete`** – deletes comment (CSRF-protected).
- **`Edit`** - edit comment
  
### HomeController
[`Controllers/HomeController.cs`](TicketSystem/Controllers/HomeController.cs)

- Landing, navigation; example of safe JSON embedding in Razor.

---

## Services

- **RecaptchaService** – verifies Google reCAPTCHA tokens via HTTP.  
  [`Services/RecaptchaService.cs`](TicketSystem/Services/RecaptchaService.cs)

- **AccountCreationService** – generates random users/passwords for demo.  
  [`Services/AccountCreationService.cs`](TicketSystem/Services/AccountCreationService.cs)

- **Rate Limiting** – configured in `Program.cs`, applied to secure login with `[EnableRateLimiting("LoginPolicy")]`.  
  [`Program.cs`](TicketSystem/Program.cs)

---

## Security highlights (OWASP)

### **A03: Injection**
- **Vulnerable code**:
```csharp
var sql = $"SELECT * FROM Users WHERE Email = '{email}' AND Password = '{password}'";
var userList = await _context.Users.FromSqlRaw(sql).ToListAsync();
