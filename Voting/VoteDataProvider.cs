using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BSLMyVotes.Models;

namespace BSLMyVotes.Voting
{
    public sealed class VoteDataProvider : IDisposable
    {
        private readonly string path;
        private readonly Action<string> warn;
        private readonly object gate = new object();
        private Timer timer;
        private bool disposed;
        private string previousText;
        private string lastWarning;
        private VoteCache snapshot = new VoteCache(HistoryStatus.Missing);
        public VoteCache Snapshot => Volatile.Read(ref snapshot);
        public VoteDataProvider(string path, Action<string> warn) { this.path = path; this.warn = warn; }
        public void Start() { timer = new Timer(_ => Refresh(), null, 0, 1000); }
        public Task RefreshAsync(CancellationToken token) => Task.Run(() => { token.ThrowIfCancellationRequested(); Refresh(); }, token);
        public void Refresh()
        {
            lock (gate)
            {
                if (disposed) return;
                try
                {
                    // Background only, one read per history, never per song. Reading also detects
                    // rewrites with identical timestamp/length on coarse filesystems.
                    string text;
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(stream)) text = reader.ReadToEnd();
                    if (text == previousText && Snapshot.Status == HistoryStatus.Available) return;
                    var loaded = VoteFileParser.Parse(text);
                    previousText = text;
                    Volatile.Write(ref snapshot, loaded);
                    lastWarning = null;
                }
                catch (FileNotFoundException) { Fail(HistoryStatus.Missing); }
                catch (DirectoryNotFoundException) { Fail(HistoryStatus.Missing); }
                catch (JsonException) { Fail(HistoryStatus.Corrupt); }
                catch (IOException) { Fail(HistoryStatus.Unreadable); }
                catch (UnauthorizedAccessException) { Fail(HistoryStatus.Unreadable); }
                catch (ArgumentException) { Fail(HistoryStatus.Corrupt); }
            }
        }
        private void Fail(HistoryStatus status)
        {
            previousText = null;
            Volatile.Write(ref snapshot, new VoteCache(status));
            string message = "Personal vote history: " + status + ". My Votes results are empty until recovery.";
            if (message != lastWarning) { lastWarning = message; warn(message); }
        }
        public void Dispose() { lock (gate) { disposed = true; timer?.Dispose(); } }
    }
}
