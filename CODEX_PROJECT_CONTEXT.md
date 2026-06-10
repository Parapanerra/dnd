# DnD Character Sheet App Context

This Unity project is an Android app for an electronic D&D character sheet.
It is not a game simulator. The real game happens IRL at the table; the app is
only a digital character sheet with multiple pages/tabs, saves, localization,
HP handling, inventory, spells, wild shapes, notes, and calculator helpers.

## User Expectations

- The user prefers direct practical fixes, not broad product brainstorms.
- Do not suggest features that assume the app replaces the tabletop game.
- Do not push to GitHub unless the user explicitly says to push/back up.
- The user often edits Unity scene hierarchy manually, then asks Codex to wire logic.
- Be careful not to overwrite scene/prefab changes the user made in Unity.

## Project Shape

- Workspace: `D:\dnd2.0test\dnd`
- Main scenes include:
  - `menu`
  - `cartaPersonaj`
  - `inventory`
  - `spelBook`
  - `petsesn` for wild shapes
  - `informForPerson`
- Git remote: `https://github.com/Parapanerra/dnd.git`
- GitHub Desktop git path often used:
  `C:\Users\Tom\AppData\Local\GitHubDesktop\app-3.5.8\resources\app\git\cmd\git.exe`

## Important Systems

- `DndSaveManager.cs`
  Handles character data, active character, scene data, save/load, export/import.

- `CharacterSheetManagerScene1.cs`
  Main per-scene save/load manager for character sheet pages.

- `MainMenuManager.cs`
  Handles main menu character list, character create/delete/open, import/export UI.

- `RuntimeLocalization.cs`
  Custom runtime localization layer for Ukrainian, English, Russian.
  It applies translations to `Text`, `TMP_Text`, `TextMesh`, dropdown options,
  calculator text, inventory cells, and some dynamic generated labels.

- `ManualLocalizedText.cs`
  Component the user can place on scene text objects and manually set source text.

- `CalculatorManager.cs`
  Dice calculator plus HP modes: max HP, damage, healing, short rest, long rest,
  temporary HP, potion counters/usage.

- `HealthBar.cs` / `HealthBar1.cs`
  HP bar logic. HP sliders are visually fragile because the scene fill image is
  custom; the current code drives fill images manually through `Image.fillAmount`.

- `InventoryItemCell.cs`
  Inventory cells, category dropdowns, custom item images via gallery, per-item
  import/export.

- `InventoryPageManager.cs`
  Inventory page switching and page title.

- `SpellbookPageSwitcher.cs`
  Spellbook page switching, dropdown persistence, page title, nav buttons.

- `WildShapeTitleUpdater.cs`
  Wild shape page title and `Forma/Form` button text handling.

## Recent Work / Known Areas

- Multiple characters exist in one app; each character has separate saved scene data.
- Character list in menu uses user-created rows in a scroll view.
- There is localization to Ukrainian, English, Russian.
- App language defaults from phone locale on first launch:
  Ukrainian phone -> Ukrainian
  Russian/Belarusian phone -> Russian
  everything else -> English
  If user chose language before, saved choice wins.
- Localization has many hardcoded dictionary entries in `RuntimeLocalization.cs`.
- Some dynamic text is translated by pattern:
  - `Forma 1` / `Форма 1`
  - `Дика форма №1`
  - `Сторінка №1`
  - `Круг 1`
- `КО` means armor class:
  - EN: `AC`
  - RU: `КД`
- `Таємничі заклики` means Warlock `Eldritch Invocations`, not Mystic Arcanum.

## Git Notes

- The user explicitly asked: do not push after every change.
- Only commit/push when asked, e.g. "зроби бекап", "пуш на гід".
- Last pushed backup was `1044409 Backup localization updates`.

## Communication Notes

- User writes mostly Ukrainian/Russian informal.
- Keep answers short and practical.
- Avoid saying "I did not push" repeatedly unless the user asks.
- If unsure about hierarchy, ask for screenshot or inspect scene YAML.
