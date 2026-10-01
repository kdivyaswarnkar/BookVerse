using BookStore.Api.Middleware;
using BookStore.Core.Interfaces;
using BookStore.Data;
using BookStore.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + daily rolling file in the "logs" folder
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/bookstore-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database (EF Core)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();

// CORS: allowed React origins come from appsettings.json ("Cors:Origins")
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("ui", p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Must be first so it can catch errors from everything after it
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "BookStore API v1"));
}

app.UseHttpsRedirection();
app.UseCors("ui");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/db", async (AppDbContext db) =>
    Results.Ok(new { canConnect = await db.Database.CanConnectAsync() }));

app.Run();