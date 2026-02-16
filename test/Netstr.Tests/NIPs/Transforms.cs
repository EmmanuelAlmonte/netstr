using Netstr.Messaging;
using Netstr.Messaging.Models;
using Netstr.Options;
using System.IO;
using System.Linq;
using System.Text.Json;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Netstr.Tests.NIPs
{
    [Binding]
    public class Transforms
    {
        [StepArgumentTransformation]
        public IEnumerable<SubscriptionFilterRequest> CreateSubscriptionFilters(Table table)
        {
            return table.CreateSet<SubscriptionFilterRequest>().Select((x, i) =>
            {
                var since = table.Rows[i].GetInt64("Since");
                var until = table.Rows[i].GetInt64("Until");
                return x with
                {
                    AdditionalData = table.Rows[i]
                        .Where(x => (x.Key.StartsWith("#") || x.Key.StartsWith("&")) && !string.IsNullOrEmpty(x.Value))
                        .ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(x.Value.Split(",")))),
                    Since = since > 0 ? DateTimeOffset.FromUnixTimeSeconds(since) : null,
                    Until = since > 0 ? DateTimeOffset.FromUnixTimeSeconds(until) : null,
                };
            });
        }

        [StepArgumentTransformation]
        public IEnumerable<object[]> CreateEventIds(Table table)
        {
            return table.Rows.Select<TableRow, object[]>(row =>
            {
                var messageType = row.GetString("Type");
                var subscriptionId = GetPayloadId(row, "Id", "EventId");

                var eventId = row.TryGetValue("EventId", out var idValue) ? idValue ?? string.Empty : string.Empty;
                var message = row.TryGetValue("Message", out var messageValue) ? messageValue ?? string.Empty : string.Empty;
                var notice = row.TryGetValue("Notice", out var noticeValue) ? noticeValue ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(notice) && row.TryGetValue("EventId", out var eventIdNoticeValue))
                {
                    notice = eventIdNoticeValue ?? string.Empty;
                }

                return messageType switch
                {
                MessageType.Event => [MessageType.Event, subscriptionId, eventId],
                MessageType.EndOfStoredEvents => [MessageType.EndOfStoredEvents, subscriptionId],
                MessageType.Ok => [MessageType.Ok, subscriptionId, row.GetBoolean("Success"), message],
                MessageType.Closed => [MessageType.Closed, subscriptionId, message],
                MessageType.Auth => [MessageType.Auth, subscriptionId],
                MessageType.Count => [MessageType.Count, subscriptionId, row.GetInt32("Count")],
                MessageType.Notice => [MessageType.Notice, "", notice],
                _ => throw new NotImplementedException($"Unsupported message type: {messageType}"),
                };
            });
        }

        private static string GetPayloadId(TableRow row, string firstKey, string secondKey)
        {
            return row.TryGetValue(firstKey, out var value) ? value ?? string.Empty : row.GetString(secondKey);
        }

        [StepArgumentTransformation]
        public Keys CreateKeys(Table table)
        {
            return table.CreateInstance<Keys>();
        }

        [StepArgumentTransformation]
        public Dictionary<string, string> CreateHeaders(Table table)
        {
            return table.Rows.ToDictionary(row => row.GetString("Header"), row => row.GetString("Value"));
        }

        public static IEnumerable<Event> CreateEvents(Table table, Client c)
        {
            var debugFile = Environment.GetEnvironmentVariable("NETSTR_TEST_DEBUG_FILE");

            return table.CreateSet<Event>().Select((e, i) =>
            {
                var providedId = table.Rows[i].GetString("Id");
                var tags = table.Rows[i].GetString("Tags");
                var providedSignature = table.Rows[i].GetString("Signature");
                var hasExplicitSignature = !string.IsNullOrWhiteSpace(providedSignature) && providedSignature != "*";
                var hasExplicitId = !string.IsNullOrWhiteSpace(providedId) && providedId != "*";
                if (Environment.GetEnvironmentVariable("NETSTR_TEST_DEBUG_TRANSFORM") == "1")
                {
                    Console.WriteLine(
                        $"Transform row={i} rawId={providedId ?? "<null>"} rawSig={providedSignature ?? "<null>"} hasExplicitId={hasExplicitId} hasExplicitSignature={hasExplicitSignature}");
                }
                if (!string.IsNullOrWhiteSpace(debugFile))
                {
                    File.AppendAllText(
                        debugFile,
                        $"Transform row={i} client={c.Keys.PublicKey} rawId={providedId ?? "<null>"} rawSig={providedSignature ?? "<null>"} hasExplicitId={hasExplicitId} hasExplicitSignature={hasExplicitSignature}{Environment.NewLine}");
                }

                var updatedEvent = e with
                {
                    Content = e.Content?.Replace("\\b", "\b").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\n", "\n") ?? "",
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(table.Rows[i].GetInt64("CreatedAt")),
                    PublicKey = string.IsNullOrEmpty(e.PublicKey) ? c.Keys.PublicKey : e.PublicKey,
                    Tags = string.IsNullOrWhiteSpace(tags)
                        ? []
                        : JsonSerializer.Deserialize<string[][]>(tags) ?? []
                };

                if ((!hasExplicitId || IsSyntheticId(providedId)) && !IsInvalidSignatureValue(providedSignature))
                {
                    // Wildcard or synthetic placeholder IDs are intentionally synthetic and should be
                    // recomputed, unless an explicit invalid signature marker is asserted in the table.
                    return Helpers.FinalizeEvent(updatedEvent, c.Keys.PrivateKey);
                }

                var canonicalId = Helpers.GenerateId(updatedEvent);
                if (!string.Equals(providedId, canonicalId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasExplicitSignature)
                    {
                        return Helpers.FinalizeEvent(updatedEvent, c.Keys.PrivateKey);
                    }
                }

                var explicitSignature = !hasExplicitSignature
                    ? Helpers.Sign(providedId, c.Keys.PrivateKey)
                    : providedSignature;

                return updatedEvent with
                {
                    Id = providedId,
                    Signature = explicitSignature,
                };
            });
        }

        private static bool IsSyntheticId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "*")
            {
                return true;
            }

            return id.Length >= 16 && id.All(x => x == id[0]);
        }

        private static bool IsInvalidSignatureValue(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            return signature.Equals("Invalid", StringComparison.OrdinalIgnoreCase);
        }

    }
}
