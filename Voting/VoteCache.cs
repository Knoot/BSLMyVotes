using System;
using System.Collections.Generic;
using BSLMyVotes.Models;

namespace BSLMyVotes.Voting
{
    // Published snapshots are immutable; failed loads never retain stale classifications.
    public sealed class VoteCache
    {
        private readonly Dictionary<string, VoteState> votes;
        public HistoryStatus Status { get; }
        public int Count => votes.Count;
        public VoteCache(HistoryStatus status, Dictionary<string, VoteState> entries = null)
        { Status = status; votes = entries ?? new Dictionary<string, VoteState>(StringComparer.OrdinalIgnoreCase); }
        public VoteState Get(string hash) => hash != null && votes.TryGetValue(hash, out var vote) ? vote : VoteState.Unknown;
        public static bool IsHash(string value)
        {
            if (value == null || value.Length != 40) return false;
            foreach (char c in value)
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')) return false;
            return true;
        }
        public static string HashFromLevelId(string id)
        {
            const string prefix = "custom_level_";
            if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal) || id.Length < prefix.Length + 40) return null;
            // SongCore may append a space and a duplicate/WIP suffix.
            if (id.Length > 53 && id[53] != ' ') return null;
            string hash = id.Substring(prefix.Length, 40);
            return IsHash(hash) ? hash : null;
        }
    }
}
