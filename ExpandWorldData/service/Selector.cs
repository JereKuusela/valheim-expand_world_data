
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Service;

public static class Selector
{
  public static bool IsValid(ZNetView view) => view && IsValid(view.GetZDO());
  public static bool IsValid(ZDO zdo) => zdo != null && zdo.IsValid();
  public static ZDO? GetHovered()
  {
    var obj = Player.m_localPlayer;
    if (!obj) return null;
    var mask = LayerMask.GetMask(new string[]
    {
      "item",
      "piece",
      "piece_nonsolid",
      "Default",
      "static_solid",
      "Default_small",
      "character",
      "character_net",
      "terrain",
      "vehicle",
      "character_trigger" // Added to remove spawners with ESP mod.
    });
    var hits = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, mask);
    Array.Sort(hits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    foreach (var hit in hits)
    {
      if (Vector3.Distance(hit.point, obj.m_eye.position) >= 50f) continue;
      var netView = hit.collider.GetComponentInParent<ZNetView>();
      if (!IsValid(netView)) continue; ;
      if (hit.collider.GetComponent<EffectArea>()) continue;
      var player = netView.GetComponentInChildren<Player>();
      if (player) continue;
      return netView.GetZDO();
    }
    return null;
  }
}