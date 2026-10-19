# Requirement Lists

Fields marked as `list` or `distance list` accept either a single string or an array of strings (one per row).

- Rows are combined with AND (all rows must pass).
- Entries inside a row are separated with a comma and are combined with OR (any entry passes).
- `forbidden` fields use the same logic, but fail when the whole condition is true. A single row means that any of its entries blocks.
- Field-specific restrictions and exceptions are described with each field.

## List

Lists without distances use the format `nameA, nameB` for each row.

```yaml
# Requires either key.
requiredGlobalKeys: defeated_eikthyr, defeated_gdking
```

```yaml
# Requires defeated_bonemass AND either defeated_eikthyr OR defeated_gdking.
requiredGlobalKeys:
- defeated_eikthyr, defeated_gdking
- defeated_bonemass

# Blocks when either key is set.
forbiddenGlobalKeys: defeated_moder, defeated_goblinking
```

## Distance List

Distance lists use the format `nameA, nameB, distance, nameC, distance` for each row. Each distance applies to all names since the previous distance (or the start of the row). Distances are in meters. Names without a following distance are ignored with a warning.

```yaml
# Within 50 meters of a Crypt or a Runestone, or within 100 meters of a Troll Cave.
requiredLocations: Crypt2, Runestone_Boars, 50, TrollCave02, 100
```

```yaml
# Within 50 meters of a Crypt AND within 100 meters of a Troll Cave.
requiredLocations:
- Crypt2, 50
- TrollCave02, 100
```

```yaml
# Not within 30 meters of a workbench or a stonecutter.
forbiddenObjects: piece_workbench, piece_stonecutter, 30
```

The accepted names and how nearby locations or objects are checked depend on the field.
