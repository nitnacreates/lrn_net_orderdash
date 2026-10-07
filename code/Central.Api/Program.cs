using Central.Api.Connectors;
using Central.Api.Data;
using Central.Api.Endpoints;
using Central.Api.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CentralDbContext>(o => o.UseNpgsql(connectionString));

// Hangfire on the same Postgres — durable jobs + retries, no extra broker (§5, §16).
builder.Services.AddHangfire(c => c.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// Connector templates (§9) + the per-channel sync job.
builder.Services.AddSingleton(new ConnectorFactory(
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "data", "ftp"))));
builder.Services.AddScoped<ChannelSyncJob>();

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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBridgeEndpoints();

app.Run();
