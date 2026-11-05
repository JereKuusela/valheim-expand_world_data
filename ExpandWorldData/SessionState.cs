using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ExpandWorldData.Tests")]

namespace ExpandWorldData;

internal enum SessionMode { Menu, Pending, Local, Full, Passive, Closing, Failed }

internal sealed class SessionState
{
  internal object? Network { get; private set; }
  internal object? Server { get; private set; }
  internal int Generation { get; private set; }
  internal SessionMode Mode { get; private set; }
  internal bool Synchronized { get; private set; }
  internal bool Active => Mode == SessionMode.Local || Mode == SessionMode.Full && Synchronized;
  internal bool LocalFiles => Mode == SessionMode.Menu || Mode == SessionMode.Local;

  internal bool Begin(object network)
  {
    if (ReferenceEquals(Network, network)) return false;
    if (Network != null || Mode == SessionMode.Failed)
      throw new InvalidOperationException("The previous EWD session has not finished closing. Restart Valheim.");
    Network = network;
    Server = null;
    Synchronized = false;
    Mode = SessionMode.Pending;
    ++Generation;
    return true;
  }

  internal void Bind(object network, object server)
  {
    if (!ReferenceEquals(Network, network) || Mode != SessionMode.Pending) return;
    if (Server != null && !ReferenceEquals(Server, server))
      throw new InvalidOperationException("EWD received a second server connection.");
    Server = server;
  }

  internal bool Current(object network, object server, int generation)
    => ReferenceEquals(Network, network) && ReferenceEquals(Server, server) && generation == Generation &&
      Mode != SessionMode.Closing && Mode != SessionMode.Failed && Mode != SessionMode.Menu;

  internal void Local()
  {
    if (Mode != SessionMode.Pending && Mode != SessionMode.Local) throw new InvalidOperationException("EWD session role changed.");
    Mode = SessionMode.Local;
  }

  internal bool Full(object network, object server, int generation)
  {
    if (!Current(network, server, generation)) return false;
    if (Mode == SessionMode.Passive) throw new InvalidOperationException("The server changed its EWD mode during connection.");
    if (Mode != SessionMode.Pending && Mode != SessionMode.Full) return false;
    Mode = SessionMode.Full;
    return true;
  }

  internal bool CompleteSync(object network, object server, int generation)
  {
    if (!Current(network, server, generation) || Mode != SessionMode.Full) return false;
    Synchronized = true;
    return true;
  }

  internal bool Passive(object network, object server, int generation)
  {
    if (!Current(network, server, generation)) return false;
    if (Mode == SessionMode.Full) throw new InvalidOperationException("The server changed its EWD mode during connection.");
    if (Mode != SessionMode.Pending && Mode != SessionMode.Passive) return false;
    Mode = SessionMode.Passive;
    return true;
  }

  internal bool Close(object network)
  {
    if (!ReferenceEquals(Network, network) || Mode == SessionMode.Closing) return false;
    Mode = SessionMode.Closing;
    ++Generation;
    return true;
  }

  internal void End(bool restored)
  {
    Network = Server = null;
    Synchronized = false;
    Mode = restored ? SessionMode.Menu : SessionMode.Failed;
  }
}
