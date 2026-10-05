# A0 baseline status

## Status

**Complete for proceeding to A1.** No further user files are required before the test-fixture stage. The available artifacts cover the Android collection format, Android image data, Windows character export, Windows item export, and an Android-to-Windows round trip.

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

## Accepted artifact set

One real Android full collection export and two Windows exports have now been inspected. Their character and item data remain outside Git. Structural results are recorded in `LegacySave_AllCharacters_VALIDATION.md` and `LegacyWindowsExports_VALIDATION.md`.

The Windows single-character export confirms that reopening and saving the Android data on Windows accumulates both the old and current hierarchy-derived keyed paths. Positional data remains exact in populated scenes, which confirms that migration must use the frozen positional map before writing stable IDs.

The accepted A0 evidence is:

1. Android full collection export with one character and real inventory image data.
2. Windows single-character export produced after using the Android data.
3. Windows standalone item export.
4. `LegacyFieldMap_v1.csv` covering every current persisted control, including pages that are empty in the real character.
5. Clean Unity compilation and generation on Unity `6000.4.7f1`.

## Work carried into A1 and later verification

- Create sanitized golden fixtures from the inspected structures; do not commit the user's raw character data.
- Generate deterministic non-empty values for `informForPerson` and other empty pages before A2 changes any scene hierarchy.
- Build an item fixture using the validated Android JPEG and the Windows item schema.
- Test legacy local-save, full-export, character-export, and item-export readers separately even where their payload models overlap.
- Record real save duration on Windows and Android during the A5 performance pass; the current file sizes are already recorded.
- Run the full device smoke checklist before declaring the later migration stages complete.
