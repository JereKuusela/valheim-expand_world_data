using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ExpandWorldData.Migration;
using NUnit.Framework;
using Service;

namespace ExpandWorldData.Tests;

[SetUpFixture]
public class MigrationTestEnvironment
{
  private string? OriginalConfigPath;

  [OneTimeSetUp]
  public void InitializeBepInEx()
  {
    OriginalConfigPath = Paths.ConfigPath;
    typeof(Paths).GetProperty(nameof(Paths.ConfigPath))!.SetValue(null, Path.GetTempPath());
  }

  [OneTimeTearDown]
  public void RestoreBepInEx()
    => typeof(Paths).GetProperty(nameof(Paths.ConfigPath))!.SetValue(null, OriginalConfigPath);
}

[TestFixture]
[NonParallelizable]
public class MigrationBackupTests
{
  private string DirectoryPath = null!;
  private string OriginalBaseDirectory = null!;
  private string OriginalBackupDirectory = null!;
  private ManualLogSource Logger = null!;
  private readonly List<string> Messages = [];
  private string FilePath => Path.Combine(Yaml.BaseDirectory, "expand_test.yaml");

  [SetUp]
  public void SetUp()
  {
    DirectoryPath = Path.Combine(Path.GetTempPath(), "ExpandWorldData.Tests", Guid.NewGuid().ToString("N"));
    OriginalBaseDirectory = Yaml.BaseDirectory;
    OriginalBackupDirectory = Yaml.BackupDirectory;
    Yaml.BaseDirectory = Path.Combine(DirectoryPath, "expand_world", "nested");
    Yaml.BackupDirectory = Path.Combine(DirectoryPath, "expand_world_backups");
    Directory.CreateDirectory(Yaml.BaseDirectory);
    Messages.Clear();
    Logger = new ManualLogSource("MigrationBackupTests");
    Logger.LogEvent += (_, args) => Messages.Add(args.Data.ToString());
    Log.Init(Logger);
  }

  [TearDown]
  public void TearDown()
  {
    Yaml.BaseDirectory = OriginalBaseDirectory;
    Yaml.BackupDirectory = OriginalBackupDirectory;
    Logger.Dispose();
    Directory.Delete(DirectoryPath, true);
  }

  private static bool Migrate(string migration) => migration switch
  {
    "event" => EventMigration.Migrate("expand_test.yaml"),
    "remove" => Yaml.RemoveFields("expand_test.yaml", ["notRequiredGlobalKeys"]),
    "insert" => Yaml.InsertMissingField("expand_test.yaml", "name", "newField", new() { ["test"] = "true" }),
    _ => throw new ArgumentException(migration),
  };

  [TestCase("event")]
  [TestCase("remove")]
  [TestCase("insert")]
  public void ChangedFileIsBackedUpAndLoggedBeforeMigration(string migration)
  {
    File.WriteAllText(FilePath, "# Original\r\n- name: test\r\n  notRequiredGlobalKeys: boss\r\n");
    var original = File.ReadAllBytes(FilePath);

    Assert.That(Migrate(migration), Is.True);

    var backups = Directory.GetFiles(Yaml.BackupDirectory);
    Assert.That(backups, Has.Length.EqualTo(1));
    Assert.That(File.ReadAllBytes(backups[0]), Is.EqualTo(original));
    Assert.That(File.ReadAllBytes(FilePath), Is.Not.EqualTo(original));
    Assert.That(Messages, Has.Some.Contains(backups[0]));
    Assert.That(Migrate(migration), Is.False);
    Assert.That(Directory.GetFiles(Yaml.BackupDirectory), Has.Length.EqualTo(1));
  }

  [TestCase("event")]
  [TestCase("remove")]
  [TestCase("insert")]
  public void UnchangedFileDoesNotCreateBackup(string migration)
  {
    File.WriteAllText(FilePath, "- name: test\n  newField: true\n");

    Assert.That(Migrate(migration), Is.False);
    Assert.That(Directory.Exists(Yaml.BackupDirectory), Is.False);
    Assert.That(Messages, Is.Empty);
  }

  [Test]
  public void RepeatedBackupsDoNotOverwriteEarlierCopies()
  {
    File.WriteAllText(FilePath, "original");
    Migrations.BackupFile(FilePath);
    var firstBackup = Directory.GetFiles(Yaml.BackupDirectory)[0];
    File.WriteAllText(FilePath, "edited");

    Migrations.BackupFile(FilePath);

    Assert.That(Directory.GetFiles(Yaml.BackupDirectory), Has.Length.EqualTo(2));
    Assert.That(File.ReadAllText(firstBackup), Is.EqualTo("original"));
  }

  [TestCase("event")]
  [TestCase("remove")]
  [TestCase("insert")]
  public void BackupFailureLeavesOriginalFileUntouched(string migration)
  {
    const string original = "- name: test\n  notRequiredGlobalKeys: boss\n";
    File.WriteAllText(FilePath, original);
    File.WriteAllText(Yaml.BackupDirectory, "not a directory");

    Assert.Throws<IOException>(() => Migrate(migration));
    Assert.That(File.ReadAllText(FilePath), Is.EqualTo(original));
  }
}

[TestFixture]
public class RevisionTests
{
  private string DirectoryPath = null!;
  private string ConfigPath => Path.Combine(DirectoryPath, "expand_world_data.cfg");

  [SetUp]
  public void SetUp()
  {
    DirectoryPath = Path.Combine(Path.GetTempPath(), "ExpandWorldData.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(DirectoryPath);
  }

  [TearDown]
  public void TearDown() => Directory.Delete(DirectoryPath, true);

  [Test]
  public void MissingConfigStartsAtLatestRevision()
    => Assert.That(Migrations.ReadRevision(ConfigPath), Is.EqualTo(Migrations.Latest));

  [TestCase("")]
  [TestCase("[3. Data]\nAutomatic data migration = true\n")]
  [TestCase("[3. Data]\n# Data revision = 1\n")]
  [TestCase("[Other]\nData revision = 1\n")]
  [TestCase("[3. Data]\nData revision = invalid\n")]
  public void ExistingConfigWithoutValidRevisionNeedsMigration(string config)
  {
    File.WriteAllText(ConfigPath, config);
    Assert.That(Migrations.ReadRevision(ConfigPath), Is.Zero);
  }

  [TestCase(0)]
  [TestCase(1)]
  [TestCase(42)]
  public void SavedRevisionIsPreserved(int revision)
  {
    File.WriteAllText(ConfigPath, $"# Config\n[3. Data]\n  Data revision = {revision}  \n");
    Assert.That(Migrations.ReadRevision(ConfigPath), Is.EqualTo(revision));
  }
}

[TestFixture]
[NonParallelizable]
public class YamlMigrationTests
{
  private string DirectoryPath = null!;
  private string OriginalDirectory = null!;
  private string OriginalBackupDirectory = null!;
  private ManualLogSource Logger = null!;
  private string FilePath => Path.Combine(DirectoryPath, "expand_events.yaml");

  [SetUp]
  public void SetUp()
  {
    DirectoryPath = Path.Combine(Path.GetTempPath(), "ExpandWorldData.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(DirectoryPath);
    OriginalDirectory = Yaml.BaseDirectory;
    OriginalBackupDirectory = Yaml.BackupDirectory;
    Yaml.BaseDirectory = DirectoryPath;
    Yaml.BackupDirectory = Path.Combine(DirectoryPath, "backups");
    Logger = new ManualLogSource("YamlMigrationTests");
    Log.Init(Logger);
  }

  [TearDown]
  public void TearDown()
  {
    Yaml.BaseDirectory = OriginalDirectory;
    Yaml.BackupDirectory = OriginalBackupDirectory;
    Logger.Dispose();
    Directory.Delete(DirectoryPath, true);
  }

  private List<Event.Data> ReadEvents()
    => Yaml.Deserializer().Deserialize<List<Event.Data>>(File.ReadAllText(FilePath));

  [Test]
  public void LegacyEventFieldsMigrateBeforeStrictDeserialization()
  {
    File.WriteAllText(FilePath, "- name: raid\n  notRequiredGlobalKeys: defeated_gdking\n  notRequiredPlayerKeys: visited\n  notRequiredKnownItems: SwordIron\n");
    Assert.That(EventMigration.Migrate("expand_events*.yaml"), Is.True);
    var entry = ReadEvents().Single();
    Assert.Multiple(() =>
    {
      Assert.That(entry.forbiddenGlobalKeys, Is.Not.Null);
      Assert.That(entry.forbiddenPlayerKeys, Is.Not.Null);
      Assert.That(entry.forbiddenKnownItems, Is.EqualTo("SwordIron"));
      Assert.That(File.ReadAllText(FilePath), Does.Not.Contain("notRequired"));
    });
  }

  [TestCase("requiredGlobalKeys: KilledTroll, defeated_gdking")]
  [TestCase("requiredPlayerKeysAll: KilledTroll, defeated_gdking")]
  public void LegacyAndKeysBecomeUnquotedBlockRows(string field)
  {
    File.WriteAllText(FilePath, $"- {field}\n  name: raid\n");
    Assert.That(EventMigration.Migrate("expand_events*.yaml"), Is.True);
    Assert.That(ReadEvents().Single().name, Is.EqualTo("raid"));
    var text = File.ReadAllText(FilePath).Replace("\r\n", "\n");
    Assert.That(text, Does.Contain("    - KilledTroll\n    - defeated_gdking"));
    Assert.That(text, Does.Not.Contain("\"").And.Not.Contain("["));
  }

  [Test]
  public void PlayerAnyKeysStayOneRowWhenMergedWithAllKeys()
  {
    File.WriteAllText(FilePath, "- name: raid\n  requiredPlayerKeys: first, second\n  requiredPlayerKeysAll: third, fourth\n");
    EventMigration.Migrate("expand_events*.yaml");
    Assert.That(ReadEvents().Single().requiredPlayerKeys, Is.Not.Null);
    var text = File.ReadAllText(FilePath);
    Assert.That(text, Does.Contain("- first, second").And.Contain("- third").And.Contain("- fourth"));
    Assert.That(text, Does.Not.Contain("requiredPlayerKeysAll"));
  }

  [TestCase("- name: raid\n  requiredGlobalKeys:\n    - KilledTroll\n    - defeated_gdking\n")]
  [TestCase("- name: raid\n  requiredGlobalKeys: [KilledTroll, defeated_gdking]\n")]
  [TestCase("- name: raid\n  requiredPlayerKeys:\n    - first, second\n    - third\n")]
  public void ModernListsAreUnchangedOnRetry(string yaml)
  {
    File.WriteAllText(FilePath, yaml);
    Assert.That(EventMigration.Migrate("expand_events*.yaml"), Is.False);
    Assert.That(File.ReadAllText(FilePath), Is.EqualTo(yaml));
    Assert.That(ReadEvents(), Has.Count.EqualTo(1));
  }

  [Test]
  public void MigratedFileIsUnchangedOnSecondPass()
  {
    File.WriteAllText(FilePath, "# Keep this comment\n- name: raid\n  requiredGlobalKeys: first, second\n  requiredPlayerKeys: third, fourth\n  requiredPlayerKeysAll: fifth\n  notRequiredGlobalKeys: sixth\n- name: other\n  duration: 120\n");
    Assert.That(EventMigration.Migrate("expand_events*.yaml"), Is.True);
    var migrated = File.ReadAllText(FilePath);
    Assert.That(EventMigration.Migrate("expand_events*.yaml"), Is.False);
    Assert.That(File.ReadAllText(FilePath), Is.EqualTo(migrated));
    Assert.That(migrated, Does.StartWith("# Keep this comment"));
    Assert.That(ReadEvents().Select(entry => entry.name), Is.EqualTo(new[] { "raid", "other" }));
  }

  [Test]
  public void RemovingEmptyFirstFieldKeepsTheEventMarker()
  {
    File.WriteAllText(FilePath, "- requiredPlayerKeysAll: ''\n  name: raid\n");
    EventMigration.Migrate("expand_events*.yaml");
    Assert.That(ReadEvents().Single().name, Is.EqualTo("raid"));
    Assert.That(File.ReadAllText(FilePath), Does.StartWith("- name: raid"));
  }

  [TestCase("true")]
  [TestCase("a: b")]
  public void ScalarEscapingPreservesKeyText(string key)
  {
    File.WriteAllText(FilePath, $"- name: raid\n  requiredGlobalKeys: '{key}, other'\n");
    EventMigration.Migrate("expand_events*.yaml");
    var entries = Yaml.Deserializer().Deserialize<List<Dictionary<string, object>>>(File.ReadAllText(FilePath));
    var rows = (IEnumerable<object>)entries.Single()["requiredGlobalKeys"];
    Assert.That(rows.First(), Is.TypeOf<string>().And.EqualTo(key));
    Assert.That(ReadEvents(), Has.Count.EqualTo(1));
  }

  [TestCase("true")]
  [TestCase("- red\n- green")]
  public void BackfillWriterPreservesExistingValuesAndIsRepeatable(string value)
  {
    File.WriteAllText(FilePath, "# Keep\n- name: missing\n  other: 5\n- name: custom\n  added: custom\n- name: unrelated\n");
    var values = new Dictionary<string, string> { ["missing"] = value, ["custom"] = value };
    Assert.That(Yaml.InsertMissingField("expand_events*.yaml", "name", "added", values), Is.True);
    var migrated = File.ReadAllText(FilePath);
    var entries = Yaml.Deserializer().Deserialize<List<Dictionary<string, object>>>(migrated);
    Assert.Multiple(() =>
    {
      Assert.That(entries[0].ContainsKey("added"), Is.True);
      Assert.That(entries[1]["added"], Is.EqualTo("custom"));
      Assert.That(entries[2].ContainsKey("added"), Is.False);
      Assert.That(migrated, Does.StartWith("# Keep"));
      Assert.That(migrated, Does.Not.Contain("["));
    });
    if (value.StartsWith("- "))
      Assert.That((IEnumerable<object>)entries[0]["added"], Is.EqualTo(new[] { "red", "green" }));
    else
      Assert.That(entries[0]["added"].ToString(), Is.EqualTo("true"));
    Assert.That(Yaml.InsertMissingField("expand_events*.yaml", "name", "added", values), Is.False);
    Assert.That(File.ReadAllText(FilePath), Is.EqualTo(migrated));
  }

  [Test]
  public void RemovingObsoleteAltBiomeFieldsKeepsNestedDataAndEntriesValid()
  {
    File.WriteAllText(FilePath, "- addVegetation:\n    - prefab: obsolete\n  name: first\n  addLocations: true\n  biome: Meadows\n- name: second\n  biome: BlackForest\n");
    Assert.That(Yaml.RemoveFields("expand_events*.yaml", new[] { "addVegetation", "addLocations" }), Is.True);
    var migrated = File.ReadAllText(FilePath);
    var entries = Yaml.Deserializer().Deserialize<List<Dictionary<string, object>>>(migrated);
    Assert.That(entries.Select(entry => entry["name"]), Is.EqualTo(new[] { "first", "second" }));
    Assert.That(migrated, Does.Not.Contain("obsolete").And.Not.Contain("addVegetation").And.Not.Contain("addLocations"));
    Assert.That(Yaml.RemoveFields("expand_events*.yaml", new[] { "addVegetation", "addLocations" }), Is.False);
  }
}