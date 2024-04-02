using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData;

[HarmonyPatch(typeof(Minimap))]
public class MinimapIcon
{

  public static void Clear()
  {
    var mm = Minimap.instance;
    if (!mm) return;
    foreach (var pin in mm.m_locationPins)
      mm.RemovePin(pin.Value);
    mm.m_locationPins.Clear();
    IconSizes.Clear();
  }

  [HarmonyPatch(nameof(Minimap.GetLocationIcon)), HarmonyPostfix]
  static Sprite NewLocationIcons(Sprite result, string name)
  {
    if (result != null) return result;
    name = Parse.Split(name)[0];
    if (Enum.TryParse<Minimap.PinType>(name, true, out var icon))
      return Minimap.instance.GetSprite(icon);
    var hash = name.GetStableHashCode();
    if (ObjectDB.instance.m_itemByHash.TryGetValue(hash, out var item))
      return item.GetComponent<ItemDrop>()?.m_itemData?.GetIcon()!;
    var effect = ObjectDB.instance.GetStatusEffect(hash);
    if (effect) return effect.m_icon;
    return null!;
  }

  private static readonly Dictionary<Minimap.PinData, float> IconSizes = [];

  [HarmonyPatch(nameof(Minimap.UpdateLocationPins)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> IconSizeSetup(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      // Remove can be just wrapped.
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Minimap), nameof(Minimap.RemovePin), [typeof(Minimap.PinData)])))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(RemovePin).operand)
      // No good entry point for adding, so need to get the data manually.
      .MatchForward(false, new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Minimap.PinData), nameof(Minimap.PinData.m_doubleSize))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 5))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 7))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(AddPin).operand))
      .InstructionEnumeration();
  }

  private static void RemovePin(Minimap mm, Minimap.PinData pin)
  {
    IconSizes.Remove(pin);
    mm.RemovePin(pin);
  }
  private static void AddPin(KeyValuePair<Vector3, string> kvp, Minimap.PinData pin)
  {
    var split = Parse.Split(kvp.Value);
    if (split.Length < 2) return;
    var size = Parse.Float(split[1]);
    if (size <= 0) return;
    if (size >= 5)
      pin.m_worldSize = size;
    else
      IconSizes.Add(pin, size);
    pin.m_animate = split.Length > 2;
  }

  [HarmonyPatch(nameof(Minimap.Awake)), HarmonyPostfix]
  static void ClearSizes()
  {
    IconSizes.Clear();
  }

  [HarmonyPatch(nameof(Minimap.UpdatePins)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> ApplyIconSize(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
    // For some reason, targeting the size variable doesn't work, so hack the multiplier instead.
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 2f))
      .SetAndAdvance(OpCodes.Ldloc_S, 6)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(GetIconSize).operand))
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 2f))
      .SetAndAdvance(OpCodes.Ldloc_S, 6)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(GetIconSize).operand))
      .InstructionEnumeration();
  }
  private static float GetIconSize(Minimap.PinData pin) => IconSizes.TryGetValue(pin, out var iconSize) ? 2f * iconSize : 2f;
}