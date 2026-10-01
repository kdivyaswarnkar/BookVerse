var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<BookStore.Data.DbConnectionFactory>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health/db", async (BookStore.Data.DbConnectionFactory f) => {
    using var c = f.Create();
    var db = await Dapper.SqlMapper.ExecuteScalarAsync<string>(c, "SELECT DB_NAME()");
    return Results.Ok(new { database = db });
});

app.Run();
