using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Extensions;
using Netstr.Middleware;
using Netstr.Options;
using Netstr.RelayInformation;
using Netstr.Services;
using Netstr.Startup;
using Serilog;

var runtimePaths = RuntimePathsOptions.FromEnvironment();

var startupCommand = StartupCommandHandler.TryHandle(args, runtimePaths);
if (startupCommand.ShouldExit)
{
    Environment.ExitCode = startupCommand.ExitCode;
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Apply runtime path defaults and host-level configuration files.
builder.Configuration.AddInMemoryCollection(runtimePaths.ToConfigurationEntries());
builder.Configuration.AddJsonFile(runtimePaths.GetConfigFilePath("appsettings.json"), optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(runtimePaths.GetConfigFilePath("appsettings.local.json"), optional: true, reloadOnChange: true);

// Load local configuration for secrets (not committed to git).
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Keep environment variables and command line args as highest-priority overrides.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

if (OperatingSystem.IsLinux() && string.IsNullOrWhiteSpace(builder.Configuration[WebHostDefaults.ServerUrlsKey]))
{
    builder.WebHost.UseUrls("http://127.0.0.1:8086");
}

if (OperatingSystem.IsLinux())
{
    var configuredLogPath = builder.Configuration["Serilog:WriteTo:1:Args:path"];
    if (string.IsNullOrWhiteSpace(configuredLogPath) ||
        string.Equals(configuredLogPath, "logs/log.txt", StringComparison.OrdinalIgnoreCase))
    {
        builder.Configuration["Serilog:WriteTo:1:Args:path"] = runtimePaths.GetDefaultLogFilePath();
    }
}

var connectionString = builder.Configuration.GetConnectionString("NetstrDatabase");

// Setup Serilog logging
builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));

builder.Services
    .AddCors(x => x.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()))
    .AddControllersWithViews().Services
    .AddHttpContextAccessor()
    .AddApplicationsOptions()
    .AddMessaging()
    .AddHostedService<UserCacheStartupService>()
    .AddHostedService<NegentropyBackgroundWatcher>()
    .AddHostedService<CleanupBackgroundService>()
    .AddScoped<IRelayInformationService, RelayInformationService>()
    .AddDbContextFactory<NetstrDbContext>(x => x.UseNpgsql(connectionString, options =>
    {
        // Enable automatic retry on transient failures (network issues, timeouts, deadlocks)
        options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);

        // Set command timeout to 30 seconds (default is 30, but being explicit)
        options.CommandTimeout(30);

        // Enable connection pooling optimization for Supabase
        options.MaxBatchSize(100);
    }))
    .AddSingleton<IConfigurationWriter, ConfigurationWriter>();

var app = builder.Build();
var options = app.Services.GetRequiredService<IOptions<ConnectionOptions>>();
var runtimePathOptions = app.Services.GetRequiredService<IOptions<RuntimePathsOptions>>();

// Log environment and configuration
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("HTTPS Redirect Enabled: {Enabled}", options.Value.UseHttpsRedirection);
logger.LogInformation("Forwarded Headers Enabled: {Enabled}", options.Value.UseForwardedHeaders);
logger.LogInformation("WebSocket Path: {Path}", options.Value.WebSocketsPath);
logger.LogInformation(
    "Runtime Paths - Config: {ConfigDirectory}, Data: {DataDirectory}, Log: {LogDirectory}",
    runtimePathOptions.Value.ConfigDirectory,
    runtimePathOptions.Value.DataDirectory,
    runtimePathOptions.Value.LogDirectory);

// Setup pipeline + init DB
if (options.Value.UseForwardedHeaders)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    });
}

app
    .UseCors()
    .UseWebSockets()
    .UseStaticFiles()
    .UseRouting();

// Conditionally apply HTTPS redirection based on configuration
if (options.Value.UseHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app
    .AcceptWebSocketsConnections()
    .EnsureDbContextMigrations<NetstrDbContext>();

app.MapGet("/healthz", async (IDbContextFactory<NetstrDbContext> dbContextFactory, CancellationToken cancellationToken) =>
{
    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return Results.Json(
                new { status = "unhealthy", reason = "database_unreachable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new { status = "ok" });
    }
    catch
    {
        return Results.Json(
            new { status = "unhealthy", reason = "database_check_failed" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Controllers maps
app.MapDefaultControllerRoute();

// Start the app
app.Run();

// Required for tests
public partial class Program { }
