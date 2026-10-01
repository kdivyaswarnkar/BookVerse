using System.Text;
using BookStore.Api.Middleware;
using BookStore.Api.OpenApi;
using BookStore.Core.Constants;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using BookStore.Data;
using BookStore.Data.Repositories;
using BookStore.Data.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + daily rolling file in the "logs" folder
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/bookstore-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// Settings: "App" and "Jwt" sections (the Jwt Key comes from secrets.json)
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' settings. Add Jwt:Key to secrets.json.");
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");

// Authentication: every request with "Authorization: Bearer <token>" is checked here
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    });

// Authorization: named rules that controllers use with [Authorize(Policy = ...)]
builder.Services.AddAuthorization(o =>
    o.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin)));

// Database (EF Core)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repositories and services
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

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
app.UseAuthentication();   // who are you?   (must come before UseAuthorization)
app.UseAuthorization();    // are you allowed?
app.MapControllers();

app.MapGet("/health/db", async (AppDbContext db) =>
    Results.Ok(new { canConnect = await db.Database.CanConnectAsync() }));

app.Run();