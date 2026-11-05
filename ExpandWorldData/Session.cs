using System;
using Common;
using ExpandWorldData.Biome;
using ExpandWorldData.Data;
using ExpandWorldData.Dungeon;
using ExpandWorldData.Event;
using Service;

namespace ExpandWorldData;

internal static class Session
{
  internal const int ModeProtocol = 1;
  internal const string ModeRpc = "expand_world_data ServerMode";
  private static readonly SessionState State = new();
  private static bool contentAvailable, roomsAvailable, contentInitialized, roomsInitialized;
  private static bool loadingLocal;
  private static bool savingSuspended, saveOnConfigSet;
  internal static int Generation => State.Generation;
  internal static bool Active => State.Active;
  internal static bool LocalFiles => State.LocalFiles && !loadingLocal;
  internal static bool Receiving => State.Mode == SessionMode.Full || State.Mode == SessionMode.Local;
  internal static bool UseNames => State.Mode == SessionMode.Menu || Active;
  internal static bool Failed => State.Mode == SessionMode.Failed;
  internal static bool IsCurrent(ZNet net, ZRpc rpc, int generation) => State.Current(net, rpc, generation) && ReferenceEquals(net, ZNet.instance);
  internal static bool IsServer(ZRpc rpc) => State.Network is ZNet net && IsCurrent(net, rpc, Generation);
  internal static bool AcceptsConfig(ZRpc rpc) => State.Mode == SessionMode.Full && IsServer(rpc);
  internal static bool Passive(ZRpc? rpc = null)
  {
    rpc ??= State.Server as ZRpc;
    return rpc != null && State.Mode == SessionMode.Passive && IsServer(rpc) && Patcher.GameplayInactive;
  }

  internal static void Begin(ZNet net)
  {
    if (!State.Begin(net)) return;
    saveOnConfigSet = EWD.Instance.Config.SaveOnConfigSet;
    savingSuspended = true;
    EWD.Instance.Config.SaveOnConfigSet = false;
    Yaml.SuspendWatchers();
    BiomeManager.ResetSession();
    contentAvailable = roomsAvailable = contentInitialized = roomsInitialized = false;
    if (!Patcher.SetGameplay(false)) throw new InvalidOperationException("EWD gameplay patches could not be suspended.");
  }

  internal static void Local(ZNet net)
  {
    if (!ReferenceEquals(State.Network, net) || !net.IsServer() || State.Mode == SessionMode.Local) return;
    loadingLocal = true;
    var save = EWD.Instance.Config.SaveOnConfigSet;
    var processing = ServerSync.ConfigSync.ProcessingServerUpdate;
    try
    {
      EWD.Instance.Config.SaveOnConfigSet = false;
      ServerSync.ConfigSync.ProcessingServerUpdate = true;
      EWD.Instance.Config.Reload();
    }
    finally
    {
      EWD.Instance.Config.SaveOnConfigSet = save;
      ServerSync.ConfigSync.ProcessingServerUpdate = processing;
      loadingLocal = false;
    }
    State.Local();
    ResumeSaving();
    BiomeManager.NamesFromFile();
    Patcher.SetGameplay(true);
    Yaml.ResumeWatchers();
    Initialize();
  }

  internal static void Bind(ZNet net, ZNetPeer peer)
  {
    if (net.IsServer() || !peer.m_server) return;
    State.Bind(net, peer.m_rpc);
    var generation = Generation;
    peer.m_rpc.Register<ZPackage>(ModeRpc, (rpc, package) =>
    {
      if (!IsCurrent(net, rpc, generation)) return;
      try
      {
        if (package.Size() - package.GetPos() != 5 || package.ReadInt() != ModeProtocol || !package.ReadBool()) return;
        if (!Patcher.GameplayInactive) throw new InvalidOperationException("Local EWD features are still active.");
        var alreadyPassive = State.Mode == SessionMode.Passive;
        if (State.Passive(net, rpc, generation) && !alreadyPassive)
          Log.Info("Server-only session: local EWD features suspended.");
      }
      catch (Exception error) { Reject(error); }
    });
  }

  internal static void Full(ZRpc rpc)
  {
    if (State.Network is not ZNet net) return;
    try { State.Full(net, rpc, Generation); }
    catch (Exception error) { Reject(error); }
  }

  internal static void Synchronized(ZRpc rpc)
  {
    if (State.Network is not ZNet net || !State.CompleteSync(net, rpc, Generation)) return;
    Patcher.SetGameplay(true);
    Initialize();
    if (RandEventSystem.instance) EventManager.ApplyTiming(RandEventSystem.instance);
    Refresh.Apply(Regen.Patches);
  }

  internal static void ContentStarted() { contentAvailable = true; Initialize(); }
  internal static void RoomsStarted() { roomsAvailable = true; Initialize(); }
  private static void Initialize()
  {
    if (!Active) return;
    if (roomsAvailable && !roomsInitialized)
    {
      InitializeRooms.Initialize();
      roomsInitialized = true;
    }
    if (contentAvailable && !contentInitialized)
    {
      InitializeContent.Initialize();
      contentInitialized = true;
    }
  }

  internal static void Quiesce(ZNet net)
  {
    if (!State.Close(net)) return;
    Yaml.SuspendWatchers();
    // Saving inside native Shutdown still needs the room and location patches.
  }

  internal static void End(ZNet net, bool shutdownCompleted = true)
  {
    if (!ReferenceEquals(State.Network, net)) return;
    Quiesce(net);
    Exception? failure = null;
    void Restore(Action action) { try { action(); } catch (Exception error) { failure ??= error; Log.Error(error.ToString()); } }
    Restore(ServerSync.ConfigSync.RestoreSession);
    Restore(() =>
    {
      var save = EWD.Instance.Config.SaveOnConfigSet;
      var processing = ServerSync.ConfigSync.ProcessingServerUpdate;
      EWD.Instance.Config.SaveOnConfigSet = false;
      ServerSync.ConfigSync.ProcessingServerUpdate = true;
      try { EWD.Instance.Config.Reload(); }
      finally
      {
        EWD.Instance.Config.SaveOnConfigSet = save;
        ServerSync.ConfigSync.ProcessingServerUpdate = processing;
      }
    });
    Restore(() => { if (!Patcher.SetGameplay(false)) throw new InvalidOperationException("EWD gameplay patches are still active."); });
    Restore(SessionResources.Restore);
    Restore(CleanupOnShutdown.CleanUp);
    ResumeSaving();
    contentAvailable = roomsAvailable = contentInitialized = roomsInitialized = false;
    State.End(failure == null && shutdownCompleted);
    if (failure == null && shutdownCompleted)
    {
      Restore(() =>
      {
        Yaml.ResumeWatchers();
        BiomeManager.NamesFromFile();
      });
      if (failure != null)
      {
        State.End(false);
        Restore(Yaml.SuspendWatchers);
      }
    }
    if (State.Mode == SessionMode.Failed)
      Log.Warning("EWD could not finish resetting. Restart Valheim before entering another world.");
  }

  private static void ResumeSaving()
  {
    if (!savingSuspended) return;
    EWD.Instance.Config.SaveOnConfigSet = saveOnConfigSet;
    savingSuspended = false;
  }

  internal static void Reject(Exception error)
  {
    Log.Warning("EWD connection failed: " + error.Message);
    if (Game.instance) Game.instance.Logout();
  }
}
