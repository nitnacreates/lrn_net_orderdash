using Central.Api.Connectors;
using Central.Api.Data;
using Central.Api.Endpoints;
using Central.Api.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CentralDbContext>(o => o.UseNpgsql(connectionString));

// Dashboard login (§12): single seeded admin -> JWT. Bridge auth stays on the API key (§12).
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-jwt-signing-key-change-me-please-0123456789";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "orderdash",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "orderdash-dashboard",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true
    });
builder.Services.AddAuthorization();

// Hangfire on the same Postgres — durable jobs + retries, no extra broker (§5, §16).
builder.Services.AddHangfire(c => c.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// Connector templates (§9) + the per-channel sync job + the document relay (§14).
builder.Services.AddSingleton(new ConnectorFactory(
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "data"))));
builder.Services.AddScoped<ChannelSyncJob>();
builder.Services.AddScoped<DocumentRelay>();

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

// One recurring Central<->channel job per active channel (§5), scheduled from the channel row.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
    var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    foreach (var channel in db.Channels.Where(c => c.IsActive).ToList())
    {
        recurring.AddOrUpdate<ChannelSyncJob>(
            $"sync-{channel.Key}",
            job => job.RunAsync(channel.Key),
            CronFor(channel.ScheduleMinutes));
    }
}

// Hangfire's Cron.MinuteInterval only accepts 1..59; fall back to hourly for anything >= 60.
static string CronFor(int minutes) => minutes is > 0 and < 60
    ? $"*/{minutes} * * * *"
    : "0 * * * *";

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapBridgeEndpoints();
app.MapDashboardEndpoints();

app.Run();
