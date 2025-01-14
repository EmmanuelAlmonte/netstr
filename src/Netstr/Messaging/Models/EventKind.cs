﻿namespace Netstr.Messaging.Models
{
    public static class EventKind
    {
        // Basic event kinds
        public static int UserMetadata = 0;
        public static int Delete = 5;
        public static int RequestToVanish = 62;
        public static int GiftWrap = 1059;
        public static int Auth = 22242;

        // NIP-51 Standard Lists (10000-10999)
        public static int MuteList = 10000;
        public static int PinnedNotes = 10001;
        public static int Bookmarks = 10003;
        public static int Communities = 10004;
        public static int PublicChats = 10005;
        public static int BlockedRelays = 10006;
        public static int SearchRelays = 10007;
        public static int SimpleGroups = 10009;
        public static int Interests = 10015;
        public static int Emojis = 10030;
        public static int DmRelays = 10050;
        public static int GoodWikiAuthors = 10101;
        public static int GoodWikiRelays = 10102;

        // NIP-51 Sets (30000-30999)
        public static int FollowSets = 30000;
        public static int RelaySets = 30002;
        public static int BookmarkSets = 30003;
        public static int ArticleCurationSets = 30004;
        public static int VideoCurationSets = 30005;
        public static int KindMuteSets = 30007;
        public static int InterestSets = 30015;
        public static int EmojiSets = 30030;
        public static int ReleaseArtifactSets = 30063;
        public static int AppCurationSets = 30267;
    }
}
