using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Service;

namespace ExpandWorldData;


// Base game has a way to execute commands on the server, but there is no admin check on it.
[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
public class IdManager
{
  public static string RPC_RequestIds = "EW_RequestIds";
  public static string RPC_SyncLocationIds = "EW_SyncLocationIds";
  public static string RPC_SyncVegetationIds = "EW_SyncVegetationIds";
  private static bool IsAllowed(ZRpc rpc)
  {
    var zNet = ZNet.instance;
    if (!zNet.enabled)
      return false;
    return rpc != null && zNet.IsAdmin(rpc.GetSocket().GetHostName());
  }
  private static readonly List<ZRpc> RegisteredIdClients = [];
  private static void RPC_RegisterIdSyncing(ZRpc rpc)
  {
    if (!IsAllowed(rpc)) return;
    if (RegisteredIdClients.Contains(rpc)) return;
    RegisteredIdClients.Add(rpc);
    rpc.Invoke(RPC_SyncLocationIds, [LocationIds()]);
    rpc.Invoke(RPC_SyncVegetationIds, [VegetationIds()]);
  }
  public static void SendLocationIds()
  {
    var locationIds = LocationIds();
    foreach (var rpc in RegisteredIdClients)
    {
      if (rpc.IsConnected())
      {
        rpc.Invoke(RPC_SyncLocationIds, [locationIds]);
      }
      else
      {
        RegisteredIdClients.Remove(rpc);
      }
    }
  }
  private static string LocationIds() => string.Join("|", [.. ZoneSystem.instance.m_locations.Where(IsValid).Select(l => l.m_prefab.Name).Distinct()]);
  private static bool IsValid(ZoneSystem.ZoneLocation loc) => loc != null && loc.m_prefab != null && (loc.m_prefab.IsValid || loc.m_prefab.m_name != null);

  public static void SendVegetationIds()
  {
    var vegetationIds = VegetationIds();
    foreach (var rpc in RegisteredIdClients)
    {
      if (rpc.IsConnected())
      {
        rpc.Invoke(RPC_SyncVegetationIds, [vegetationIds]);
      }
      else
      {
        RegisteredIdClients.Remove(rpc);
      }
    }
  }
  private static string VegetationIds() => string.Join("|", [.. ZoneSystem.instance.m_vegetation.Where(IsValid).Select(v => v.m_name).Distinct()]);
  private static bool IsValid(ZoneSystem.ZoneVegetation veg) => veg != null && veg.m_prefab != null;

  static void Postfix(ZNet __instance, ZRpc rpc)
  {
    RegisteredIdClients.Clear();
    if (__instance.IsServer())
    {
      rpc.Register(RPC_RequestIds, new(RPC_RegisterIdSyncing));
    }
  }
}
