# Character creation prototype / B4

Open `Assets/Scenes/CharacterCreationPrototype.unity` and press Play.
An entry from the main menu has not been added yet. The top-right Menu button returns to the main menu; Back on step 01/06 does the same, while Back on later steps returns one step. Leaving a changed, unsaved draft asks for confirmation.

## Available test path

- D&D 2014 or 2024, Fighter level 1.
- Standard Human or Hill Dwarf (2014); Human or Dwarf (2024).
- Soldier or Criminal; their skill proficiencies and bonuses follow the selected edition.
- Standard array with swaps, 27-point buy, or six 4d6 rolls (drop the lowest die) assigned to abilities.
- Two class skills, a unique Human skill in 2024, languages and tool choices.
- Four fighting styles and two starting equipment packages per edition.
- Review, validate and create an independent character using the existing atomic save repository.
- 2024 Human currently uses the «Міцний» origin feat and Medium size.
- 2024 Fighter uses the three weapon masteries corresponding to the selected equipment package.
- 2024 backgrounds currently use the 50 GP equipment alternative.
- Equipment is written into the sheet's text inventory; item-grid records are not generated yet.
- The sheet receives numeric Hit Dice, score/modifier fields, the attack row and its details, proficiencies, equipment, ammunition, relevant resources, and open notes for additional features. The current Fighter 1 test path has no spellcasting or other classes' feature dice.
- Edition and the complete creation draft persist in existing shared string entries. The binary format is unchanged.

The new scene uses a dedicated font and existing project button/background artwork. The 2560×1440 tavern background covers the screen without changing aspect ratio. Wide layouts cap the panel at 1460 logical units, direct action buttons at 520 and navigation buttons at 260×66, so a large monitor does not stretch them across the whole screen.
Portrait uses a single scrollable column with larger controls; landscape/wide screens add the step sidebar and live summary. Safe area is respected. UI text in this prototype is Ukrainian; full English/Russian localization is pending.

## Structure

- `Domain/CharacterCreationDraft.cs` and `CharacterCreationRules.cs`: independent model, rules, dependency repair and validation.
- `CharacterCreation/CharacterCreationWizard.cs`: six-step navigation and draft editing.
- `CreationWizardUi.cs`: uGUI view factory.
- `CreationSheetBindings.cs` and `SheetBindings.asset`: explicit stable field bindings captured from the real sheet.
- `CharacterCreationService.cs`: candidate construction, persistent edition metadata and atomic commit through the existing save service.
- `Editor/CharacterCreationSceneBuilder.cs`: one-time scene creation; refuses to overwrite an existing prototype.

## Sources

Rule facts were checked against the official Wizards/D&D Beyond rules on 25 October 2026:

- 2014 classes: https://www.dndbeyond.com/sources/dnd/basic-rules-2014/classes#Fighter
- 2014 races: https://www.dndbeyond.com/sources/dnd/basic-rules-2014/races#Dwarf
- 2024 classes: https://www.dndbeyond.com/sources/dnd/br-2024/character-classes
- 2024 origins: https://www.dndbeyond.com/sources/dnd/br-2024/character-origins
- SRD 5.2.1: https://media.dndbeyond.com/compendium-images/srd/5.2/SRD_CC_v5.2.1.pdf

Descriptions in the prototype are concise Ukrainian paraphrases. Before expanding the catalog for release, audit the content sources, include the applicable SRD attribution and finish localization.

## Rollback checkpoint

- Commit: `58244c95e77879064c48733d602d16efec752801`
- GitHub backup branch: `codex/backup-before-character-wizard-2026-10-25`
- Tag: `checkpoint/before-character-wizard-2026-10-25`
- Local project source archive, Git bundle and player-save copy: `_codex_tmp/Backups/before-character-wizard-2026-10-25/`.
- Generated Unity caches and builds are reconstructable and are excluded from the source archive.
