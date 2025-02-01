using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validator for NIP-65 Relay List events (kind: 10002).
    /// Implements IEventValidator to integrate with the event processing pipeline.
    /// </summary>
    public class RelayListEventValidator : IEventValidator
    {
        private readonly ILogger<RelayListEventValidator> _logger;

        public RelayListEventValidator(ILogger<RelayListEventValidator> logger)
        {
            this._logger = logger;
        }


        /// <summary>
        /// Validates relay list events according to NIP-65 specifications.
        /// </summary>
        /// <param name="event">The event to validate</param>
        /// <param name="context">The client context</param>
        /// <returns>Error message if validation fails, null if validation succeeds</returns>
        public string? Validate(Event @event, ClientContext context)
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            if (!@event.Kind.Equals(EventKind.RelayList))
            {
                return null; // Not a relay list event, skip validation
            }

            try
            {
                RelayListValidator.Validate(@event);
                this._logger.LogInformation("Validated relay list event: {@Event}", @event);
                return null;
            }
            catch (EventProcessingException ex)
            {
                this._logger.LogError(ex, "Failed to validate relay list event: {@Event}", @event);
                return ex.Message;
            }
        }
    }
}
