using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Netstr.Options;
using Netstr.Services;
using System.Collections.Generic;
using System.Linq;

namespace Netstr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhitelistController : ControllerBase
    {
        private readonly IOptionsMonitor<WhitelistOptions> _whitelistOptions;
        private readonly ILogger<WhitelistController> _logger;
        private readonly IConfigurationWriter _configWriter;

        public WhitelistController(
            IOptionsMonitor<WhitelistOptions> whitelistOptions,
            ILogger<WhitelistController> logger,
            IConfigurationWriter configWriter)
        {
            _whitelistOptions = whitelistOptions;
            _logger = logger;
            _configWriter = configWriter;
        }

        [HttpGet]
        public ActionResult<WhitelistOptions> GetWhitelistSettings()
        {
            return Ok(_whitelistOptions.CurrentValue);
        }

        [HttpGet("keys")]
        public ActionResult<IEnumerable<string>> GetWhitelistedKeys()
        {
            return Ok(_whitelistOptions.CurrentValue.AllowedPublicKeys);
        }

        [HttpPost("keys")]
        public async Task<ActionResult> AddPublicKey([FromBody] string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return BadRequest("Public key cannot be empty");
            }

            try
            {
                var currentKeys = _whitelistOptions.CurrentValue.AllowedPublicKeys.ToList();
                
                if (currentKeys.Contains(publicKey, StringComparer.OrdinalIgnoreCase))
                {
                    return Ok("Public key already in whitelist");
                }
                
                currentKeys.Add(publicKey);
                
                await _configWriter.UpdateConfigurationAsync("Whitelist:AllowedPublicKeys", currentKeys);
                
                _logger.LogInformation("Added public key to whitelist: {PublicKey}", publicKey);
                return Ok("Public key added to whitelist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add public key to whitelist: {PublicKey}", publicKey);
                return StatusCode(500, "Failed to update whitelist");
            }
        }

        [HttpDelete("keys/{publicKey}")]
        public async Task<ActionResult> RemovePublicKey(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return BadRequest("Public key cannot be empty");
            }

            try
            {
                var whitelistOptions = _whitelistOptions.CurrentValue;
                var currentKeys = whitelistOptions.AllowedPublicKeys.ToList();
                var ownerKey = whitelistOptions.OwnerPublicKey;
                
                // Check if trying to remove owner key
                if (!string.IsNullOrEmpty(ownerKey) && 
                    string.Equals(publicKey, ownerKey, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Cannot remove owner's public key from whitelist");
                }
                
                // Check if key exists
                if (!currentKeys.Contains(publicKey, StringComparer.OrdinalIgnoreCase))
                {
                    return NotFound("Public key not found in whitelist");
                }
                
                // Remove the key
                currentKeys.RemoveAll(k => string.Equals(k, publicKey, StringComparison.OrdinalIgnoreCase));
                
                // Update configuration
                await _configWriter.UpdateConfigurationAsync("Whitelist:AllowedPublicKeys", currentKeys);
                
                _logger.LogInformation("Removed public key from whitelist: {PublicKey}", publicKey);
                return Ok("Public key removed from whitelist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove public key from whitelist: {PublicKey}", publicKey);
                return StatusCode(500, "Failed to update whitelist");
            }
        }

        [HttpPut("settings")]
        public async Task<ActionResult> UpdateSettings([FromBody] WhitelistSettingsDto settings)
        {
            try
            {
                await _configWriter.UpdateConfigurationAsync("Whitelist:Enabled", settings.Enabled);
                await _configWriter.UpdateConfigurationAsync("Whitelist:RestrictPublishing", settings.RestrictPublishing);
                await _configWriter.UpdateConfigurationAsync("Whitelist:RestrictSubscribing", settings.RestrictSubscribing);
                
                _logger.LogInformation("Updated whitelist settings: Enabled={Enabled}, RestrictPublishing={RestrictPublishing}, RestrictSubscribing={RestrictSubscribing}",
                    settings.Enabled, settings.RestrictPublishing, settings.RestrictSubscribing);
                
                return Ok("Whitelist settings updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update whitelist settings");
                return StatusCode(500, "Failed to update whitelist settings");
            }
        }

        [HttpPut("owner")]
        public async Task<ActionResult> SetOwnerPublicKey([FromBody] string ownerPublicKey)
        {
            if (string.IsNullOrWhiteSpace(ownerPublicKey))
            {
                return BadRequest("Owner public key cannot be empty");
            }

            try
            {
                var currentKeys = _whitelistOptions.CurrentValue.AllowedPublicKeys.ToList();
                
                // Ensure owner key is in the whitelist
                if (!currentKeys.Contains(ownerPublicKey, StringComparer.OrdinalIgnoreCase))
                {
                    currentKeys.Add(ownerPublicKey);
                    await _configWriter.UpdateConfigurationAsync("Whitelist:AllowedPublicKeys", currentKeys);
                }
                
                // Set the owner key
                await _configWriter.UpdateConfigurationAsync("Whitelist:OwnerPublicKey", ownerPublicKey);
                
                _logger.LogInformation("Set owner public key: {PublicKey}", ownerPublicKey);
                return Ok("Owner public key set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set owner public key: {PublicKey}", ownerPublicKey);
                return StatusCode(500, "Failed to update whitelist");
            }
        }
    }

    public class WhitelistSettingsDto
    {
        public bool Enabled { get; set; }
        public bool RestrictPublishing { get; set; }
        public bool RestrictSubscribing { get; set; }
    }
}
