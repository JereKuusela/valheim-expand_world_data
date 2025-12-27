using HarmonyLib;
using UnityEngine;
namespace ExpandWorldData;

[HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
public class StartColorTransition
{
  public static void Postfix(Heightmap.Biome biome)
  {
    if (Configuration.CustomWaterColor)
      WaterColor.StartTransition(biome);
  }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
public class ResetColorTransition
{
  public static void Postfix()
  {
    WaterColor.StopTransition();
  }
}

public class WaterColor
{
  private static float TransitionProgress = 0f;
  private static bool Transitioning = false;
  public static Color CurrentSurfaceColor;
  public static Color CurrentTopColor;
  public static Color CurrentBottomColor;
  public static Color CurrentShallowColor;
  public static Color TargetSurfaceColor;
  public static Color TargetTopColor;
  public static Color TargetBottomColor;
  public static Color TargetShallowColor;

  public static void Transition(float time)
  {
    if (!Transitioning) return;
    TransitionProgress += 0.1f * time;
    if (TransitionProgress >= 1f)
      StopTransition();
    UpdateTransitions();
  }

  public static void StartTransition(Heightmap.Biome targetBiome)
  {
    // Bit hacky, but must trigger update to get initial colors.
    bool firstRun = !InitDone;
    if (firstRun) UpdateTransitions();
    var prevTopColor = TargetTopColor;
    var prevBottomColor = TargetBottomColor;
    var prevShallowColor = TargetShallowColor;
    var prevSurfaceColor = TargetSurfaceColor;
    TargetTopColor = targetBiome == Heightmap.Biome.AshLands ? AshlandsTop : WaterTop;
    TargetBottomColor = targetBiome == Heightmap.Biome.AshLands ? AshlandsBottom : WaterBottom;
    TargetShallowColor = targetBiome == Heightmap.Biome.AshLands ? AshlandsShallow : WaterShallow;
    TargetSurfaceColor = targetBiome == Heightmap.Biome.AshLands ? AshlandsSurface : WaterSurface;
    if (BiomeManager.TryGetData(targetBiome, out var data))
    {
      if (data.colorWaterTop.HasValue)
        TargetTopColor = data.colorWaterTop.Value;
      if (data.colorWaterBottom.HasValue)
        TargetBottomColor = data.colorWaterBottom.Value;
      if (data.colorWaterShallow.HasValue)
        TargetShallowColor = data.colorWaterShallow.Value;
      if (data.colorWaterSurface.HasValue)
        TargetSurfaceColor = data.colorWaterSurface.Value;
    }
    // Minor optimization to keep the current transition if the target doesn't change.
    if (prevBottomColor == TargetBottomColor &&
        prevTopColor == TargetTopColor &&
        prevShallowColor == TargetShallowColor &&
        prevSurfaceColor == TargetSurfaceColor)
    {
      return;
    }
    if (Transitioning)
    {
      // Only smooth way is to use the previous color as the starting point.
      CurrentTopColor = Color.Lerp(CurrentTopColor, prevTopColor, TransitionProgress);
      CurrentBottomColor = Color.Lerp(CurrentBottomColor, prevBottomColor, TransitionProgress);
      CurrentShallowColor = Color.Lerp(CurrentShallowColor, prevShallowColor, TransitionProgress);
      CurrentSurfaceColor = Color.Lerp(CurrentSurfaceColor, prevSurfaceColor, TransitionProgress);
    }
    TransitionProgress = firstRun ? 1f : 0f;
    Transitioning = true;
  }
  public static void StopTransition()
  {
    if (!Transitioning) return;
    Transitioning = false;
    TransitionProgress = 1f;
    CurrentTopColor = TargetTopColor;
    CurrentBottomColor = TargetBottomColor;
    CurrentShallowColor = TargetShallowColor;
    CurrentSurfaceColor = TargetSurfaceColor;
  }
  private static void UpdateTransitions()
  {
    foreach (var water in WaterVolume.Instances)
      UpdateTransition(water.m_waterSurface.sharedMaterial);
    var globalWater = EnvMan.instance?.transform.Find("WaterPlane").Find("watersurface");
    if (globalWater != null)
      UpdateTransition(globalWater.GetComponent<MeshRenderer>().sharedMaterial);
  }
  private static void UpdateTransition(Material mat)
  {
    InitColors(mat);
    var surfaceColor = Color.Lerp(CurrentSurfaceColor, TargetSurfaceColor, TransitionProgress);
    var topColor = Color.Lerp(CurrentTopColor, TargetTopColor, TransitionProgress);
    var bottomColor = Color.Lerp(CurrentBottomColor, TargetBottomColor, TransitionProgress);
    var shallowColor = Color.Lerp(CurrentShallowColor, TargetShallowColor, TransitionProgress);
    UpdateColors(mat, surfaceColor, topColor, bottomColor, shallowColor);
  }
  public static void FixColors(Material mat)
  {
    InitColors(mat);
    UpdateTransition(mat);
  }
  public static void Regenerate()
  {
    if (Player.m_localPlayer)
      StartTransition(Player.m_localPlayer.GetCurrentBiome());
    StopTransition();
    UpdateTransitions();
  }

  private static void UpdateColors(Material mat, Color surface, Color top, Color bottom, Color shallow)
  {
    mat.SetColor("_SurfaceColor", surface);
    mat.SetColor("_AshlandsSurfaceColor", surface);
    mat.SetColor("_ColorTop", top);
    mat.SetColor("_AshlandsColorTop", top);
    mat.SetColor("_ColorBottom", bottom);
    mat.SetColor("_AshlandsColorBottom", bottom);
    mat.SetColor("_ColorBottomShallow", shallow);
    mat.SetColor("_AshlandsColorBottomShallow", shallow);
  }
  private static bool InitDone = false;
  private static void InitColors(Material mat)
  {
    if (InitDone) return;
    InitDone = true;
    WaterSurface = mat.GetColor("_SurfaceColor");
    AshlandsSurface = mat.GetColor("_AshlandsSurfaceColor");
    WaterTop = mat.GetColor("_ColorTop");
    AshlandsTop = mat.GetColor("_AshlandsColorTop");
    WaterBottom = mat.GetColor("_ColorBottom");
    AshlandsBottom = mat.GetColor("_AshlandsColorBottom");
    WaterShallow = mat.GetColor("_ColorBottomShallow");
    AshlandsShallow = mat.GetColor("_AshlandsColorBottomShallow");
  }
  private static Color WaterSurface;
  private static Color AshlandsSurface;
  private static Color WaterTop;
  private static Color AshlandsTop;
  private static Color WaterBottom;
  private static Color AshlandsBottom;
  private static Color WaterShallow;
  private static Color AshlandsShallow;
}