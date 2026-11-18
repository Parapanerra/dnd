# Character creation wizard (in expansion)

The main menu opens `Assets/Scenes/CharacterCreationPrototype.unity`. The top-right Menu button returns to the main menu; Back on step 01/06 does the same, while Back on later steps returns one step. Leaving a changed, unsaved draft asks for confirmation.

## Current supported paths

- Fighter, Barbarian, Rogue, Monk, Paladin, Ranger, Sorcerer and Bard: levels 1–20 in both editions, with their implemented PHB/XPHB subclasses. Paladin includes the three 2014 oaths and four 2024 oaths. Ranger includes Hunter/Beast Master in 2014 and those plus Fey Wanderer/Gloom Stalker in 2024. Sorcerer includes Draconic/Wild Magic in 2014 and those plus Aberrant/Clockwork in 2024, metamagic, full spell progression, sorcery points and optional ready forms. Bard includes Lore/Valor in 2014 and those plus Dance/Glamour in 2024, Expertise, inspiration recovery, Magical Secrets, full spell progression and optional ready forms.
- Artificer (Винахідник): TCE/EFA levels 1–20. Alchemist/Armorer/Artillerist/Battle Smith, with Cartographer in EFA; armor models, full half-caster slots, automatic subclass spells, level-gated infusions/item plans in the existing 12 preparation rows, limited-use resources and optional ready forms. Concrete item choices come from the source tables; open common/uncommon/rare categories use explicit player-specified names and DM-confirmed properties. Infusions are known choices, not automatically applied equipment bonuses.
- The other selectable classes currently remain at level 1. Higher levels are exposed only after completing that class and its final validation batch. See `CHARACTER_CREATION_EXPANSION.md` for the active class and remaining work.
- Class, level, species, background, weapon and spell choices use dropdowns. Required choices begin unset and are validated before proceeding.
- Standard array, 27-point buy, or six individual 4d6-drop-lowest rolls; assigned array/roll values cannot be reused.
- Edition-aware species, backgrounds, class skills, Expertise, languages, tools, origin feats, ability increases and starting equipment.
- The sheet receives numeric Hit Dice, scores/modifiers in their respective fields, attacks, proficiencies, equipment, ammunition, class/species resources and compact overflow notes. Nonempty note panels open automatically; all eight notes support bounded scrolling.
- Spell cards use the existing casting-action and duration selectors and preparation checkboxes, across all five spellbook pages. Paladin oath spells are automatically prepared outside the normal preparation count; Blessed Warrior selects two Cleric cantrips using Charisma.
- Paladin Lay on Hands uses a saved point pool, a whole-number spending slider and a Give action. Moving the slider does not spend points. Long Rest restores the pool; Short Rest does not. It has a separate resource-selector entry before Custom Feature, without an image. Custom Feature retains its original independent controls.
- Paladin Channel Divinity uses its existing main-sheet counter: 2014 restores its one use on Short/Long Rest; 2024 restores one on Short Rest and all on Long Rest. Aura of Protection is included in generated saving-throw values from level 6; the description states its conditions.
- Summon variants remain in the asset catalogue. The forms page has a Ready Forms dropdown below the form list; selecting a variant adds only that form with its HP and statistics. The wizard no longer writes every variant into the save. A stable template ID hides added variants even after renaming or save/reload; deleting a form makes its template available again. Blank-form creation remains available with +. Lower form notes include slot-by-slot HP/AC/damage tables and caster/proficiency formulas. These are reference notes; existing statistics are edited manually, with automatic recalculation deferred to a future level-up feature. The picker itself is transient and excluded from saved field collections.
- New text is localized in Ukrainian, English and Russian during implementation. A final terminology audit remains pending.
- General feat alternatives to ASI beyond the existing Epic Boon choices, a full remaining-class progression catalogue, automated post-creation level-up and the final cross-class audit are not complete.
- Equipment remains text in the sheet inventory; item-grid records are not generated. The independent candidate is committed through the existing atomic save repository. The binary format is unchanged.

The new scene uses a dedicated font and existing project button/background artwork. The 2560×1440 tavern background covers the screen without changing aspect ratio. Wide layouts cap the panel at 1460 logical units, direct action buttons at 520 and navigation buttons at 260×66, so a large monitor does not stretch them across the whole screen.
Portrait uses a single scrollable column with larger controls; landscape/wide screens add the step sidebar and live summary. Safe area is respected. New wizard text is added to the Ukrainian, English, and Russian localization table as each feature is implemented.

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

- Before expansion on 26 October 2026: commit `4ffd26f`, GitHub branch `codex/backup-character-wizard-prototype-2026-10-26`, tag `checkpoint/character-wizard-prototype-2026-10-26`.

- Commit: `58244c95e77879064c48733d602d16efec752801`
- GitHub backup branch: `codex/backup-before-character-wizard-2026-10-25`
- Tag: `checkpoint/before-character-wizard-2026-10-25`
- Local project source archive, Git bundle and player-save copy: `_codex_tmp/Backups/before-character-wizard-2026-10-25/`.
- Generated Unity caches and builds are reconstructable and are excluded from the source archive.

- 2026-10-29 Artificer completion: 220 distinct level/subclass/model candidates passed (88 low-level candidates rechecked after correcting a future-level eligibility bound), 4166 selected-spell checks, 795 ready-form writes, 160 additional full-list description checks; zero missing English text/name mappings in that scope. This is not a manual phone/Play UI pass. See tools/character-data/artificer-validation-summary.json.

Blood Hunter now supports levels 1–20 using the supplied pre-2024 PDF: Ghostslayer, Profane Soul, Mutant and Lycan. The native hemocraft die, blood-curse counter and four crimson-rite fields are filled directly. Profane Soul uses existing spell cards and pact slot controls; temporary transformations/mutagens do not silently change the base sheet. Final rules/write audit: 436/436 candidates; visual phone/Play verification is separate.

Cleric supports levels 1–20 in PHB 2014 (seven domains) and XPHB 2024 (four domains). Domain spells are separate from normal preparation. Channel Divinity uses the native 0–4 counter, with edition-specific short-rest recovery. Domain resources use the existing magic rows; conditional Intervention cooldowns are tracked manually. Selected XPHB Summon Celestial provides two optional ready-form templates. Final rules/write audit: 400/400 scenarios; visual phone/Play checks are separate.
