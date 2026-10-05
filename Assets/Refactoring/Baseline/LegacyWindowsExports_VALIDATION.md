# Legacy Windows export validation

The original character and item exports remain outside the repository and were not committed. This report contains only structural metadata required for migration testing.

## Single-character export

- SHA-256: `1DB9A4B9574DD13B4CA0FF6EB48842E3017E31EA950AB8540A11A74DD1AE680F`
- Size: `295985` bytes
- Export wrapper version: `1`
- Character ID and name are present.
- Scene states: `9`
- Shared string entries: `3`

### Positional data compared with LegacyFieldMap_v1

| Scene | Inputs | Expected | Toggles | Expected | Sliders | Expected | Dropdowns | Expected | Result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `cartaPersonaj` | 102 | 102 | 147 | 147 | 0 | 0 | 16 | 16 | exact |
| `Spels` | 92 | 92 | 280 | 280 | 0 | 0 | 28 | 28 | exact |
| `informForPerson` | 0 | 25 | 0 | 9 | 0 | 0 | 0 | 1 | page is empty |
| `spelBook` | 30 | 30 | 61 | 61 | 0 | 0 | 46 | 46 | exact |
| `petsesn` | 45 | 45 | 10 | 10 | 0 | 0 | 1 | 1 | exact |
| `inventory` | 0 | 18 | 0 | 64 | 0 | 0 | 0 | 64 | page is empty |

The export also contains legacy states for `menu`, `avtoru`, and `proApk`. They must be preserved until migration policy explicitly handles them.

### Key aliases accumulated after Android-to-Windows use

The Windows character export contains both the earlier Android keyed paths and newly written Windows keyed paths. After ignoring only the root sibling number, each mapped control in populated scenes has two aliases rather than one:

| Scene | Mapped keyed controls | Saved keyed entries | Controls with two normalized aliases |
|---|---:|---:|---:|
| `cartaPersonaj` | 163 | 332 | 163 |
| `Spels` | 308 | 616 | 308 |
| `spelBook` | 107 | 324 | 107 |
| `petsesn` | 11 | 25 | 11 |

This confirms that normalizing `GetSiblingIndex()` alone is not a sufficient permanent identity strategy. Migration must use the frozen positional mapping when a stable ID is absent, write one stable ID, and stop carrying duplicate hierarchy-derived aliases forward.

## Item export

- SHA-256: `C7A787A0686FF03B3CAC07BFA4886264D9213F87CDB31C09AC3961D968818BB3`
- Size: `246` bytes
- All expected `InventoryItemExportData` properties are present.
- Category: weapon (`0`)
- Weapon index: `6`
- Other stored dropdown indexes: `0`
- Embedded image length: `0`

The item is a valid legacy structural fixture, but it does not satisfy the separate image-transfer fixture requirement.
