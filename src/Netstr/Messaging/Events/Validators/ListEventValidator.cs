using Microsoft.Extensions.Options;
using Netstr.Messaging.Models;
using Netstr.Options;
using System.Linq;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates NIP-51 list events.
    /// </summary>
    public class ListEventValidator : IEventValidator
    {
        private const string InvalidListTags = "invalid: list event missing required tags";
        private const string InvalidSetIdentifier = "invalid: set event missing 'd' tag identifier";

        public string? Validate(Event e, ClientContext context)
        {
            // Only validate list events
            if (!IsListEvent((long)e.Kind))
            {
                return null;
            }

            // Sets (30000-30999) require a 'd' tag
            if (IsSetEvent((long)e.Kind) && !HasDTag(e))
            {
                return InvalidSetIdentifier;
            }

            // Validate specific list types
            return ValidateListType(e);
        }

        private static bool IsListEvent(long kind)
        {
            return (kind >= 10000L && kind <= 10999L) || (kind >= 30000L && kind <= 30999L);
        }

        private static bool IsSetEvent(long kind)
        {
            return kind >= 30000L && kind <= 30999L;
        }

        private static bool HasDTag(Event e)
        {
            return e.Tags.Any(t => t.Length > 0 && t[0] == "d");
        }

        private static string? ValidateListType(Event e)
        {
            // Validate tags based on event kind
            return (EventKind)e.Kind switch
            {
                EventKind.MuteList => ValidateMuteList(e),
                EventKind.PinnedNotes => ValidatePinnedNotes(e),
                EventKind.Bookmarks => ValidateBookmarks(e),
                EventKind.Communities => ValidateCommunities(e),
                EventKind.PublicChats => ValidatePublicChats(e),
                EventKind.BlockedRelays or 
                EventKind.SearchRelays or 
                EventKind.DmRelays or
                EventKind.GoodWikiRelays => ValidateRelayList(e),
                EventKind.SimpleGroups => ValidateSimpleGroups(e),
                EventKind.Interests => ValidateInterests(e),
                EventKind.Emojis => ValidateEmojis(e),
                EventKind.GoodWikiAuthors => ValidateWikiAuthors(e),
                
                // Sets
                EventKind.FollowSets => ValidateFollowSet(e),
                EventKind.RelaySets => ValidateRelaySet(e),
                EventKind.BookmarkSets => ValidateBookmarkSet(e),
                EventKind.ArticleCurationSets or
                EventKind.VideoCurationSets => ValidateCurationSet(e),
                EventKind.KindMuteSets => ValidateKindMuteSet(e),
                EventKind.InterestSets => ValidateInterestSet(e),
                EventKind.EmojiSets => ValidateEmojiSet(e),
                EventKind.ReleaseArtifactSets => ValidateReleaseArtifactSet(e),
                EventKind.AppCurationSets => ValidateAppCurationSet(e),
                
                _ => null // Unknown list type, skip validation
            };
        }

        private static string? ValidateMuteList(Event e)
        {
            // Mute lists can contain p (pubkeys), t (hashtags), word (lowercase string), e (threads)
            var validTags = e.Tags.All(t => t.Length > 0 && (
                t[0] == "p" || t[0] == "t" || t[0] == "word" || t[0] == "e"
            ));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidatePinnedNotes(Event e)
        {
            // Pinned notes can only contain e (kind:1 notes)
            var validTags = e.Tags.All(t => t.Length > 0 && t[0] == "e");
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateBookmarks(Event e)
        {
            // Bookmarks can contain e (kind:1 notes), a (kind:30023 articles), t (hashtags), r (URLs)
            var validTags = e.Tags.All(t => t.Length > 0 && (
                t[0] == "e" || t[0] == "a" || t[0] == "t" || t[0] == "r"
            ));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateCommunities(Event e)
        {
            // Communities can only contain a (kind:34550 community definitions)
            var validTags = e.Tags.All(t => t.Length > 0 && t[0] == "a");
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidatePublicChats(Event e)
        {
            // Public chats can only contain e (kind:40 channel definitions)
            var validTags = e.Tags.All(t => t.Length > 0 && t[0] == "e");
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateRelayList(Event e)
        {
            // Relay lists can only contain relay (relay URLs)
            var validTags = e.Tags.All(t => t.Length > 0 && t[0] == "relay");
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateSimpleGroups(Event e)
        {
            // Simple groups can contain group (NIP-29 group id + relay URL + optional name) and r (relay URLs)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "group" || t[0] == "r"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateInterests(Event e)
        {
            // Interests can contain t (hashtags) and a (kind:30015 interest set)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "t" || t[0] == "a"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateEmojis(Event e)
        {
            // Emojis can contain emoji (NIP-30) and a (kind:30030 emoji set)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "emoji" || t[0] == "a"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateWikiAuthors(Event e)
        {
            // Wiki authors can only contain p (pubkeys)
            var validTags = e.Tags.All(t => t.Length > 0 && t[0] == "p");
            return validTags ? null : InvalidListTags;
        }

        // Set validators

        private static string? ValidateFollowSet(Event e)
        {
            // Follow sets can only contain p (pubkeys)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "p"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateRelaySet(Event e)
        {
            // Relay sets can only contain relay (relay URLs)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "relay"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateBookmarkSet(Event e)
        {
            // Bookmark sets can contain e (kind:1 notes), a (kind:30023 articles), t (hashtags), r (URLs)
            var validTags = e.Tags.All(t => t.Length > 0 && (
                t[0] == "d" || t[0] == "e" || t[0] == "a" || t[0] == "t" || t[0] == "r"
            ));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateCurationSet(Event e)
        {
            // Curation sets can contain a (articles/videos) and e (kind:1 notes)
            var validTags = e.Tags.All(t => t.Length > 0 && (
                t[0] == "d" || t[0] == "a" || t[0] == "e"
            ));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateKindMuteSet(Event e)
        {
            // Kind mute sets can only contain p (pubkeys)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "p"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateInterestSet(Event e)
        {
            // Interest sets can only contain t (hashtags)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "t"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateEmojiSet(Event e)
        {
            // Emoji sets can only contain emoji (NIP-30)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "emoji"));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateReleaseArtifactSet(Event e)
        {
            // Release artifact sets can contain e (kind:1063 file metadata) and a (software application)
            var validTags = e.Tags.All(t => t.Length > 0 && (
                t[0] == "d" || t[0] == "e" || t[0] == "a"
            ));
            return validTags ? null : InvalidListTags;
        }

        private static string? ValidateAppCurationSet(Event e)
        {
            // App curation sets can only contain a (software application)
            var validTags = e.Tags.All(t => t.Length > 0 && (t[0] == "d" || t[0] == "a"));
            return validTags ? null : InvalidListTags;
        }
    }
}
