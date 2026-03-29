using Microsoft.EntityFrameworkCore;
using TransactionalBusiness.Api.Data;
using TransactionalBusiness.Api.Services;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using TransactionalBusiness.Api.Jobs;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<PaymentDbContext>(
    options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<RetryTransactionJob>();


builder.Services.AddHangfire(config =>

          config.UsePostgreSqlStorage(
            builder.Configuration.GetConnectionString("DefaultConnection")

          )
);



builder.Services.AddHangfireServer();


var app = builder.Build();

app.UseHttpsRedirection();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception?.Error switch
        {
            InvalidOperationException => 400,
            KeyNotFoundException => 404,
            _ => 500
        };

        await context.Response.WriteAsJsonAsync(new
        {
            error = exception?.Error.Message
        });
    });
});
app.UseRouting();
app.UseHangfireDashboard("/hangfire");

app.UseAuthorization();
app.MapControllers();

app.Run();