# A0 baseline status

## Completed

- The pre-refactoring state is preserved in Git commit `159230d9f324f4fd8dc19ff35df5d336895da1ba` and pushed to `origin/main`.
- `LegacyFieldMap_v1.csv` was generated from that exact commit with Unity `6000.4.7f1`.
- The map covers all six character-sheet scenes enabled in Build Settings.
- The generator reproduces the current `GetControlPath()` ordinal sorting, the shared legacy offsets for `InputField`/`TMP_InputField` and `Dropdown`/`TMP_Dropdown`, and the current health-bar filtering.
- The baseline contains 1,039 persisted controls.
- `LegacyFieldMap_v1.csv` SHA-256: `377F856A8D65392F101C02A03100C419CFAA1212E689DCC233909EB98FA1E572`.
- Empty legacy paths: 0.
- Empty proposed stable IDs: 0.
- Duplicate typed legacy paths: 0.
- Duplicate proposed stable IDs: 0.
- Generating the baseline did not modify any `.unity` scene or `ProjectSettings` file.
- Unity compiled and executed the editor tool successfully.

## Scene totals

| Scene | Persisted controls | Excluded health controls |
|---|---:|---:|
| `cartaPersonaj` | 265 | 2 |
| `petsesn` | 56 | 1 |
| `Spels` | 400 | 0 |
| `inventory` | 146 | 0 |
| `informForPerson` | 35 | 0 |
| `spelBook` | 137 | 0 |

Detailed per-type counts and scene GUIDs are recorded in `LegacyFieldMap_v1_REPORT.md`.

## Required before A0 can be completed

One real full collection export has now been inspected. Its character data remains outside Git. The structural result is recorded in `LegacySave_AllCharacters_VALIDATION.md`; five populated scenes match the frozen positional map exactly, while `informForPerson` is empty.

The following legacy artifacts or confirmations are still required:

1. A Windows `DndCharactersData.json` containing several characters.
2. Confirmation of whether the inspected `AllCharacters.json` was exported on Windows or copied from Android.
3. A single-character export made on Windows.
4. An item export containing an image made on Windows.
5. Equivalent representative files made on Android.
6. A character with populated fields on every character-sheet page, especially `informForPerson`.

After these files are supplied, copy sanitized test fixtures into the test-data location created during A1. Preserve the original files outside the project as recovery copies. Then record the legacy save size and save duration on Windows and Android and run the core smoke checklist.

Do not mark A0 complete and do not start scene hierarchy changes until these artifacts are available and checked against `LegacyFieldMap_v1.csv`.
