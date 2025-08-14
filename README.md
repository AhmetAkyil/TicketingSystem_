# Ticket System – OWASP Security Demo (ASP.NET Core MVC)

This repository demonstrates **OWASP Top 10** concepts—especially **SQL Injection** and **Authentication weaknesses**—inside a simple **ASP.NET Core MVC** ticketing app.  
It intentionally contains **vulnerable endpoints** (for demos) as well as secure implementations.



---

## Table of contents

- [Architecture](#architecture)
- [Entities](#entities-short--links)
- [Controllers](#controllers-methods--purpose)
- [Services](#services)
- [Security highlights (OWASP)](#security-highlights-owasp)

---

## Architecture

- **Controllers** – HTTP endpoints & flows  
- **Models/Entities** – EF Core classes  
- **Data** – `AppDbContext` for EF Core  
- **Services** – reCAPTCHA verification, account creation  
- **Views** – Razor UI pages

Root: `TicketSystem/` → app code lives under this folder in your repo.

---

## Entities (short + links)

- **User** – minimal user record (Email, Password, Role).  
  [`Models/User.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Models/User.cs)

- **Ticket** – title, description, status + creator/assignee relations.  
  [`Models/Ticket.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Models/Ticket.cs)

- **Comment** – per-ticket comments with timestamps and author.  
  [`Models/Comment.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Models/Comment.cs)

- **UserInsecure** – simplified “demo only” user table for SQLi example.  
  [`Models/UserInsecure.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Models/UserInsecure.cs)

- **AppDbContext** – EF Core `DbContext` (DbSets, relationships).  
  [`Data/AppDbContext.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Data/AppDbContext.cs)

---

## Controllers (methods + purpose)

### AuthController
[`Controllers/AuthController.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Controllers/AuthController.cs)

- **`GET /auth/login` → `LoginForm()`** – serves login view, injects reCAPTCHA site key.
- **`POST /auth/login` – SECURE** – EF LINQ query (parameterized → no SQLi), Google reCAPTCHA verification, rate limiting via `[EnableRateLimiting("LoginPolicy")]`, stores session info.
- **`POST /auth/login-open` – INTENTIONALLY WEAK** – EF LINQ but no CAPTCHA or rate limiting (brute-force demo).
- **`POST /auth/login-insecure` – INTENTIONALLY VULNERABLE** – builds raw SQL string with `FromSqlRaw` → SQL Injection possible (`a@example.com' OR 1=1 --`).
- **`POST /auth/logout`** – clears session, redirects.

### UsersController
[`Controllers/UsersController.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Controllers/UsersController.cs)

- Admin-only user list/create (manual role check via session).
- **`CreateAuto()`** – demo helper: generates a random user/password.

### TicketsController
[`Controllers/TicketsController.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Controllers/TicketsController.cs)

- **`Index()`** – lists tickets with related users.
- **`Details(id)`** – shows ticket details.
- **`Create()` GET –** shows create form (requires login).
- **`Create(...)` POST –** `[ValidateAntiForgeryToken]` CSRF protection, sets creator from session.
- **`Edit(...)` / `Delete(...)` –** CSRF-protected, updates or removes tickets.

### CommentsController
[`Controllers/CommentsController.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Controllers/CommentsController.cs)

- **`Create`** – adds comment (CSRF-protected).
- **`Delete`** – deletes comment (CSRF-protected).

### HomeController
[`Controllers/HomeController.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Controllers/HomeController.cs)

- Landing, navigation; example of safe JSON embedding in Razor.

---

## Services

- **RecaptchaService** – verifies Google reCAPTCHA tokens via HTTP call.  
  [`Services/RecaptchaService.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Services/RecaptchaService.cs)

- **AccountCreationService** – generates random users/passwords for demo.  
  [`Services/AccountCreationService.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Services/AccountCreationService.cs)

- **Rate Limiting** – configured in `Program.cs`, applied to secure login with `[EnableRateLimiting("LoginPolicy")]`.  
  [`Program.cs`](https://github.com/AhmetAkyil/TicketingSystem/blob/main/TicketSystem/Program.cs)

---

## Security highlights (OWASP)

- **A03: Injection** – `login-insecure` endpoint vulnerable to SQL Injection; use EF LINQ or parameterized SQL instead.
- **A07: Identification & Authentication Failures** – secure login uses CAPTCHA + rate limit; `login-open` omits both for brute-force demo.

---


