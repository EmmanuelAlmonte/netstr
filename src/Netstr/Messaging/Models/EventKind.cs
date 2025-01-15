﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿namespace Netstr.Messaging.Models
{
/// <summary>
/// Represents the different kinds of events in the NOSTR protocol.
/// </summary>
public enum EventKind
{
    // Basic event kinds
    UserMetadata = 0,
    Delete = 5,
    RequestToVanish = 62,
    GiftWrap = 1059,
    Auth = 22242,

    // NIP-51 Standard Lists (10000-10999)
    MuteList = 10000,
    PinnedNotes = 10001,
    Bookmarks = 10003,
    Communities = 10004,
    PublicChats = 10005,
    BlockedRelays = 10006,
    SearchRelays = 10007,
    SimpleGroups = 10009,
    Interests = 10015,
    Emojis = 10030,
    DmRelays = 10050,
    GoodWikiAuthors = 10101,
    GoodWikiRelays = 10102,

    // NIP-51 Sets (30000-30999)
    FollowSets = 30000,
    RelaySets = 30002,
    BookmarkSets = 30003,
    ArticleCurationSets = 30004,
    VideoCurationSets = 30005,
    KindMuteSets = 30007,
    InterestSets = 30015,
    EmojiSets = 30030,
    ReleaseArtifactSets = 30063,
    AppCurationSets = 30267
}

/// <summary>
/// Extension methods for working with EventKind values.
/// </summary>
public static class EventKindExtensions
{
    /// <summary>
    /// Converts an EventKind to its long value.
    /// </summary>
    public static long ToLong(this EventKind kind) => (long)kind;

    /// <summary>
    /// Converts a long value to an EventKind.
    /// </summary>
    public static EventKind ToEventKind(this long value) => (EventKind)value;
}
}
