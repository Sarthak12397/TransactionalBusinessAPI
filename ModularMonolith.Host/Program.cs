using Hangfire;
using ModularMonolith.Host.Seeding;
using Modules.Common.API.Extensions;
using Modules.Common.Infrastructure.BackgroundJobs;
using Modules.Common.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddWebHostDependencies();

builder.AddCoreHostLogging();

builder.Services.AddCoreWebApiInfrastructure();

// ⚡ ADD HANGFIRE
builder.Services.AddHangfireBackgroundJobs(builder.Configuration);

builder.Services.AddCoreInfrastructure(builder.Configuration,
[
    TransactionsModuleRegistration.ActivityModuleName,
    // Add other modules...
]);

builder.Services
    .AddUsersModule(builder.Configuration)
    .AddTransactionsModule(builder.Configuration); // New transactions module

// Seed entities in DEVELOPMENT mode
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<SeedService>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

// ⚡ ADD HANGFIRE DASHBOARD
// Access at /hangfire to monitor jobs
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Payment System Background Jobs",
    StatsPollingInterval = 5000 // Refresh every 5 seconds
});

// Run migrations in DEVELOPMENT mode
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.MigrateModuleDatabasesAsync();

    var userSeedService = scope.ServiceProvider.GetRequiredService<UserSeedService>();
    await userSeedService.SeedUsersAsync();

    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seedService.SeedDataAsync();
}

// ⚡ CONFIGURE RECURRING JOBS
// This sets up scheduled background jobs (reconciliation, retries, monitoring)
HangfireConfiguration.ConfigureRecurringJobs();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseModuleMiddlewares();

app.MapApiEndpoints();

await app.RunAsync();

// ========================================
// Hangfire Dashboard Authorization
// ========================================

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, add proper authentication!
        // For now, allow access in development
        var httpContext = context.GetHttpContext();
        return httpContext.Request.Host.Host == "localhost" 
            || httpContext.Request.Host.Host == "127.0.0.1";
    }
}
