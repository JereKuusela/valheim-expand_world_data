using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData.Environment;

// The game merges key times of both gradients when blending environments, which can exceed Unity's limit of 8.
[HarmonyPatch(typeof(Utils.Gradient), nameof(Utils.Gradient.Lerp), [typeof(Gradient), typeof(Gradient), typeof(float), typeof(bool), typeof(bool)])]
public class LimitGradientKeys
{
  private const int MaxKeys = 8;

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions)
      .MatchStartForward(new CodeMatch(OpCodes.Newarr, typeof(GradientColorKey)))
      .Advance(-2);
    var load = matcher.Instruction;
    var copy = new CodeInstruction(load.opcode, load.operand) { labels = load.labels };
    load.labels = [];
    return matcher
      .Insert(copy, CodeInstruction.Call(typeof(LimitGradientKeys), nameof(Shrink)))
      .InstructionEnumeration();
  }

  // Repeatedly merges the two closest times into their average.
  static void Shrink(List<float> times)
  {
    if (times.Count <= MaxKeys) return;
    times.Sort();
    while (times.Count > MaxKeys)
    {
      var best = 0;
      var bestGap = float.MaxValue;
      for (var i = 0; i < times.Count - 1; i++)
      {
        var gap = times[i + 1] - times[i];
        if (gap >= bestGap) continue;
        bestGap = gap;
        best = i;
      }
      times[best] = (times[best] + times[best + 1]) / 2f;
      times.RemoveAt(best + 1);
    }
  }
}
