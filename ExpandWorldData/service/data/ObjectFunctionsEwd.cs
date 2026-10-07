namespace Data;

// EWD only: player information of the owner.
public partial class ObjectFunctions
{
  partial void GetHostFunction(string key, ref string? result) =>
    result = key switch
    {
      "pid" => GetPid(),
      "pname" => GetPname(),
      "pchar" => GetPchar(),
      _ => null,
    };

  private string GetPid()
  {
    var peer = GetPeer();
    if (peer != null)
      return peer.m_rpc.GetSocket().GetHostName();
    return Player.m_localPlayer ? "Server" : "";
  }
  private string GetPname()
  {
    var peer = GetPeer();
    if (peer != null)
      return peer.m_playerName;
    return Player.m_localPlayer ? Player.m_localPlayer.GetPlayerName() : "";
  }
  private string GetPchar()
  {
    var peer = GetPeer();
    if (peer != null)
      return peer.m_characterID.ToString();
    return Player.m_localPlayer ? Player.m_localPlayer.GetPlayerID().ToString() : "";
  }
  private ZNetPeer? GetPeer() => zdo.GetOwner() != 0 ? ZNet.instance.GetPeer(zdo.GetOwner()) : null;
}
