using System.Collections.Generic;
using Data;
using ExpandWorldData.Data;

namespace ExpandWorldData.Spawn;

// Spawn system loads spawns from multiple sources, which means metadata must be combined.
// This makes reloading more complex because existing data can't be cleared.

/// <summary>Coordinates reloading of metadata from multiple sources.</summary>
public sealed class MetadataLoad : System.IDisposable
{
  private readonly Dictionary<SpawnSystem.SpawnData, DataEntry?> PreviousData = Loader.Data;
  private readonly Dictionary<SpawnSystem.SpawnData, List<BlueprintObject>> PreviousObjects = Loader.Objects;
  private readonly Dictionary<SpawnSystem.SpawnData, Requirements> PreviousRequirements = Loader.Requirements;
  private bool Committed;

  public MetadataLoad()
  {
    Loader.Data = [];
    Loader.Objects = [];
    Loader.Requirements = [];
  }

  public void Commit(IEnumerable<SpawnSystem.SpawnData> previous, IEnumerable<SpawnSystem.SpawnData> current)
  {
    var retained = new HashSet<SpawnSystem.SpawnData>(current);
    var data = new Dictionary<SpawnSystem.SpawnData, DataEntry?>(PreviousData);
    var objects = new Dictionary<SpawnSystem.SpawnData, List<BlueprintObject>>(PreviousObjects);
    var requirements = new Dictionary<SpawnSystem.SpawnData, Requirements>(PreviousRequirements);
    foreach (var spawn in previous)
    {
      if (retained.Contains(spawn)) continue;
      data.Remove(spawn);
      objects.Remove(spawn);
      requirements.Remove(spawn);
    }
    foreach (var spawn in retained)
    {
      if (Loader.Data.TryGetValue(spawn, out var entry)) data[spawn] = entry;
      if (Loader.Objects.TryGetValue(spawn, out var entries)) objects[spawn] = entries;
      if (Loader.Requirements.TryGetValue(spawn, out var extra)) requirements[spawn] = extra;
    }
    Loader.Data = data;
    Loader.Objects = objects;
    Loader.Requirements = requirements;
    Committed = true;
  }

  public void Dispose()
  {
    if (Committed) return;
    Loader.Data = PreviousData;
    Loader.Objects = PreviousObjects;
    Loader.Requirements = PreviousRequirements;
  }
}