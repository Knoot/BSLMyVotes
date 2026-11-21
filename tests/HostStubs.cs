// Minimal host types let the production predicate run without launching Unity.
public class BeatmapLevel { public string levelID; }
public class BeatmapLevelPack { }
public class SelectLevelCategoryViewController { public enum LevelCategory { All } }
namespace BetterSongList.FilterModels { public interface IFilter { bool isReady { get; } System.Threading.Tasks.Task Prepare(System.Threading.CancellationToken token); bool GetValueFor(BeatmapLevel level); } }
namespace BetterSongList.Interfaces { public interface ITransformerPlugin { string name { get; } bool visible { get; } void ContextSwitch(SelectLevelCategoryViewController.LevelCategory category, BeatmapLevelPack pack); } }
