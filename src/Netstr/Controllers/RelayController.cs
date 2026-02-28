using Microsoft.AspNetCore.Mvc;
using Netstr.Data;

namespace Netstr.Controllers
{
    /// <summary>
    /// Controller for managing relay configurations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RelayController : ControllerBase
    {
        private readonly NetstrDbContext _dbContext;
        private readonly ILogger<RelayController> _logger;

        public RelayController(NetstrDbContext dbContext, ILogger<RelayController> logger)
        {
            this._dbContext = dbContext;
            this._logger = logger;
        }

        /// <summary>
        /// Gets all relay configurations for a user.
        /// </summary>
        /// <param name="pubKey">The user's public key</param>
        /// <returns>List of relay configurations</returns>
        [HttpGet("{pubKey}")]
        public async Task<ActionResult<IEnumerable<RelayConfigEntity>>> GetRelayConfigs(string? pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                this._logger.LogWarning("Attempted to retrieve relay configurations with null or empty public key");
                return BadRequest("Public key is required");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(this._dbContext, nameof(this._dbContext));

                var configs = await this._dbContext.GetRelayConfigsAsync(pubKey);
                
                if (configs == null)
                {
                    this._logger.LogWarning("No relay configurations found for user {PubKey}", pubKey);
                    return NotFound($"No relay configurations found for user {pubKey}");
                }

                this._logger.LogInformation("Retrieved {Count} relay configurations for user {PubKey}", configs.Count, pubKey);
                return Ok(configs);
            }
            catch (ArgumentNullException ex)
            {
                this._logger.LogError(ex, "Database context is null when retrieving relay configurations");
                return StatusCode(500, "Internal server error");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to retrieve relay configurations for user {PubKey}", pubKey);
                return StatusCode(500, "Failed to retrieve relay configurations");
            }
        }
    }
}
