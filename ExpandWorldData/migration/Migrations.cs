using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData;

///<summary>One-time yaml upgrades, done separately from data loading. The hidden revision tells which upgrades have been done.</summary>
public static class Migrations
{
  // Increase when adding a step, and give the step a check for the new revision. Steps must also be safe to repeat.
  public const int Latest = 1;

  public static bool Pending => YamlDataManager.MigrationEnabled && Configuration.configDataRevision.Value < Latest;

  [Flags]
  private enum Stage { Files = 1, Vanilla = 2, Dungeons = 4, Spawns = 8, All = Files | Vanilla | Dungeons | Spawns }
  private static Stage Completed;

  public static void Reset() => Completed = 0;

  private static void Run(Stage stage, Action migrate)
  {
    if (!Pending || (Completed & stage) != 0) return;
    try
    {
      migrate();
      Completed |= stage;
      Complete();
    }
    catch (Exception exception)
    {
      Log.Error($"Data migration ({stage}) failed: {exception.Message}");
      Log.Error(exception.StackTrace);
    }
  }

  public static Dictionary<string, List<T>> ReadFiles<T>(string pattern)
  {
    Dictionary<string, List<T>> result = [];
    if (!Directory.Exists(Yaml.BaseDirectory)) return result;
    foreach (var file in Directory.GetFiles(Yaml.BaseDirectory, pattern, SearchOption.AllDirectories).Reverse())
    {
      if (!Yaml.TryDeserialize<T>(File.ReadAllText(file), file, out var data))
        throw new InvalidDataException($"Unable to migrate {file}.");
      result.Add(file, data);
    }
    return result;
  }

  public static List<T> Read<T>(string pattern) => ReadFiles<T>(pattern).Values.SelectMany(data => data).ToList();

  // Steps that only need the files, so they can run before anything is loaded.
  public static void RunFiles() => Run(Stage.Files, () =>
  {
    Finish(AltBiomeManager.Pattern, Yaml.RemoveFields(AltBiomeManager.Pattern, ["addVegetation", "addLocations"]),
      "Removed obsolete addVegetation and addLocations from alt biome files. These are handled by the vegetation and location files.");
    Read<ExpandWorld.Event.Data>(ExpandWorld.Event.EventManager.Pattern);
    Finish(ExpandWorld.Event.EventManager.Pattern, ExpandWorld.Event.EventMigration.Migrate(ExpandWorld.Event.EventManager.Pattern),
      "Migrated event keys to the new row format (notRequired* is now forbidden*, requiredPlayerKeysAll merged to requiredPlayerKeys).");
    Finish(WorldManager.Pattern, WorldManager.MigrateOldDefault(), null);
  });

  // Steps that backfill values from the vanilla data. Must run after the vanilla data is known and before the files are read.
  public static void RunVanilla() => Run(Stage.Vanilla, () =>
  {
    EnvironmentMigration.Run();
    AltBiomeMigration.Run();
    LocationMigration.Run();
  });

  public static void RunDungeons(Dictionary<string, DungeonGenerator> originals) => Run(Stage.Dungeons, () =>
  {
    var pattern = Dungeon.DungeonManager.Pattern;
    Read<DungeonYaml>(pattern);
    if (Yaml.HasField(pattern, "addBaseSeedToRandomSpawn")) return;
    var values = originals.Where(entry => entry.Value.m_addBaseSeedToRandomSpawn).ToDictionary(entry => entry.Key, _ => "true");
    Finish(pattern, Yaml.InsertMissingField(pattern, "name", "addBaseSeedToRandomSpawn", values),
      $"Added addBaseSeedToRandomSpawn to {pattern} files.");
  });
  public static void RunSpawns(Action migrate) => Run(Stage.Spawns, migrate);

  // Called once everything is migrated.
  public static void Complete()
  {
    if (!Pending || Completed != Stage.All) return;
    Configuration.configDataRevision.Value = Latest;
    Log.Info($"Data migrations finished (revision {Latest}).");
  }

  public static void Finish(string pattern, bool changed, string? message = null)
  {
    if (!changed) return;
    if (message != null) Log.Warning(message);
    // Otherwise the file watcher would reload the data.
    Yaml.IgnoreWrites(pattern);
  }
}
