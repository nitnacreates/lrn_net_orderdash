using Central.Api.Data;
using Central.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CentralDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// MVP: migrate + seed on boot so there's no separate manual step (§6).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBridgeEndpoints();

app.Run();
