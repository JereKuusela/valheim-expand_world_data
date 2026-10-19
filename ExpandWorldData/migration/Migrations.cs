using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.AltBiomes;
using ExpandWorldData.Data;
using ExpandWorldData.Dungeon;
using ExpandWorldData.World;
using Service;

namespace ExpandWorldData.Migration;

///<summary>One-time yaml upgrades, done separately from data loading. The hidden revision tells which upgrades have been done.</summary>
public static class Migrations
{
  // Increase when adding a step, and give the step a check for the new revision. Steps must also be safe to repeat.
  public const int Latest = 2;

  public static int ReadRevision(string path)
  {
    if (!File.Exists(path)) return Latest;
    var section = "";
    foreach (var line in File.ReadLines(path))
    {
      var text = line.Trim();
      if (text.Length == 0 || text.StartsWith("#")) continue;
      if (text.StartsWith("[") && text.EndsWith("]"))
      {
        section = text.Substring(1, text.Length - 2).Trim();
        continue;
      }
      if (section != "3. Data") continue;
      var separator = text.IndexOf('=');
      if (separator < 0 || text.Substring(0, separator).Trim() != "Data revision") continue;
      return int.TryParse(text.Substring(separator + 1).Trim(), out var revision) ? revision : 0;
    }
    return 0;
  }

  public static bool Pending => YamlDataManager.MigrationEnabled && Configuration.configDataRevision.Value < Latest;

  [Flags]
  private enum Stage { Files = 1, Vanilla = 2, Dungeons = 4, Spawns = 8, All = Files | Vanilla | Dungeons | Spawns }
  private static Stage Completed;

  public static void Reset() => Completed = 0;

  public static void BackupFile(string file)
  {
    Directory.CreateDirectory(Yaml.BackupDirectory);
    var name = $"{Path.GetFileName(file)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
    var backup = Path.Combine(Yaml.BackupDirectory, $"{name}.bak");
    var suffix = 1;
    while (File.Exists(backup))
      backup = Path.Combine(Yaml.BackupDirectory, $"{name}_{suffix++}.bak");
    File.Copy(file, backup);
    Log.Info($"Backed up {file} to {backup} before migration.");
  }

  private static void Apply(Stage stage, Action migrate)
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
  public static void RunFiles() => Apply(Stage.Files, () =>
  {
    Finish(AltBiomeManager.Pattern, Yaml.RemoveFields(AltBiomeManager.Pattern, ["addVegetation", "addLocations"]),
      "Removed obsolete addVegetation and addLocations from alt biome files. These are handled by the vegetation and location files.");
    Finish(Event.EventManager.Pattern, EventMigration.Migrate(Event.EventManager.Pattern),
      "Migrated event keys to the new row format (notRequired* is now forbidden*, requiredPlayerKeysAll merged to requiredPlayerKeys).");
    Read<Event.Data>(Event.EventManager.Pattern);
    Finish(Spawn.SpawnManager.Pattern, Yaml.RenameFields(Spawn.SpawnManager.Pattern, SpawnMigration.Renames),
      "Renamed spawn requirement fields to plural (for example requiredGlobalKey is now requiredGlobalKeys).");
    Finish(Vegetation.VegetationManager.Pattern, Yaml.RenameFields(Vegetation.VegetationManager.Pattern, VegetationMigration.Renames),
      "Renamed vegetation requiredGlobalKey and forbiddenGlobalKey to plural.");
    Finish(WorldManager.Pattern, WorldManager.MigrateOldDefault(), null);
  });

  // Steps that backfill values from the vanilla data. Must run after the vanilla data is known and before the files are read.
  public static void RunVanilla() => Apply(Stage.Vanilla, () =>
  {
    EnvironmentMigration.Apply();
    AltBiomeMigration.Apply();
    LocationMigration.Apply();
  });

  public static void RunDungeons(Dictionary<string, DungeonGenerator> originals) => Apply(Stage.Dungeons, () =>
  {
    var pattern = DungeonManager.Pattern;
    Read<DungeonYaml>(pattern);
    if (Yaml.HasField(pattern, "addBaseSeedToRandomSpawn")) return;
    var values = originals.Where(entry => entry.Value.m_addBaseSeedToRandomSpawn).ToDictionary(entry => entry.Key, _ => "true");
    Finish(pattern, Yaml.InsertMissingField(pattern, "name", "addBaseSeedToRandomSpawn", values),
      $"Added addBaseSeedToRandomSpawn to {pattern} files.");
  });
  public static void RunSpawns(List<(SpawnSystemList List, List<SpawnSystem.SpawnData> Spawners)> defaults, Action reload) =>
    Apply(Stage.Spawns, () => SpawnMigration.MigratePersistentEvents(defaults.SelectMany(entry => entry.Spawners), reload));

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
