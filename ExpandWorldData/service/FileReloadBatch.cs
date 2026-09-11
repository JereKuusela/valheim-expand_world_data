using System;

namespace Service;

// Main-thread file notifications may arrive after their generating call has
// already loaded the files. Compare with that applied snapshot before reloading.
internal sealed class FileReloadBatch(Action reload, Func<string> snapshot)
{
  private string? loaded;
  private bool pending;
  private float quietTime;

  public void Notify()
  {
    pending = true;
    quietTime = 0f;
  }

  public void MarkLoaded(string value) => loaded = value;

  public void Clear()
  {
    loaded = null;
    pending = false;
    quietTime = 0f;
  }

  public void Update(float deltaTime)
  {
    if (!pending) return;
    quietTime += Math.Max(0f, deltaTime);
    if (quietTime < 0.5f) return;
    pending = false;
    if (snapshot() != loaded)
      reload();
  }
}
