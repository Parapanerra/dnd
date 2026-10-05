# Legacy full-save validation: AllCharacters.json

The original character data remains outside the repository and was not committed. This report contains only structural metadata needed for migration testing.

## File identity

- SHA-256: `F91E8BA0D6826177B5B7BAE859B174AA21988726CABDDA8A83BCD5E478050DDC`
- Size: `257892` bytes
- Legacy type: full `AppSaveData` collection export
- Characters: `1`
- `lastActiveCharacterId` refers to an existing character: yes
- Source platform: not confirmed

## Positional data compared with LegacyFieldMap_v1

| Scene | Inputs | Expected | Toggles | Expected | Sliders | Expected | Dropdowns | Expected | Result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `cartaPersonaj` | 102 | 102 | 147 | 147 | 0 | 0 | 16 | 16 | exact |
| `Spels` | 92 | 92 | 280 | 280 | 0 | 0 | 28 | 28 | exact |
| `informForPerson` | 0 | 25 | 0 | 9 | 0 | 0 | 0 | 1 | page is empty |
| `spelBook` | 30 | 30 | 61 | 61 | 0 | 0 | 46 | 46 | exact |
| `petsesn` | 45 | 45 | 10 | 10 | 0 | 0 | 1 | 1 | exact |
| `inventory` | 18 | 18 | 64 | 64 | 0 | 0 | 64 | 64 | exact |

The export also contains empty or non-character scene states for `menu` and `avtoru`. They are preserved by the legacy model but are outside `LegacyFieldMap_v1`.

## Keyed control compatibility

Direct comparison found that many saved keys use root sibling prefix `0000_`, while the same scene objects in the Editor baseline use `0001_`. Comparing the remainder of each path after the root sibling index gives a unique match for every mapped keyed control in the five populated character scenes:

| Scene | Mapped keyed controls | Saved keyed controls | Unique normalized matches | Extra runtime keys |
|---|---:|---:|---:|---:|
| `cartaPersonaj` | 163 | 166 | 163 | 3 |
| `Spels` | 308 | 308 | 308 | 0 |
| `spelBook` | 107 | 110 | 107 | 3 |
| `petsesn` | 11 | 14 | 11 | 3 |
| `inventory` | 128 | 128 | 128 | 0 |

Total uniquely matched mapped keys: `717` of `717`. The nine extra keys belong to runtime UI controls outside the scene baseline and must not shift positional character data.

## Migration requirements confirmed by this file

- Do not use the root object's `GetSiblingIndex()` as persistent identity.
- Prefer the frozen positional mapping for the legacy lists and stable IDs for the new format.
- If legacy keyed aliases are used, normalize or explicitly map the root sibling segment and require an unambiguous match.
- Preserve unknown scene states such as `menu` and `avtoru` until a migration policy explicitly removes them.
- This export is not the fully populated golden fixture because `informForPerson` is empty.
