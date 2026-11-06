# BankApi

![CI](https://github.com/alaricXshiran/BankApi/actions/workflows/ci.yml/badge.svg)

A bank account management REST API built with **ASP.NET Core (.NET 10)**.
Staff can open and manage accounts, make deposits and withdrawals, and view each account's transaction history,
with JWT authentication, validation, rate limiting and automated tests.

## Features

- **Accounts (CRUD):** open, view, update and close bank accounts, with automatically generated account numbers
- **Deposits and withdrawals:** with an insufficient-funds check, recorded atomically together with the balance change
- **Transaction history:** a statement for each account, newest first
- **Search, filtering and pagination:** search by name, account number or email, and filter by account type
- **Validation:** invalid input is rejected with clear, field-by-field error messages
- **Business rules:** balances never change directly, and accounts with money left can't be closed
- **Authentication:** staff login with JWT; passwords are stored as salted hashes, never in plain text
- **Rate limiting:** stricter limits on login and money endpoints to block brute-force attacks and abuse
- **Consistent errors:** every error uses the standard Problem Details format; unexpected errors are logged, never exposed
- **Self-setup:** migrations and the first admin user are applied automatically at startup
- **Tests and CI:** xUnit tests run on every push with GitHub Actions
- **Docker:** runs in a container with Docker Compose, with data stored in a volume

## Tech stack

.NET 10 · ASP.NET Core Web API · Entity Framework Core · SQLite · JWT Bearer authentication ·
Swagger UI (OpenAPI) · xUnit · Docker and Docker Compose · GitHub Actions

## Project structure

```
BankApi/
├── src/BankApi/
│   ├── Controllers/     # API endpoints (Accounts, Auth)
│   ├── Data/            # DbContext, startup setup, UTC date converter
│   ├── Dtos/            # What goes in and out of the API
│   ├── ErrorHandling/   # Global exception handler
│   ├── Mappings/        # Model to DTO conversion
│   ├── Migrations/      # Database history (EF Core)
│   ├── Models/          # Account, Transaction, User
│   └── Services/        # JWT token creation
├── tests/BankApi.Tests/ # xUnit tests
├── Dockerfile
└── docker-compose.yml
```

## Getting started

### Option 1: Run with Docker (recommended)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop).

```bash
# Create your settings file from the example, then fill in JWT_KEY and ADMIN_PASSWORD
cp .env.example .env

# Generate a JWT signing key to paste into .env
openssl rand -hex 64

# Build and start the API
docker compose up --build
```

Then open **http://localhost:8080/swagger**.

### Option 2: Run locally with .NET

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Restore the local tools (dotnet-ef)
dotnet tool restore

# Set the secrets (stored outside the project, never committed)
dotnet user-secrets set "Jwt:Key" "<your-random-key>" --project src/BankApi
dotnet user-secrets set "Admin:Username" "admin" --project src/BankApi
dotnet user-secrets set "Admin:Password" "<your-password>" --project src/BankApi

# Run the API (the database and admin user are created automatically)
dotnet run --project src/BankApi
```

Then open **http://localhost:5280/swagger**.

### Using the API in Swagger

1. Call `POST /api/Auth/login` with the admin username and password.
2. Copy the `token` from the response.
3. Click **Authorize** and paste the token.
4. All account endpoints are now available.

## Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Auth/login` | Log in and receive a JWT |
| POST | `/api/Auth/register` | Create a staff user (logged-in staff only) |
| GET | `/api/Accounts` | List accounts (`search`, `type`, `page`, `pageSize`) |
| GET | `/api/Accounts/{id}` | Get one account |
| POST | `/api/Accounts` | Open an account |
| PUT | `/api/Accounts/{id}` | Update holder name, email or type |
| DELETE | `/api/Accounts/{id}` | Close an account (balance must be zero) |
| POST | `/api/Accounts/{id}/deposit` | Deposit money |
| POST | `/api/Accounts/{id}/withdraw` | Withdraw money |
| GET | `/api/Accounts/{id}/transactions` | Transaction history |

All endpoints except login require a JWT.

## Running the tests

```bash
# Build and run all tests
dotnet test
```

## What I'd improve next

- Soft delete for accounts, keeping closed accounts for auditing
- Roles (for example, only admins can close accounts or create staff)
- Integration tests for validation, authorization and rate limiting
- A production database such as SQL Server or PostgreSQL

## Author

**Alaric Shiran** · [GitHub](https://github.com/alaricXshiran)