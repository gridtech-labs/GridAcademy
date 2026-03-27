# GridAcademy — Backend API

Simple, clean ASP.NET Core 8 backend for the GridAcademy LMS.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- PostgreSQL 14+
- `dotnet-ef` CLI tool

```bash
dotnet tool install --global dotnet-ef
```

## Quick Start

### 1. Configure database
Edit `appsettings.Development.json` with your PostgreSQL connection string.

### 2. Create & apply migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Run
```bash
dotnet run
```
App starts at `http://localhost:5000` — Swagger UI loads at the root.

### 4. Login with seeded admin
```
POST /api/auth/login
{ "email": "admin@gridacademy.com", "password": "Admin@123!" }
```
Copy the token → click **Authorize** in Swagger → paste `Bearer <token>`.

### 5. Hangfire Dashboard
```
http://localhost:5000/hangfire
```

## Install all packages
```bash
dotnet restore
```

## Folder Structure
```
GridAcademy/
├── Controllers/        HTTP endpoints (thin — just routing + response shaping)
├── Services/           Business logic (IUserService, IAuthService)
├── Data/
│   ├── Entities/       EF Core entity models
│   ├── AppDbContext.cs EF Core DbContext
│   └── DbSeeder.cs     Seeds default admin on first run
├── DTOs/
│   ├── Auth/           LoginRequest / LoginResponse
│   └── Users/          CreateUserRequest / UpdateUserRequest / UserDto
├── Jobs/               Hangfire background jobs
├── Helpers/            PasswordHelper (BCrypt), JwtHelper
├── Middleware/         Global exception handler
├── Common/             ApiResponse<T>, PagedResult<T>
├── Program.cs          App bootstrap & DI wiring
└── appsettings.json
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | Public | Login → get JWT |
| GET | `/api/users` | Admin | List users (paginated) |
| GET | `/api/users/{id}` | Admin | Get user by ID |
| GET | `/api/users/me` | Any auth | Get own profile |
| POST | `/api/users` | Admin | Create user |
| PUT | `/api/users/{id}` | Admin | Update user |
| DELETE | `/api/users/{id}` | Admin | Delete user |

## Background Jobs (Hangfire)

| Job | Schedule | Description |
|-----|----------|-------------|
| `InactiveUserJob` | Daily 02:00 UTC | Logs users inactive for 90+ days |
| `EmailJob` | On-demand | Welcome/notification emails (fire & forget) |
