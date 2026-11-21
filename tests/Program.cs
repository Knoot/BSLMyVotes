using System.Diagnostics;
using BSLMyVotes.Voting;
using BSLMyVotes.Models;
using BSLMyVotes.Filters;
using Newtonsoft.Json;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
string hash = new string('a', 40), other = new string('b', 40);
string Json(string vote) => "{\"" + hash + "\":{\"hash\":\"" + hash + "\",\"voteType\":\"" + vote + "\"}}";
bool Match(VoteCache cache, string h, VoteFilterMode mode) => MyVotesFilter.Matches(cache, "custom_level_" + h, mode);
var liked = VoteFileParser.Parse(Json("Upvote"));
Check(Match(liked, hash.ToUpperInvariant(), VoteFilterMode.Liked), "case insensitive likes");
Check(!Match(liked, hash, VoteFilterMode.Disliked), "likes not dislikes");
Check(Match(liked, hash, VoteFilterMode.Voted), "voted");
Check(Match(liked, other, VoteFilterMode.NotVoted), "unknown hash");
Check(!Match(liked, other, VoteFilterMode.Liked), "different map version");
Check(!MyVotesFilter.Matches(liked, "ost_" + hash, VoteFilterMode.NotVoted), "exclude OST");
Check(!MyVotesFilter.Matches(liked, "custom_level_" + hash + "bad", VoteFilterMode.Liked), "reject malformed ID");
foreach (var status in new[] {HistoryStatus.Missing, HistoryStatus.Corrupt, HistoryStatus.Unreadable})
 foreach (VoteFilterMode mode in Enum.GetValues<VoteFilterMode>()) Check(!Match(new VoteCache(status), other, mode), "fail closed " + status);
foreach (string bad in new[] {"{", "null", "[]", Json("Unknown"), Json("Upvote").Replace("\"hash\":\"" + hash, "\"hash\":\"" + other), Json("Upvote") + " {}", "{\""+hash+"\":{},\""+hash+"\":{}}"})
{ try { VoteFileParser.Parse(bad); throw new Exception("Accepted corrupt JSON"); } catch (JsonException) { checks++; } }
string dir = Path.Combine(Path.GetTempPath(), "BSLMyVotes-test-" + Guid.NewGuid());
Directory.CreateDirectory(dir);
try
{
 string path = Path.Combine(dir, "votes.json");
 var warnings = new List<string>();
 using var provider = new VoteDataProvider(path, warnings.Add);
 provider.Refresh(); provider.Refresh();
 Check(provider.Snapshot.Status == HistoryStatus.Missing && warnings.Count == 1, "missing and log dedup");
 foreach (string vote in new[] {"Upvote", "Downvote", "Upvote"})
 {
  File.WriteAllText(path, Json(vote)); await provider.RefreshAsync(default);
  Check(Match(provider.Snapshot, hash, vote == "Upvote" ? VoteFilterMode.Liked : VoteFilterMode.Disliked), "vote transition");
 }
 File.WriteAllText(path, "{"); provider.Refresh();
 Check(provider.Snapshot.Status == HistoryStatus.Corrupt && provider.Snapshot.Count == 0, "corrupt clears old data");
 File.WriteAllText(path, "{}"); provider.Refresh();
 Check(provider.Snapshot.Status == HistoryStatus.Available && Match(provider.Snapshot, hash, VoteFilterMode.NotVoted), "empty history recovery");
 File.WriteAllText(path, Json("Downvote"));
 provider.Start();
 var deadline = Stopwatch.StartNew();
 while (provider.Snapshot.Get(hash) != VoteState.Downvoted && deadline.ElapsedMilliseconds < 4000) await Task.Delay(20);
 Check(provider.Snapshot.Get(hash) == VoteState.Downvoted, "background reload");
 File.Delete(path); provider.Refresh();
 Check(provider.Snapshot.Status == HistoryStatus.Missing, "deleted history");
}
finally { Directory.Delete(dir, true); }
var entries = new Dictionary<string, VoteState>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < 10000; i++) entries.Add(i.ToString("x40"), i % 2 == 0 ? VoteState.Upvoted : VoteState.Downvoted);
var large = new VoteCache(HistoryStatus.Available, entries);
var ids = Enumerable.Range(0, 5000).Select(i => "custom_level_" + i.ToString("x40")).ToArray();
int count = 0;
for (int run = 0; run < 10; run++) foreach (string id in ids) MyVotesFilter.Matches(large, id, VoteFilterMode.Liked);
var watch = Stopwatch.StartNew();
foreach (string id in ids) if (MyVotesFilter.Matches(large, id, VoteFilterMode.Liked)) count++;
watch.Stop();
Check(count == 2500, "5000 map result");
Check(watch.Elapsed.TotalMilliseconds < 100, "performance budget");
Console.WriteLine($"PASS: {checks} checks. 5000 maps / 10000 votes: {watch.Elapsed.TotalMilliseconds:F3} ms ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}).");
