using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using NUnit.Framework;
using Service;

namespace ExpandWorldData.Tests;

[TestFixture, NonParallelizable]
public class DataDirectoryReadTests
{
  private string root = null!;
  private ManualLogSource logger = null!;
  private readonly List<string> warnings = [];

  [SetUp]
  public void SetUp()
  {
    root = Path.Combine(Path.GetTempPath(), "EwdDataDirectoryTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    warnings.Clear();
    logger = new ManualLogSource("DataDirectoryReadTests");
    logger.LogEvent += (_, args) =>
    {
      if (args.Level == LogLevel.Warning) warnings.Add(args.Data.ToString());
    };
    Log.Init(logger);
  }

  [TearDown]
  public void TearDown()
  {
    logger.Dispose();
    Directory.Delete(root, true);
  }

  private static string[] Read(string path, string pattern = "*.yaml") =>
    (string[])typeof(global::Data.DataLoading).GetMethod("GetFilesOrEmpty", BindingFlags.NonPublic | BindingFlags.Static)!
      .Invoke(null, new object[] { path, pattern })!;

  [Test]
  public void MissingSourceDoesNotPreventReadingOtherSources()
  {
    var missing = Path.Combine(root, "missing");
    var nested = Path.Combine(root, "nested");
    Directory.CreateDirectory(nested);
    var yaml = Path.Combine(nested, "sample.yaml");
    File.WriteAllText(yaml, "[]");
    File.WriteAllText(Path.Combine(nested, "ignored.txt"), "ignored");
    Assert.That(Read(missing).Concat(Read(root)).ToArray(), Is.EqualTo(new[] { yaml }));
    Assert.That(warnings.Count, Is.EqualTo(1));
    Assert.That(warnings[0], Does.Contain(missing).And.Contain("DirectoryNotFoundException"));
    Assert.That(Directory.Exists(missing), Is.False);
  }

  [Test]
  public void AllFailedSourcesProduceNoFilesAndOneWarningEach()
  {
    var files = Enumerable.Range(0, 3).SelectMany(i => Read(Path.Combine(root, "missing" + i))).ToArray();
    Assert.That(files, Is.Empty);
    Assert.That(warnings.Count, Is.EqualTo(3));
  }

  [Test]
  public void FileInsteadOfDirectoryAlsoProducesWarningAndNoFiles()
  {
    var path = Path.Combine(root, "data");
    File.WriteAllText(path, "unchanged");
    Assert.That(Read(path), Is.Empty);
    Assert.That(warnings.Count, Is.EqualTo(1));
    Assert.That(warnings[0], Does.Contain(path));
    Assert.That(File.ReadAllText(path), Is.EqualTo("unchanged"));
  }

  [Test]
  public void EmptyReadableDirectoryProducesNoWarning()
  {
    Assert.That(Read(root), Is.Empty);
    Assert.That(warnings, Is.Empty);
  }

  private static void Ensure(string path) =>
    typeof(global::Data.DataLoading).GetMethod("EnsureDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
      .Invoke(null, new object[] { path });

  [Test]
  public void PreparingMissingDirectoryPreventsReadWarning()
  {
    var path = Path.Combine(root, "new", "data");
    Ensure(path);
    Ensure(path);
    Assert.That(Directory.Exists(path), Is.True);
    Assert.That(Read(path), Is.Empty);
    Assert.That(warnings, Is.Empty);
  }

  [Test]
  public void FailedCreationDoesNotBlockOtherDirectoriesOrDuplicateReadWarning()
  {
    var blocked = Path.Combine(root, "blocked");
    File.WriteAllText(blocked, "unchanged");
    var valid = Path.Combine(root, "valid");
    Ensure(blocked);
    Ensure(valid);
    Assert.That(Directory.Exists(valid), Is.True);
    Assert.That(Read(blocked).Concat(Read(valid)), Is.Empty);
    Assert.That(warnings.Count, Is.EqualTo(1));
    Assert.That(File.ReadAllText(blocked), Is.EqualTo("unchanged"));
  }

  private static List<string> Sources(string game, string profile, string baseDirectory) =>
    (List<string>)typeof(global::Data.DataLoading).GetMethod("GetSourceFiles", BindingFlags.NonPublic | BindingFlags.Static)!
      .Invoke(null, new object[] { game, profile, baseDirectory, "expand_data*.yaml" })!;

  [Test]
  public void IdenticalMissingGameAndProfilePathsOnlyWarnOnce()
  {
    var missing = Path.Combine(root, "missing");
    Assert.That(Sources(missing, missing, root), Is.Empty);
    Assert.That(warnings.Count, Is.EqualTo(1));
    Assert.That(warnings[0], Does.Contain(missing));
  }

  [Test]
  public void SourceDiscoveryPreservesOrderPatternsAndFileDeduplication()
  {
    var game = Path.Combine(root, "game");
    var profile = Path.Combine(root, "profile");
    Directory.CreateDirectory(game);
    Directory.CreateDirectory(profile);
    var first = Path.Combine(game, "expand_data_game.yaml");
    var second = Path.Combine(profile, "profile.yaml");
    File.WriteAllText(first, "[]");
    File.WriteAllText(second, "[]");
    // The base source overlaps the game source and must not duplicate its file.
    Assert.That(Sources(game, profile, game), Is.EqualTo(new[] { first, second }));
    Assert.That(Sources(game, game, profile), Is.EqualTo(new[] { first }));
    Assert.That(warnings, Is.Empty);
  }

  [Test]
  public void FailedSourceDiscoveryReturnsEmptyForAllThreeSources()
  {
    Assert.That(Sources(Path.Combine(root, "game"), Path.Combine(root, "profile"), Path.Combine(root, "base")), Is.Empty);
    Assert.That(warnings.Count, Is.EqualTo(3));
  }

  private static void Watch(string path, Action setup) =>
    typeof(global::Data.DataLoading).GetMethod("TryWatch", BindingFlags.NonPublic | BindingFlags.Static)!
      .Invoke(null, new object[] { path, setup });

  [Test]
  public void FailedWatcherConstructionDoesNotBlockNextSetup()
  {
    var missing = Path.Combine(root, "missing");
    Watch(missing, () =>
    {
      using var watcher = new FileSystemWatcher(missing, "*");
      watcher.EnableRaisingEvents = true;
    });
    var nextSetupReached = false;
    Watch(root, () => nextSetupReached = true);
    Assert.That(nextSetupReached, Is.True);
    Assert.That(warnings.Count, Is.EqualTo(1));
    Assert.That(warnings[0], Does.Contain("Unable to watch data directory").And.Contain(missing));
  }
}
