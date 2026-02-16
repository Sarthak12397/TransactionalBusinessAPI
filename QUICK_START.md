# Payment System - Quick Start

## Files You Need

From the payment template zip I gave you earlier, you need:
1. `Transaction.cs` (from Transactions.Domain/Entities/)
2. `TransactionStatus.cs` (from Transactions.Domain/Enums/)
3. `Money.cs` (from Transactions.Domain/ValueObjects/)

From this starter kit:
4. `Program.cs` (API host setup)
5. `TransactionsDbContext.cs` (Database context)
6. `TransactionsController.cs` (First endpoint)
7. `appsettings.json` (Configuration)

## Project Structure

```
PaymentSystem/
├── PaymentSystem.csproj
├── Program.cs
├── appsettings.json
├── Domain/
│   ├── Entities/
│   │   └── Transaction.cs
│   ├── Enums/
│   │   └── TransactionStatus.cs
│   └── ValueObjects/
│       └── Money.cs
├── Infrastructure/
│   └── Database/
│       └── TransactionsDbContext.cs
└── API/
    └── Controllers/
        └── TransactionsController.cs
```

## Steps to Run

### 1. Create Project
```bash
dotnet new webapi -n PaymentSystem
cd PaymentSystem
```

### 2. Install Packages
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Hangfire.Core
dotnet add package Hangfire.PostgreSql
dotnet add package Hangfire.AspNetCore
```

### 3. Copy Files
- Copy the 7 files into correct folders (match structure above)
- Delete default `WeatherForecast.cs` and `WeatherForecastController.cs`

### 4. Update Namespaces
Make sure all files use `PaymentSystem` as the namespace:
- `PaymentSystem.Domain.Entities`
- `PaymentSystem.Domain.Enums`
- `PaymentSystem.Domain.ValueObjects`
- `PaymentSystem.Infrastructure.Database`
- `PaymentSystem.API.Controllers`

### 5. Setup PostgreSQL
Make sure PostgreSQL is running:
```bash
# Check if running
psql -U postgres

# If not installed, install it first
```

Update `appsettings.json` with your PostgreSQL password:
```json
"Database": "Host=localhost;Database=payment_system;Username=postgres;Password=YOUR_PASSWORD"
```

### 6. Create Database
```bash
dotnet ef migrations add Initial
dotnet ef database update
```

### 7. Run!
```bash
dotnet run
```

### 8. Test
Open browser: http://localhost:5000/swagger

Try creating a transaction:
```json
POST /api/transactions
{
  "idempotencyKey": "order-123",
  "amount": 99.99,
  "currency": "USD",
  "customerId": "cust_123",
  "orderId": "order_123",
  "paymentMethod": "card",
  "description": "Test payment"
}
```

### 9. Check Hangfire Dashboard
Visit: http://localhost:5000/hangfire

You should see the Hangfire dashboard (no jobs yet, that's OK).

## What Works Now

✅ Create transaction via API
✅ Idempotency (same key = same transaction)
✅ Database persistence
✅ Swagger UI for testing
✅ Hangfire dashboard ready

## What's Next

After this works, we'll add:
1. Background job to process transactions
2. Payment processor integration (mock first)
3. Retry logic
4. More endpoints (refund, etc.)

## Troubleshooting

**"Connection refused" error:**
- PostgreSQL not running
- Wrong password in appsettings.json
- Wrong database name

**"Namespace not found" errors:**
- Check all files have correct namespace
- Make sure folder structure matches

**"Migration failed":**
- Check PostgreSQL connection
- Make sure database user has permissions

## Ready?

Once you get this running and can create a transaction via Swagger, tell me and we'll add the background processing next!
