# A1 format-test status

## Status

**Complete for proceeding to A2.** The current JSON behavior is isolated from scene and UI code and covered by EditMode tests. No scene, prefab, or UI hierarchy was changed.

## Completed

- Moved `CharacterData`, `CharacterSceneData`, `AppSaveData`, legacy export models, and normalization into the testable `TaruckShip.Domain` assembly.
- Moved the current local JSON load/save and backup behavior into `LegacyJsonSaveRepository` in the `TaruckShip.Persistence` assembly.
- Kept `DndSaveManager` behavior and public model names compatible with the existing scene code.
- Added sanitized golden fixtures for the full save, character export, item export with embedded image data, null collections, and corrupted JSON.
- Added a frozen count manifest for all 1,039 entries in `LegacyFieldMap_v1.csv`.
- Kept the user's raw Android and Windows character files outside Git.

## Automated verification

Unity `6000.4.7f1` EditMode tests: **8 passed, 0 failed**.

The tests cover:

1. Current full-save JSON values and per-scene data.
2. Corrupted primary file falling back to a valid `.bak`.
3. Corrupted primary and backup files returning errors without data.
4. Two consecutive saves preserving the prior primary as backup.
5. Normalization of missing legacy collections and invalid names.
6. Version 1 character export.
7. Standalone item export and embedded PNG data.
8. Exact scene/collection counts, non-empty paths, and unique stable IDs across all 1,039 frozen legacy-map rows.

## Scope and next stage

A1 intentionally continues to write the existing JSON format. A4 will replace new writes with the versioned Taruck binary format after A2 and A3 finish stable-field migration and controller consolidation.

A2 may now introduce `PersistentFieldId` and migration on one small test page while using the frozen A0 map and these A1 tests as the compatibility guard.
