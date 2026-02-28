using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Options;
using Netstr.Options.Limits;
using System.Net.WebSockets;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Netstr.Tests
{
    public class WebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<NetstrDbContext>(x => TestDbContext.InitializeAndSeed(false).context);
                services.AddSingleton<IDbContextFactory<NetstrDbContext>>(x => new DbContextFactory());
                
                // Register missing services for tests
                services.AddHttpClient();
                services.AddMemoryCache();
                services.AddHttpClient<Netstr.Services.INip05VerificationService, Netstr.Services.Nip05VerificationService>();
            });

            builder.ConfigureAppConfiguration((ctx, b) =>
            {
                b.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Limits:MaxPayloadSize"] = $"{MaxPayloadSize}",
                    // Many fixtures use hard-coded 2024 timestamps; keep tests stable even as wall-clock time moves on.
                    ["Limits:Events:MaxCreatedAtLowerOffset"] = $"{60 * 60 * 24 * 365 * 10}",
                    ["Limits:Events:MaxCreatedAtUpperOffset"] = $"{60 * 60 * 24 * 365 * 10}",
                    ["Filters:AllowAndTagFilters"] = AllowAndTagFilters.ToString()
                });
                b.AddInMemoryObject(EventLimits, "Limits:Events");
                b.AddInMemoryObject(SubscriptionLimits, "Limits:Subscriptions");
                b.AddInMemoryObject(NegentropyLimits, "Limits:Negentropy");
                b.AddInMemoryCollection([ KeyValuePair.Create("Auth:Mode", AuthMode.ToString())]);
                b.AddInMemoryObject(WhitelistOptions, "Whitelist");
            });
        }

        public SubscriptionLimits? SubscriptionLimits { get; set; }
        public EventLimits? EventLimits { get; set; }
        public NegentropyLimits? NegentropyLimits { get; set; }
        public int MaxPayloadSize { get; set; } = 524288;
        public AuthMode AuthMode { get; set; } = AuthMode.Disabled;
        public WhitelistOptions? WhitelistOptions { get; set; }
        public bool AllowAndTagFilters { get; set; } = true;

        public async Task<WebSocket> ConnectWebSocketAsync(AuthMode authMode = AuthMode.Disabled)
        {
            this.AuthMode = authMode;
            return await Server.CreateWebSocketClient().ConnectAsync(new Uri($"ws://localhost"), CancellationToken.None);
        }
    }

    public class DbContextFactory : IDbContextFactory<NetstrDbContext>
    {
        private readonly DbContextOptions<NetstrDbContext> options;
        
        public DbContextFactory()
        {
            this.options = TestDbContext.InitializeAndSeed(false).options;
        }

        public NetstrDbContext CreateDbContext()
        {
            return new TestDbContext(this.options);
        }
    }
}
