using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ExpandWorldData.Tests;

[TestFixture]
public class SessionTests
{
  [Test]
  public void ServerOnlyVisitDoesNotPreventTheNextLocalWorld()
  {
    var state = new SessionState();
    var network = new object();
    var server = new object();
    state.Begin(network);
    state.Bind(network, server);
    Assert.That(state.Active, Is.False);
    Assert.That(state.Passive(network, server, state.Generation), Is.True);
    Assert.That(state.LocalFiles, Is.False);
    state.Close(network);
    state.End(true);
    state.Begin(new object());
    state.Local();
    Assert.That(state.Active, Is.True);
    Assert.That(state.LocalFiles, Is.True);
  }

  [Test]
  public void FullServerDoesNotActivateBeforeInitialSettingsArrive()
  {
    var state = new SessionState();
    var network = new object();
    var server = new object();
    state.Begin(network);
    state.Bind(network, server);
    Assert.That(state.CompleteSync(network, server, state.Generation), Is.False);
    Assert.That(state.Full(network, server, state.Generation), Is.True);
    Assert.That(state.Active, Is.False);
    state.CompleteSync(network, server, state.Generation);
    Assert.That(state.Active, Is.True);
    Assert.That(state.LocalFiles, Is.False);
  }

  [Test]
  public void ForeignAndPreviousConnectionMessagesCannotAuthorizeAVisit()
  {
    var state = new SessionState();
    var network = new object();
    var server = new object();
    state.Begin(network);
    state.Bind(network, server);
    var generation = state.Generation;
    Assert.That(state.Passive(network, new object(), generation), Is.False);
    state.Close(network);
    state.End(true);
    state.Begin(network);
    state.Bind(network, server);
    Assert.That(state.Passive(network, server, generation), Is.False);
    Assert.That(state.Mode, Is.EqualTo(SessionMode.Pending));
  }

  [Test]
  public void DuplicateConfirmationIsHarmlessAndContradictoryModesAreRejected()
  {
    var state = new SessionState();
    var network = new object();
    var server = new object();
    state.Begin(network);
    state.Bind(network, server);
    state.Passive(network, server, state.Generation);
    Assert.That(state.Passive(network, server, state.Generation), Is.True);
    Assert.Throws<InvalidOperationException>(() => state.Full(network, server, state.Generation));
  }

  [Test]
  public void CancellationInvalidatesCallbacksBeforeRestoration()
  {
    var state = new SessionState();
    var network = new object();
    var server = new object();
    state.Begin(network);
    state.Bind(network, server);
    var generation = state.Generation;
    Assert.That(state.Close(network), Is.True);
    Assert.That(state.Close(network), Is.False);
    Assert.That(state.Current(network, server, generation), Is.False);
    Assert.Throws<InvalidOperationException>(() => state.Begin(new object()));
    state.End(true);
    Assert.That(state.LocalFiles, Is.True);
  }

  [Test]
  public void FailedRestorationCannotBeMistakenForAReadyMenu()
  {
    var state = new SessionState();
    var network = new object();
    state.Begin(network);
    state.Close(network);
    state.End(false);
    Assert.That(state.Mode, Is.EqualTo(SessionMode.Failed));
    Assert.That(state.LocalFiles, Is.False);
    Assert.Throws<InvalidOperationException>(() => state.Begin(new object()));
  }
}

[TestFixture]
[NonParallelizable]
public class SessionResourceTests
{
  [TearDown]
  public void Restore() => SessionResources.Restore();

  [Test]
  public void SharedSpawnAssetRestoresWithoutALiveSpawnSystem()
  {
    var asset = new object();
    var original = new List<string> { "vanilla" };
    var current = original;
    var custom = new List<string> { "custom" };
    SessionResources.Set(asset, "spawns", () => current, value => current = value, custom);
    Assert.That(current, Is.SameAs(custom));
    SessionResources.Restore();
    Assert.That(current, Is.SameAs(original));
  }

  [Test]
  public void AnotherModsReplacementIsPreserved()
  {
    var asset = new object();
    var current = "original";
    SessionResources.Set(asset, "value", () => current, value => current = value, "ewd");
    current = "other mod";
    SessionResources.Restore();
    Assert.That(current, Is.EqualTo("other mod"));
  }

  [Test]
  public void ListReloadKeepsExternalAdditionsThroughRestoration()
  {
    var list = new List<string> { "vanilla" };
    SessionResources.Replace(list, new[] { "ewd1" });
    list.Add("other mod");
    SessionResources.Replace(list, new[] { "ewd2" });
    SessionResources.Restore();
    Assert.That(list, Is.EqualTo(new[] { "vanilla", "other mod" }));
  }

  [Test]
  public void NativeListApplicationPreservesDuplicatesAndRestoresTheOriginal()
  {
    var list = new List<string> { "original", "original" };
    SessionResources.ReplaceWith(list, () => list.AddRange(new[] { "custom", "custom" }));
    Assert.That(list, Has.Count.EqualTo(2));
    SessionResources.Restore();
    Assert.That(list, Is.EqualTo(new[] { "original", "original" }));
  }

  [Test]
  public void FailedNativeInitializationStillRestoresItsPartialWrites()
  {
    var list = new List<string> { "original" };
    SessionResources.Replace(list, new[] { "old custom" });
    list.Add("other mod");
    Assert.Throws<InvalidOperationException>(() => SessionResources.ReplaceWith(list, () =>
    {
      list.Add("partial custom");
      throw new InvalidOperationException("Native initialization failed.");
    }));
    SessionResources.Restore();
    Assert.That(list, Is.EqualTo(new[] { "original", "other mod" }));
  }

  [Test]
  public void ExistingAndNewTranslationKeysRestoreIndependently()
  {
    var values = new Dictionary<string, string> { ["existing"] = "original" };
    SessionResources.Translate(values, "existing", "custom");
    SessionResources.Translate(values, "new", "custom");
    SessionResources.Restore();
    Assert.That(values["existing"], Is.EqualTo("original"));
    Assert.That(values.ContainsKey("new"), Is.False);
  }

  [Test]
  public void TranslationReloadDoesNotOverwriteAnotherModsEdit()
  {
    var values = new Dictionary<string, string> { ["name"] = "original" };
    SessionResources.Translate(values, "name", "ewd");
    values["name"] = "other mod";
    SessionResources.Translate(values, "name", "ewd2");
    SessionResources.Restore();
    Assert.That(values["name"], Is.EqualTo("other mod"));
  }
}
