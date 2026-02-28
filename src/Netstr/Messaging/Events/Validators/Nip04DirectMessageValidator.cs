using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates NIP-04 encrypted direct messages (kind 4).
    /// </summary>
    public class Nip04DirectMessageValidator : IEventValidator
    {
        private const string InvalidNip04MissingRecipient = "invalid: nip-04 dm missing recipient tag";
        private const string InvalidNip04ContentFormat = "invalid: nip-04 dm content must be '<ciphertext>?iv=<iv>'";

        public string? Validate(Event e, ClientContext context)
        {
            if (e.Kind != (long)EventKind.EncryptedDirectMessage)
            {
                return null;
            }

            var hasRecipient = e.Tags.Any(t =>
                t.Length > 1 &&
                t[0] == EventTag.PublicKey &&
                !string.IsNullOrWhiteSpace(t[1]));

            if (!hasRecipient)
            {
                return InvalidNip04MissingRecipient;
            }

            if (!HasValidContentFormat(e.Content))
            {
                return InvalidNip04ContentFormat;
            }

            return null;
        }

        private static bool HasValidContentFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var ivIndex = content.IndexOf("?iv=", StringComparison.Ordinal);
            if (ivIndex <= 0)
            {
                return false;
            }

            return ivIndex + 4 < content.Length;
        }
    }
}
