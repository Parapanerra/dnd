# Legacy fixtures

These fixtures contain synthetic values, not the user's private characters.

legacy_all_fields_v1.json is a frozen full-collection fixture covering every positional slot in LegacyFieldMap_v1.csv. Text values identify their original scene/index; alternating toggles and distinct dropdown integers exercise migration independent of hierarchy order. Dropdown sentinel values are for storage/mapping assertions and may exceed UI option ranges.

Keep this fixture and the A0 map unchanged when moving UI fields. Changing both to match a reordered scene would hide migration regressions.
