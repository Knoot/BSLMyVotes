using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BSLMyVotes.Models;

namespace BSLMyVotes.Voting
{
    public static class VoteFileParser
    {
        public static VoteCache Parse(string json)
        {
            // Reject duplicates rather than silently choosing an ambiguous vote.
            JObject root;
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new JsonException("Trailing JSON content.");
            }
            var entries = new Dictionary<string, VoteState>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in root.Properties())
            {
                if (!VoteCache.IsHash(p.Name) || !(p.Value is JObject record)) throw new JsonException("Invalid vote record.");
                string hash = record["hash"]?.Type == JTokenType.String ? (string)record["hash"] : null;
                if (!string.Equals(hash, p.Name, StringComparison.OrdinalIgnoreCase)) throw new JsonException("Hash mismatch.");
                var token = record["voteType"];
                string value = token?.Type == JTokenType.String ? (string)token : null;
                VoteState vote;
                if (value == "Upvote") vote = VoteState.Upvoted;
                else if (value == "Downvote") vote = VoteState.Downvoted;
                else throw new JsonException("Unknown vote type.");
                if (entries.ContainsKey(hash)) throw new JsonException("Duplicate hash.");
                entries.Add(hash, vote);
            }
            return new VoteCache(HistoryStatus.Available, entries);
        }
    }
}
