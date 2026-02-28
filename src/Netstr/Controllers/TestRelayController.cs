using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;

namespace Netstr.Controllers
{
    /// <summary>
    /// Test controller for managing relay configurations using NIP-65 events directly.
    /// </summary>
    [ApiController]
    [Route("api/test/[controller]")]
    public class TestRelayController : ControllerBase
    {
        private readonly NetstrDbContext _dbContext;
        private readonly ILogger<TestRelayController> _logger;

        public TestRelayController(NetstrDbContext dbContext, ILogger<TestRelayController> logger)
        {
            this._dbContext = dbContext;
            this._logger = logger;
        }

        /// <summary>
        /// Gets relay configuration for a user from their latest kind 10002 event.
        /// </summary>
        /// <param name="pubKey">The user's public key</param>
        /// <returns>Relay configuration derived from the latest kind 10002 event</returns>
        [HttpGet("{pubKey}")]
        public async Task<ActionResult<object>> GetRelayConfig(string? pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                this._logger.LogWarning("Attempted to retrieve relay configuration with null or empty public key");
                return BadRequest("Public key is required");
            }

            try
            {
                // Query for the most recent kind 10002 event for the specified public key.
                var relayEvent = await this._dbContext.Events
                    .Include(e => e.Tags)
                    .Where(e => e.EventKind == (long)EventKind.RelayList && e.EventPublicKey == pubKey)
                    .OrderByDescending(e => e.EventCreatedAt)
                    .FirstOrDefaultAsync();

                if (relayEvent == null)
                {
                    this._logger.LogWarning("No relay configuration found for user {PubKey}", pubKey);
                    return NotFound($"No relay configuration found for user {pubKey}");
                }

                // Extract relay information from tags using the canonical NIP?65 approach.
                var relayList = relayEvent.Tags
                    .Where(tag => tag.Name == "r")
                    .Select(tag => new
                    {
                        Url = tag.Value,
                        Read = tag.OtherValues != null && tag.OtherValues.Contains("read"),
                        Write = tag.OtherValues != null && tag.OtherValues.Contains("write")
                    })
                    .ToList();

                var result = new
                {
                    EventId = relayEvent.Id,
                    CreatedAt = relayEvent.EventCreatedAt,
                    Relays = relayList
                };

                this._logger.LogInformation("Retrieved relay configuration for user {PubKey} from event {EventId}", pubKey, relayEvent.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to retrieve relay configuration for user {PubKey}", pubKey);
                return StatusCode(500, "Failed to retrieve relay configuration");
            }
        }
    }
}