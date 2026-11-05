using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpandWorldData;

// Only shared resources written by EWD are tracked; later edits by other mods are retained.
internal static class SessionResources
{
  private interface IChange { void Restore(); }
  private sealed class Value<T>(Func<T> read, Action<T> write, Func<bool> alive) : IChange
  {
    private readonly T original = read();
    private T last = default!;
    private bool applied, relinquished;
    internal void Set(T value)
    {
      if (relinquished) return;
      if (applied && (!alive() || !EqualityComparer<T>.Default.Equals(read(), last))) { relinquished = true; return; }
      write(value);
      last = value;
      applied = true;
    }
    public void Restore()
    {
      if (applied && !relinquished && alive() && EqualityComparer<T>.Default.Equals(read(), last)) write(original);
    }
  }
  private sealed class Contents<T>(List<T> list) : IChange
  {
    private readonly List<T> original = new(list);
    private List<T> last = new(list);
    internal void Set(IEnumerable<T> data)
    {
      var external = list.Where(item => !last.Contains(item)).ToList();
      var replacement = data.ToList();
      last = new(replacement);
      foreach (var item in external) if (!replacement.Contains(item)) replacement.Add(item);
      list.Clear();
      list.AddRange(replacement);
    }
    internal void SetNative(Action apply)
    {
      var external = list.Where(item => !last.Contains(item)).ToList();
      list.Clear();
      try { apply(); }
      finally
      {
        // A native initializer can fail after appending some of its entries.
        last = new(list);
        foreach (var item in external) if (!list.Contains(item)) list.Add(item);
      }
    }
    public void Restore()
    {
      var removed = last.Where(item => !list.Contains(item)).ToList();
      var external = list.Where(item => !last.Contains(item)).ToList();
      list.Clear();
      list.AddRange(original.Where(item => !removed.Contains(item)));
      foreach (var item in external) if (!list.Contains(item)) list.Add(item);
    }
  }
  private sealed class Translation(IDictionary<string, string> values, string key) : IChange
  {
    private readonly bool existed = values.ContainsKey(key);
    private readonly string? original = values.TryGetValue(key, out var value) ? value : null;
    private string? last;
    private bool applied, relinquished;
    internal void Set(string value)
    {
      if (relinquished) return;
      if (applied && (!values.TryGetValue(key, out var current) || current != last)) { relinquished = true; return; }
      values[key] = value;
      last = value;
      applied = true;
    }
    public void Restore()
    {
      if (!applied || relinquished || !values.TryGetValue(key, out var value) || value != last) return;
      if (existed) values[key] = original!; else values.Remove(key);
    }
  }
  private static readonly Dictionary<object, Dictionary<string, IChange>> Changes = new(ReferenceEquality.Instance);
  private static readonly List<IChange> Order = [];
  private sealed class ReferenceEquality : IEqualityComparer<object>
  {
    internal static readonly ReferenceEquality Instance = new();
    public new bool Equals(object? a, object? b) => ReferenceEquals(a, b);
    public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
  }
  private static T Track<T>(object owner, string key, Func<T> create) where T : IChange
  {
    if (!Changes.TryGetValue(owner, out var entries)) Changes[owner] = entries = [];
    if (entries.TryGetValue(key, out var previous)) return (T)previous;
    var change = create();
    entries[key] = change;
    Order.Add(change);
    return change;
  }
  internal static void Set<T>(object owner, string key, Func<T> read, Action<T> write, T value, Func<bool>? alive = null)
    => Track(owner, key, () => new Value<T>(read, write, alive ?? (() => true))).Set(value);
  internal static void Replace<T>(List<T> list, IEnumerable<T> data)
    => Track(list, "contents", () => new Contents<T>(list)).Set(data);
  internal static void ReplaceWith<T>(List<T> list, Action apply)
    => Track(list, "contents", () => new Contents<T>(list)).SetNative(apply);
  internal static void Translate(IDictionary<string, string> values, string key, string value)
    => Track(values, key, () => new Translation(values, key)).Set(value);
  internal static void Restore()
  {
    Exception? failure = null;
    for (var i = Order.Count - 1; i >= 0; --i)
      try { Order[i].Restore(); } catch (Exception error) { failure ??= error; }
    if (failure != null) throw new InvalidOperationException("EWD could not restore shared resources.", failure);
    Order.Clear();
    Changes.Clear();
  }
}
