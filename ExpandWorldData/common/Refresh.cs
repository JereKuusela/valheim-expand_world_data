// Shared code: keep identical in EWD and EWS (common/). Sync changes to both.
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Service;
using UnityEngine;
using UnityEngine.Rendering;

namespace Common;

// Values are in execution order.
[Flags]
public enum Regen
{
  None = 0,
  Seed = 1,
  // Cached values and dynamic patches.
  Patches = 2,
  ZoneGrid = 4,
  // Biome lookup caches and world generator data.
  Biomes = 8,
  // Heightmaps (also resets clutter).
  Terrain = 16,
  Clutter = 32,
  Water = 64,
  Minimap = 128,
  World = Biomes | Terrain | Clutter | Water | Minimap,
}

// Coalesces refresh requests so multiple changes are applied together after a delay.
// Nothing runs without a world. Mods can share one engine with Forward.
public static class Refresh
{
  private const float Delay = 1f;
  private const Regen All = Regen.World | Regen.Seed | Regen.Patches | Regen.ZoneGrid;
  private const Regen WorldFlags = Regen.World | Regen.Seed;
  private static readonly List<(Regen Flag, Action Hook)> Hooks = [];
  private static Regen Pending = Regen.None;
  private static float Timer = -1f;
  private static bool Executing = false;
  private static Host? Remote;

  // Hooks of the same flag run in registration order, before the built-in step.
  public static void Register(Regen flag, Action hook)
  {
    if (Remote != null) Remote.Register(flag, hook);
    else Hooks.Add((flag, hook));
  }

  public static void Request(Regen target)
  {
    if (Remote != null)
    {
      Remote.Request(target);
      return;
    }
    // All changes affect patches.
    Pending |= Regen.Patches | target;
    // Debounced for smooth config editing.
    Timer = Delay;
  }

  public static bool IsPending(Regen flag) => Remote?.IsPending(flag) ?? Pending.HasFlag(flag);

  // Runs the flags now even if they are not pending.
  public static void Run(Regen flags)
  {
    if (Remote != null)
    {
      Remote.Run(flags);
      return;
    }
    Pending |= flags;
    Flush(flags);
  }

  public static void Run()
  {
    if (Remote != null)
    {
      Remote.Run();
      return;
    }
    Flush(All);
  }

  // Drops everything pending, for cases where the new state is already applied.
  public static void Clear()
  {
    if (Remote != null)
    {
      Remote.Clear();
      return;
    }
    Pending = Regen.None;
    Timer = -1f;
  }

  public static void Tick(float deltaTime)
  {
    if (Remote != null || Timer < 0f) return;
    Timer -= deltaTime;
    if (Timer > 0f) return;
    Timer = -1f;
    Flush(All, true);
  }

  // Hands over everything to the engine of another mod, so only one pass is done for all mods.
  public static void Forward(Assembly assembly)
  {
    if (Remote != null) return;
    var host = Host.Resolve(assembly);
    if (host == null)
    {
      Log.Warning("Unable to share the refresh with another mod. Using a separate one.");
      return;
    }
    Remote = host;
    foreach (var (flag, hook) in Hooks)
      host.Register(flag, hook);
    Hooks.Clear();
    var pending = Pending;
    Pending = Regen.None;
    Timer = -1f;
    if (pending != Regen.None) host.Request(pending);
  }

  // A new generator already uses the current values, so earlier world requests are obsolete.
  internal static void OnWorldInitialize()
  {
    if (Remote != null || Executing) return;
    Pending &= ~WorldFlags;
    if (Pending == Regen.None) Timer = -1f;
  }

  // Only requested refreshes are logged, not the ones done on world load.
  private static void Flush(Regen mask, bool log = false)
  {
    if (Executing) return;
    var worldReady = ZNet.instance && WorldGenerator.instance?.m_world?.m_menu == false;
    if (log && ZNet.instance)
    {
      var applied = Pending & mask & ~Regen.Patches;
      if (!worldReady) applied &= ~WorldFlags;
      if (applied != Regen.None) Log.Info($"Refreshing {applied}.");
    }
    Executing = true;
    try
    {
      for (var bit = (int)Regen.Seed; bit <= (int)Regen.Minimap; bit <<= 1)
      {
        var flag = (Regen)bit;
        // Pending is read again because hooks can request more.
        if (!mask.HasFlag(flag) || !Pending.HasFlag(flag)) continue;
        Pending &= ~flag;
        if (!ZNet.instance) continue;
        if (WorldFlags.HasFlag(flag) && !worldReady) continue;
        Step(flag);
      }
      // Without a world nothing is applied. Patches are applied when the world loads.
      if (!ZNet.instance) Pending &= ~mask;
    }
    finally
    {
      Executing = false;
    }
    if (Pending == Regen.None) Timer = -1f;
  }

  private static void Step(Regen flag)
  {
    foreach (var (hookFlag, hook) in Hooks)
    {
      if (hookFlag != flag) continue;
      try { hook(); }
      catch (Exception e)
      {
        Log.Error($"Refresh {flag}: {e.Message}");
        Log.Error(e.StackTrace);
      }
    }
    try
    {
      switch (flag)
      {
        case Regen.Biomes: RefreshBiomes(); break;
        case Regen.Terrain: RefreshTerrain(); break;
        case Regen.Clutter: ClutterSystem.instance?.ClearAll(); break;
        case Regen.Minimap: RefreshMinimap(); break;
      }
    }
    catch (Exception e)
    {
      Log.Error($"Refresh {flag}: {e.Message}");
      Log.Error(e.StackTrace);
    }
  }

  private static void RefreshBiomes()
  {
    var generator = WorldGenerator.instance;
    if (generator == null) return;
    WorldGenerator.s_cachedBiomeAreas.Clear();
    WorldGenerator.s_cachedBiomes.Clear();
    foreach (var altBiome in AltBiomeList.m_altBiomes)
      altBiome.Sectors.Clear();
    generator.Pregenerate();
    AltBiomeWorldData.VerifyBiomeData(generator.m_world);
  }

  private static void RefreshTerrain()
  {
    foreach (var heightmap in UnityEngine.Object.FindObjectsByType<Heightmap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
    {
      heightmap.m_buildData = null;
      heightmap.Regenerate();
    }
    ClutterSystem.instance?.ClearAll();
    // Already cleared.
    Pending &= ~Regen.Clutter;
  }

  private static void RefreshMinimap()
  {
    if (Minimap.instance == null) return;
    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
    Minimap.instance.GenerateWorldMap();
  }

  // Accesses the engine of another assembly. Flags are converted because the enum types differ.
  private sealed class Host
  {
    private Type RegenType = null!;
    private MethodInfo RequestMethod = null!;
    private MethodInfo RegisterMethod = null!;
    private MethodInfo IsPendingMethod = null!;
    private MethodInfo RunFlagsMethod = null!;
    private MethodInfo RunMethod = null!;
    private MethodInfo ClearMethod = null!;

    public static Host? Resolve(Assembly assembly)
    {
      var type = assembly.GetType("Common.Refresh");
      var regen = assembly.GetType("Common.Regen");
      if (type == null || regen == null) return null;
      var host = new Host { RegenType = regen };
      var request = type.GetMethod("Request", [regen]);
      var register = type.GetMethod("Register", [regen, typeof(Action)]);
      var isPending = type.GetMethod("IsPending", [regen]);
      var runFlags = type.GetMethod("Run", [regen]);
      var run = type.GetMethod("Run", Type.EmptyTypes);
      var clear = type.GetMethod("Clear", Type.EmptyTypes);
      if (request == null || register == null || isPending == null || runFlags == null || run == null || clear == null) return null;
      host.RequestMethod = request;
      host.RegisterMethod = register;
      host.IsPendingMethod = isPending;
      host.RunFlagsMethod = runFlags;
      host.RunMethod = run;
      host.ClearMethod = clear;
      return host;
    }

    private object Convert(Regen flags) => Enum.ToObject(RegenType, (int)flags);
    public void Request(Regen flags) => RequestMethod.Invoke(null, [Convert(flags)]);
    public void Register(Regen flag, Action hook) => RegisterMethod.Invoke(null, [Convert(flag), hook]);
    public bool IsPending(Regen flag) => (bool)IsPendingMethod.Invoke(null, [Convert(flag)]);
    public void Run(Regen flags) => RunFlagsMethod.Invoke(null, [Convert(flags)]);
    public void Run() => RunMethod.Invoke(null, null);
    public void Clear() => ClearMethod.Invoke(null, null);
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize))]
public class RefreshWorldInitialize
{
  static void Postfix() => Refresh.OnWorldInitialize();
}
