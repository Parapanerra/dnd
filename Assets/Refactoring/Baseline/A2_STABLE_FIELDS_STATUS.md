# A2 stable-field migration status

Completed on 2026-10-06 with Unity 6000.4.7f1. Ready for A3.

## Implementation

- Added serialized PersistentFieldId components to all 1,039 controls from the frozen A0 map.
- Each binding stores an immutable intended ID, target component, original collection/index, and original keyed alias. IDs are assigned in the Editor and never generated at runtime.
- Both existing scene controllers now use StableFieldStorage for standard field reads/writes. Their consolidation remains A3.
- Stable string and integer entries use the StableField_v1_ prefix. Added a typed floatData collection and normalization for older saves without it.
- Read order: an existing stable key first, then the frozen positional slot, then the exact frozen integer alias if the slot is absent. Current hierarchy indices never supply a mapped field's legacy value.
- Successful legacy reads add a stable entry and set stableFieldVersion to 1 in memory. Existing save paths persist these additions. The version identifies the reader used, not a guarantee that every field is populated; individual key presence handles partial migration.
- Missing legacy slots remain unmigrated until values become available or the user saves new values.
- Saves retain legacy lists in their frozen positions alongside stable keys; resets clear both forms and the migration marker.
- Existing special shared fields, health, inventory cell keys, and runtime-created controls retain their existing specialized behavior. Name-dependent special behavior is addressed in A6.
- Local storage and exports remain JSON until A4.

## Rollout and validation

Pilot: informForPerson, 35 fields; 14/14 tests passed before the other scenes were updated.

Final bindings:

| Scene | Fields |
|---|---:|
| cartaPersonaj | 265 |
| petsesn | 56 |
| Spels | 400 |
| inventory | 146 |
| informForPerson | 35 |
| spelBook | 137 |

Final TaruckShip.EditModeTests result: **16 passed, 0 failed** (8 existing A1 tests and 8 A2 tests).

Tests cover frozen indices after renaming/reordering, stable-value JSON round trips, reverse-order writes preserving old slots, missing slots without copying neighbouring values, float/toggle/both dropdown types, separate characters/pages, resets, and invalid or missing IDs.

The all-scenes test opens the actual six scenes, validates every binding against the CSV, reorders and renames field objects in memory, and checks every field against a deterministic legacy fixture. It discards scene mutations without saving. The fixture uses distinguishable synthetic values for migration checks; dropdown sentinels are not a UI option-range fixture.

The Editor validator also rejects newly added standard scene controls without an ID. Use **Tools > DnD > Persistence > Validate Stable Field IDs** after editing scenes. The installer is only for the frozen A0 controls; new fields need new IDs with no reused legacy slot.

LegacyFieldMap_v1.csv remains unchanged:

`377F856A8D65392F101C02A03100C419CFAA1212E689DCC233909EB98FA1E572`

A serialized-object comparison against the A1 commit found no removed existing scene objects and no unexpected changes to their properties. Scene changes consist of new marker components, component references, prefab component overrides, and the stripped prefab references Unity needs to serialize them. ProjectSettings changes caused by Unity were reverted.

## Verification limits

Compilation and tests ran in the Windows Editor. No Android build/device smoke test or interactive UI smoke pass was performed in A2. The automated tests establish migration and binding behavior; the full device smoke checklist remains required before releasing the later migration stages.
