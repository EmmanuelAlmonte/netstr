using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Extensions;
using Netstr.Middleware;
using Netstr.Options;
using Netstr.RelayInformation;
using Netstr.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load local configuration for secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

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

// Setup pipeline + init DB
app
    .UseCors()
    .UseWebSockets()
    .UseStaticFiles()
    .UseRouting()
    .UseHttpsRedirection()
    .AcceptWebSocketsConnections()
    .EnsureDbContextMigrations<NetstrDbContext>();

// Controllers maps
app.MapDefaultControllerRoute();

// Start the app
app.Run();

// Required for tests
public partial class Program { }
