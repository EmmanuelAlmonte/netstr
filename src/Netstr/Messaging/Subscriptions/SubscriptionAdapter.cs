using Netstr.Messaging.Models;
using System.Threading.Channels;

namespace Netstr.Messaging.Subscriptions
{
    public class SubscriptionAdapter : IDisposable
    {
        private readonly IWebSocketAdapter webSocketAdapter;
        private readonly string subscriptionId;
        private readonly Channel<Event> eventsQueue;
        private MessageBatch? storedEventsBatch;

        public SubscriptionAdapter(IWebSocketAdapter webSocketAdapter, string subscriptionId, SubscriptionFilter[] filters, int maxQueueSize)
        {
            this.webSocketAdapter = webSocketAdapter;
            this.subscriptionId = subscriptionId;
            this.eventsQueue = Channel.CreateBounded<Event>(
                new BoundedChannelOptions(maxQueueSize)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });

            Filters = filters;
        }

        public SubscriptionFilter[] Filters { get; }

        public bool StoredEventsSent => this.storedEventsBatch != null;

        public void SendEvent(Event e)
        {
            if (StoredEventsSent)
            {
                this.webSocketAdapter.Send(EventToMessage(e));
                return;
            }

            // Bounded channel - drops oldest automatically when full
            this.eventsQueue.Writer.TryWrite(e);
        }

        public void SendStoredEvents(IEnumerable<Event> events)
        {
            if (StoredEventsSent)
            {
                throw new InvalidOperationException($"Cannot call {nameof(SendStoredEvents)} method twice");
            }

            var storedMessages = events.Select(EventToMessage).ToArray();

            // Drain queued events that arrived before stored events were sent
            var dequeuedMessages = new List<object[]>();
            while (this.eventsQueue.Reader.TryRead(out var ev))
            {
                dequeuedMessages.Add(EventToMessage(ev));
            }

            // stored events, EOSE, queue events
            var batch = new MessageBatch(this.subscriptionId, [
                ..storedMessages,
                [
                    MessageType.EndOfStoredEvents,
                    this.subscriptionId
                ],
                ..dequeuedMessages
            ]);

            this.webSocketAdapter.Send(batch);

            this.storedEventsBatch = batch;

            // Drain any late arrivals after sending the initial batch
            if (!batch.IsCancelled)
            {
                var lateMessages = new List<object[]>();
                while (this.eventsQueue.Reader.TryRead(out var ev))
                {
                    lateMessages.Add(EventToMessage(ev));
                }

                if (lateMessages.Count > 0)
                {
                    this.webSocketAdapter.Send(new MessageBatch(this.subscriptionId, [.. lateMessages]));
                }
            }
        }

        public void Dispose()
        {
            this.storedEventsBatch?.Cancel();
            this.eventsQueue.Writer.TryComplete();
        }

        private object[] EventToMessage(Event e)
        {
            return [
                MessageType.Event,
                this.subscriptionId,
                e
            ];
        }
    }
}
