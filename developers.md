# Developers

Default biome and world data can be directly modified with code. This ensures that your biomes and world changes will always be on the default configs.

## Step 1

Add references to `ExpandWorldData.dll` in your mod project. For example:

```xml
    <Reference Include="ExpandWorldData.dll">
      <HintPath>..\..\Libs\ExpandWorldData.dll</HintPath>
    </Reference>
```

## Step 2

Add dependency to Expand World Data in the manifest.json file.

Alternatively you can directly embed the `ExpandWorldData.dll` in your mod with ILRepack. In this case you also need to embed the `YamlDotNet.dll`.

## Step 3

Use the API in your mod code.

Recommended work flow:

1. Modify Expand World Data configs until you are happy with the results.
2. Copy the relevant parts of the `expand_biomes.yaml` and `expand_world.yaml` files to your mod code.
3. Test by removing the Expand World Data configs and restarting the game.

For example:

```csharp
  public void Start()
  {
    ExpandWorldData.BiomeYaml myBiome = new()
    {
      biome = "mybiome",
      name = "My biome",
      // other fields...
    };
    ExpandWorldData.Api.AddBiome(myBiome);
    ExpandWorldData.WorldYaml worldChange = new()
    {
      biome = "mybiome",
      // other fields...
    };
    // World generation selects biomes from top to bottom, so it's important to add your biome at the correct position.
    // Ordinal 2 is right after the Ocean biome which is a good starting point for custom biomes.
    // See the default `expand_world.yaml` to see how the biomes are ordered.
    int ordinal = 2;
    ExpandWorldData.Api.ChangeWorld(worldChange, ordinal);
  }
```
