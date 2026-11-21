using System;
using System.IO;
using IPA;
using IPA.Logging;
using BSLMyVotes.Filters;
using BSLMyVotes.Voting;

namespace BSLMyVotes
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public sealed class Plugin
    {
        private readonly Logger log;
        private VoteDataProvider provider;
        [Init] public Plugin(Logger logger) { log = logger; }
        [OnStart] public void Start()
        {
            provider = new VoteDataProvider(Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "votedSongs.json"), message => log.Warn(message));
            provider.Start();
            foreach (VoteFilterMode mode in Enum.GetValues(typeof(VoteFilterMode)))
                if (!BetterSongList.FilterMethods.Register(new MyVotesFilter(provider, mode)))
                    log.Warn("BetterSongList has disabled plugin filters. Enable AllowPluginSortsAndFilters and restart the game.");
        }
        [OnExit] public void Exit() { provider?.Dispose(); }
    }
}
