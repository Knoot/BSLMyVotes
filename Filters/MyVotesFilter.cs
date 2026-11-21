using System.Threading;
using System.Threading.Tasks;
using BetterSongList.FilterModels;
using BetterSongList.Interfaces;
using BSLMyVotes.Models;
using BSLMyVotes.Voting;

namespace BSLMyVotes.Filters
{
    public enum VoteFilterMode { Liked, Disliked, Voted, NotVoted }
    public sealed class MyVotesFilter : IFilter, ITransformerPlugin
    {
        private readonly VoteDataProvider provider;
        private readonly VoteFilterMode mode;
        private VoteCache cache;
        public MyVotesFilter(VoteDataProvider provider, VoteFilterMode mode)
        { this.provider = provider; this.mode = mode; cache = provider.Snapshot; }
        public string name => mode == VoteFilterMode.Liked ? "My Likes" : mode == VoteFilterMode.Disliked ? "My Dislikes" : mode == VoteFilterMode.Voted ? "My Voted" : "My Not Voted";
        public bool visible => true;
        // BSL queries readiness before applying the predicate: capture one consistent snapshot.
        public bool isReady { get { cache = provider.Snapshot; return true; } }
        public Task Prepare(CancellationToken token) => provider.RefreshAsync(token);
        public void ContextSwitch(SelectLevelCategoryViewController.LevelCategory category, BeatmapLevelPack pack) { cache = provider.Snapshot; }
        public bool GetValueFor(BeatmapLevel level) => Matches(cache, level?.levelID, mode);
        public static bool Matches(VoteCache cache, string levelId, VoteFilterMode mode)
        {
            if (cache.Status != HistoryStatus.Available) return false;
            string hash = VoteCache.HashFromLevelId(levelId);
            if (hash == null) return false;
            var vote = cache.Get(hash);
            switch (mode)
            {
                case VoteFilterMode.Liked: return vote == VoteState.Upvoted;
                case VoteFilterMode.Disliked: return vote == VoteState.Downvoted;
                case VoteFilterMode.Voted: return vote != VoteState.Unknown;
                default: return vote == VoteState.Unknown;
            }
        }
    }
}
