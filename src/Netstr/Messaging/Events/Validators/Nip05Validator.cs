using System.Text.Json;
using Netstr.Messaging.Models;
using Netstr.Services;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validator for NIP-05 DNS-based identity verification in metadata events
    /// Note: This validator doesn't reject events, it just performs verification for monitoring
    /// </summary>
    public class Nip05Validator : IEventValidator
    {
        private readonly INip05VerificationService _nip05Service;
        private readonly ILogger<Nip05Validator> _logger;

        public Nip05Validator(
            INip05VerificationService nip05Service,
            ILogger<Nip05Validator> logger)
        {
            this._nip05Service = nip05Service;
            this._logger = logger;
        }

        public string? Validate(Event e, ClientContext context)
        {
            // Only validate kind 0 (metadata) events
            if (e.Kind != 0)
                return null; // Success - no validation error

            // NIP-05 validation is async, so we'll do it in a background task
            // to avoid blocking event processing
            _ = Task.Run(async () => await ValidateNip05Async(e));

            // Never reject events based on NIP-05 validation
            // This is for verification tracking only
            return null; // Success - always allow events to pass through
        }

        private async Task ValidateNip05Async(Event e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.Content))
                    return;

                var metadata = JsonSerializer.Deserialize<UserMetadata>(e.Content);
                if (metadata?.Nip05 == null)
                    return;

                this._logger.LogDebug($"Validating NIP-05 identifier '{metadata.Nip05}' for pubkey {e.PublicKey}");

                var result = await this._nip05Service.VerifyIdentifierAsync(metadata.Nip05, e.PublicKey);
                
                if (result.IsValid)
                {
                    this._logger.LogInformation($"NIP-05 verification successful: {metadata.Nip05} -> {e.PublicKey}");
                }
                else
                {
                    this._logger.LogWarning($"NIP-05 verification failed for {e.PublicKey} claiming '{metadata.Nip05}': {result.Error}");
                }
            }
            catch (JsonException ex)
            {
                this._logger.LogWarning($"Failed to parse metadata JSON for NIP-05 validation in event {e.Id}: {ex.Message}");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Unexpected error during NIP-05 validation for event {e.Id}");
            }
        }
    }
}