using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events
{
    public interface ICleanupService
    {
        Task RunCleanupAsync();
    }

    public class CleanupService : ICleanupService
    {
        private readonly IDbContextFactory<NetstrDbContext> db;
        private readonly ILogger<CleanupService> logger;
        private readonly IOptions<CleanupOptions> options;

        public CleanupService(
            IDbContextFactory<NetstrDbContext> db,
            ILogger<CleanupService> logger, 
            IOptions<CleanupOptions> options)
        {
            this.db = db;
            this.logger = logger;
            this.options = options;
        }

        public async Task RunCleanupAsync()
        {
            var cleanupStart = DateTimeOffset.UtcNow;
            var options = this.options.Value;
            var now = DateTimeOffset.UtcNow;
            var deletedOffset = now.AddDays(-options.DeleteDeletedEventsAfterDays);
            var expiredOffset = now.AddDays(-options.DeleteExpiredEventsAfterDays);

            using var db = this.db.CreateDbContext();

            // Use execution strategy to handle transactions with retry logic
            var strategy = db.Database.CreateExecutionStrategy();
            var totalDeleted = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();
                var deleted = 0;

                // old deleted items
                var deletedCount = await db.Events.Where(x => x.DeletedAt.HasValue && x.DeletedAt < deletedOffset).ExecuteDeleteAsync();
                deleted += deletedCount;
                this.logger.LogInformation("Cleanup: removed {Count} soft-deleted events older than {Days} days", deletedCount, options.DeleteDeletedEventsAfterDays);

                // old expires items
                var expiredCount = await db.Events.Where(x => x.EventExpiration.HasValue && x.EventExpiration < expiredOffset).ExecuteDeleteAsync();
                deleted += expiredCount;
                this.logger.LogInformation("Cleanup: removed {Count} expired events older than {Days} days", expiredCount, options.DeleteExpiredEventsAfterDays);

                // kind ranges rules
                foreach (var rule in options.DeleteEventsRules)
                {
                    var offset = now.AddDays(-rule.DeleteAfterDays);
                    var ruleDeletedCount = 0;

                    foreach (var range in rule.Kinds.Select(KindRange.Parse))
                    {
                        var rangeCount = await db.Events.Where(x => x.EventKind >= range.MinKind && x.EventKind <= range.MaxKind && x.EventCreatedAt < offset).ExecuteDeleteAsync();
                        ruleDeletedCount += rangeCount;
                    }

                    deleted += ruleDeletedCount;
                    this.logger.LogInformation("Cleanup: removed {Count} events matching kind rule (kinds: {Kinds}, {Days} days old)",
                        ruleDeletedCount, string.Join(", ", rule.Kinds), rule.DeleteAfterDays);
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return deleted;
            });

            var cleanupTime = DateTimeOffset.UtcNow - cleanupStart;

            if (cleanupTime.TotalSeconds > 60)
            {
                this.logger.LogWarning("Cleanup took {Duration} seconds to delete {Count} events",
                    cleanupTime.TotalSeconds, totalDeleted);
            }
            else
            {
                this.logger.LogInformation("Cleanup completed in {Duration} seconds: deleted {Count} total events",
                    cleanupTime.TotalSeconds, totalDeleted);
            }
        }
    }
}
