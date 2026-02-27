var builder = WebApplication.CreateBuilder(args);

// Tell Kestrel to listen on port 8080
builder.WebHost.UseUrls("http://localhost:5089");

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();