using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Services;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using TransactionalBusiness.Api.Jobs;
using Serilog;

using CorrelationId;
using CorrelationId.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
    options.IncludeInResponse = true;
    options.EnforceHeader = false;
});

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/payment-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<RetryTransactionJob>();
builder.Services.AddScoped<StuckTransactionRecoveryJob>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddHangfireServer();

var app = builder.Build();

// ✅ Apply correlation ID middleware FIRST
app.UseCorrelationId();

// ✅ Enable Serilog request logging (VERY IMPORTANT)
app.UseSerilogRequestLogging();

// ✅ Auto-migrate DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

// ✅ Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception?.Error switch
        {
            InvalidOperationException => 400,
            KeyNotFoundException => 404,
            _ => 500
        };

        await context.Response.WriteAsJsonAsync(new
        {
            error = exception?.Error.Message,
            correlationId = context.TraceIdentifier // optional: return correlation ID
        });
    });
});

app.UseRouting();

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<StuckTransactionRecoveryJob>(
    "stuck-transaction-recovery",
    job => job.ExecuteAsync(),
    "*/15 * * * *" // every 15 minutes
);

app.UseAuthorization();

app.MapControllers();

app.Run();
