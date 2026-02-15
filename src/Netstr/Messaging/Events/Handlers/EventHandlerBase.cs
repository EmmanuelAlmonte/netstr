using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;
using Netstr.Options;

namespace Netstr.Messaging.Events.Handlers
{
    public abstract class EventHandlerBase : IEventHandler
    {
        protected readonly ILogger<EventHandlerBase> logger;
        protected readonly IOptions<AuthOptions> auth;
        protected readonly IWebSocketAdapterCollection adapters;

        protected EventHandlerBase(
            ILogger<EventHandlerBase> logger,
            IOptions<AuthOptions> auth,
            IWebSocketAdapterCollection adapters)
        {
            this.logger = logger;
            this.auth = auth;
            this.adapters = adapters;
        }

        public async Task HandleEventAsync(IWebSocketAdapter sender, Event e)
        {
            try
            {
                await HandleEventCoreAsync(sender, e);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation())
            {
                this.logger.LogInformation($"Event {e.ToStringUnique()} already exists, ignoring");
                sender.SendOk(e.Id, Messages.DuplicateEvent);
            }
            catch (DbUpdateException ex)
            {
                this.logger.LogError(ex, "Database update failed for event {EventId} (Kind: {Kind}, PubKey: {PubKey})",
                    e.Id, e.Kind, e.PublicKey);
                sender.SendNotOk(e.Id, Messages.DatabaseError);
            }
            catch (TimeoutException ex)
            {
                this.logger.LogError(ex, "Database timeout while saving event {EventId}", e.Id);
                sender.SendNotOk(e.Id, Messages.DatabaseTimeout);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unexpected error handling event {EventId} (Kind: {Kind})", e.Id, e.Kind);
                sender.SendNotOk(e.Id, Messages.InternalServerError);
            }
        }

        public abstract bool CanHandleEvent(Event e);

        protected abstract Task HandleEventCoreAsync(IWebSocketAdapter sender, Event e);

        protected void BroadcastEvent(Event e)
        {
            var adapters = this.adapters.GetAll();

            foreach (var adapter in adapters)
            {
                BroadcastEventForAdapterAsync(adapter, e);
            }
        }

        private void BroadcastEventForAdapterAsync(IWebSocketAdapter adapter, Event e)
        {
            var isProtectedKind = this.auth.Value.Mode != AuthMode.Disabled &&
                this.auth.Value.ProtectedKinds.Contains(e.Kind);

            if (isProtectedKind)
            {
                if (!adapter.Context.IsAuthenticated())
                {
                    this.logger.LogInformation($"Not going to broadcast event {e.Id}");
                    return;
                }

                if (adapter.Context.PublicKey != e.PublicKey)
                {
                    var isRecipient = e.Tags.Any(x =>
                        x.Length >= 2 &&
                        x[0] == EventTag.PublicKey &&
                        x[1] == adapter.Context.PublicKey);

                    if (!isRecipient)
                    {
                        this.logger.LogInformation($"Not going to broadcast event {e.Id}");
                        return;
                    }
                }
            }

            var subs = adapter.Subscriptions
                .GetAll()
                .Where(x => x.Value.Filters.IsAnyMatch(e))
                .ToList();

            if (subs.Any())
            {
                this.logger.LogInformation($"Broadcasting event {e.Id} to subscribers");

                foreach (var sub in subs)
                {
                    sub.Value.SendEvent(e);
                };
            }
        }
    }
}
