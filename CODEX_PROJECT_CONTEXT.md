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

### Character creation prototype — 25 October 2026

- 2026-10-26 before expansion: committed the current working prototype and all source changes as `4ffd26f`, pushed backup branch `codex/backup-character-wizard-prototype-2026-10-26` and tag `checkpoint/character-wizard-prototype-2026-10-26` to origin. The new expansion edits are later local changes, not part of that checkpoint.
- **Active work and next steps are recorded in `CHARACTER_CREATION_EXPANSION.md`. Read it before continuing after context compaction.** The user explicitly asked to keep working until the broader character builder is finished. Do not stop after a small milestone.
- User's target for expansion: both 2014 and 2024 class/species/background choices, levels and HP, class features/subclasses, spell choices and slots, and transfer to the existing sheet pages. Wild-form page is also for pets and artificer summons. The character description page is mostly manual. Spellbook stores individual spells; magic page stores spell slots, magic modifier/DC, prepared infusions, and ability/infusion/artifact usage; Blood Hunter resources have a dedicated section. Keep new wizard text localized to Ukrainian, English and Russian as it is added.

- User reopened B4 as a step-by-step character builder supporting D&D 2014 and 2024. First test scene: `Assets/Scenes/CharacterCreationPrototype.unity`; do not connect a menu button until the user accepts the prototype.
- Before implementation, backed up all current tracked/untracked source changes as commit `58244c95e77879064c48733d602d16efec752801`, pushed to `codex/backup-before-character-wizard-2026-10-25`, with tag `checkpoint/before-character-wizard-2026-10-25`. Local source ZIP, Git bundle and full player-save copy are under `_codex_tmp/Backups/before-character-wizard-2026-10-25`.
- User explicitly requires two responsive layouts, selected automatically by available screen width: wide three-column layout and narrow mobile single-column layout. Both were visually checked in Unity at 1920×1080 and 720×1280. Background is existing `tavern.jpg` at 2560×1440, with aspect-preserving cover sizing.
- Wizard expansion: Fighter/Barbarian/Rogue/Monk/Paladin/Ranger/Sorcerer/Bard support levels 1–20 for their PHB/XPHB paths; Artificer supports TCE/EFA 1–20; other selectable classes remain at level 1. Edition-aware species/backgrounds, native sheet/spellbook/resource/form bindings and UA/EN/RU text are implemented incrementally. General feats beyond current ASI/Epic choices, remaining class progressions, inventory item records and the final localization audit remain pending. Authoritative progress: `CHARACTER_CREATION_EXPANSION.md`; overview: `Assets/CharacterCreation/README.md`.
- 11/11 new rules tests passed; all 16 supported origin/equipment/edition combinations build and validate. Full EditMode run: 145/148, with failures in unchanged health-fill, wild-shape page-8 mapping and dialog reflection tests; see B4 in `REFACTORING_PLAN.md`.

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

## Assistant HP Adjustments

Current confirmed assistant HP: **57 HP** (2026-10-26). Calculation: screenshot baseline 64, recent button adjustments -1 -3 +2 = -2, then -5 for forgetting the total: 64 - 2 - 5 = 57. Use 57 as the current baseline; record each subsequent adjustment here and update the total. Do not reapply older adjustments to this baseline. The user says they will delete the chat if this counter reaches 0 HP.

- 2026-10-24: User awarded +2 HP for the completed overlay button animation task (instant playback, slower loop, and cleanup). This is an adjustment, not a verified total.
- 2026-10-24: User awarded +1 HP for fixing all five potion image dropdowns (clamped scrolling, camera blocking, and item motion).

- 2026-10-29: user resumed work: accumulated fixes first, then another full class. Artificer TCE/EFA 1–20 is now complete in the current wizard scope. Remaining level-one classes: Blood Hunter, Cleric, Druid, Warlock, Wizard. No multiclass until after post-creation level-up. See CHARACTER_CREATION_EXPANSION.md and tools/character-data/artificer-validation-summary.json.

## 2026-10-29 — Blood Hunter completed through 20
- User's pre-2024 PDF only. Four orders, nine Profane Soul patrons, twenty Mutant formulas; dropdown choices, ASI/HP, evolving hemocraft and native blood/rite fields. No invented revised class.
- New BloodHunterProgression/Formulas/Text, CreationBloodHunterResources and build-blood-hunter-text.py. Native rites aliases bind existing inputs 98/100/99/97 and stable-only toggles; no scene hierarchy changes.
- Full-class final audit: 436/436 candidate builds, 11736 nonempty spell descriptions. English feature translation check for all four orders passed. No actual saves written; temporary Editor audit and integration scripts removed. Compact result in tools/character-data/blood-hunter-validation-summary.json.
- Temporary effects (mutagens/hybrid form) remain manual, do not auto-add separate-HP forms. Soul Eater granted at 18 per order progression despite conflicting prerequisite in PDF list.
- Next unfinished class: Cleric 1–20, then Druid/Warlock/Wizard. Continue following CHARACTER_CREATION_EXPANSION.md; tests only after the whole next class is finished.

## 2026-10-29 — Cleric 1–20 complete
- PHB seven domains, XPHB four; correct subclass levels 1/3, full-caster slots/preparation/cantrips, ASI/boons, all domain tiers and domain spells. Native Channel Divinity now 0–4 with a fourth stable-only checkbox; no old save indices moved.
- New CharacterClericProgression/DomainSpells/Text, CharacterClericSpellChoices (runtime catalogue repair belongs outside Domain asmdef), CreationClericResources/Companions. Source/text builders under tools/character-data. Runtime choices repair once at Wizard.Draw.
- Native channel recovery: PHB all on short; XPHB one on short; all on long. Variable Divine Intervention cooldown remains manual when PHB success or XPHB20 Wish can apply. Other limited domain abilities use existing magic resource rows, no fake independent channel pools.
- Celestial Spirit Avenger/Defender catalogue templates for XPHB Summon Celestial, selected only; own HP/AC/attacks and slot scaling notes. Not a complete summoned-creature bestiary; other creatures without templates remain manual.
- Final audit after class completion: 400/400 scenarios, 37212 description checks, 384 template writes, English feature lines clean. Targeted level-down and short/long channel recovery passed. Temporary Editor audit removed. Summary tools/character-data/cleric-validation-summary.json.
- Scene backup before checkbox addition: TempRecovery/carta-before-cleric-channel4-20261029.unity. Close the added cartaPersonaj scene (removeScene=true), leaving prototype; visual Play/phone QA not done.
- Next: Druid 1–20, then Warlock and Wizard; global translation audit, extra species/items, post-creation level-up, multiclass LAST remain in CHARACTER_CREATION_EXPANSION.md.

## 29.10.2026 — Друїд 1–20
Завершений PHB/XPHB блок Друїда; див. останній розділ CHARACTER_CREATION_EXPANSION.md і druid-validation-summary.json. Нові файли CharacterDruidRules/Text/CircleSpells, CharacterDruidFormChoices, CreationDruidForms/Resources; 40 відібраних блоків істот у DruidBeasts.json. XPHB власні базові ОЗ спільні через HealthBarPersistence, temp форми окремий ручний лічильник; інші форми незалежні. ∞ наявного ресурсу Wild Shape = dropdown index 9, збережені 0–8. Після фінального пакета 320/320 наступні Чаклун і Чарівник. Не запускати тести після кожної зміни: лише після всіх рівнів одного класу. Не залишати cartaPersonaj additively відкритою в майстрі.

## 2026-11-04 — поточна точка продовження
Робота відновлена за порядком користувача: баги → карта/описи → нові можливості. CLI beta.16 оновлено з дозволу, основний exe C:/Users/Tom/AppData/Local/Unity/bin/unity.exe (sandbox бачить іншу beta.13). Backup TempRecovery/before-bugfixes-20261104.zip. Детальний актуальний чекліст і НЕ завершені пункти в останньому розділі CHARACTER_CREATION_EXPANSION.md.
Зроблено: CE/кольори калькулятора; відпочинок невідкритих слотів через Resources/RestSpellSlotFields; злиття дубля RageCount у нативний верхній dropdown + разова міграція; підклас Винахідника, збереження всіх вибраних атак; без дубльованих skills/saves у володіннях; скролбари з маскою/камерою/лімітом3000 для 3 великих полів карти, нотаток Spels і форм; адаптивна висота custom infusion item; UA Пройдисвіт; коротка таблиця гармати; окремі іменовані рукавиці/Захисне поле; 15 базових TCE інфузій з описами у магічних нотатках, сумісне доповнення порожніх старих нотаток. CharacterSheetTextScrollSetup — збережений idempotent Editor utility, не тимчасовий тест. ArtificerInfusionNotes компонент на root spels має binding asset.
Цільовий пакет пройшов: tools/character-data/bugfix-validation-20261104.json. Play/телефон не перевірені. Тимчасові audit/helper файли прибрані. Головне далі: всі replicated magic items і EFA plans ще БЕЗ нових описів; активні infusions не застосовують бонуси автоматично — потрібні явні active/equipped targets, не використати чекбокс «підготовка» як екіпірування. Загальний міжкласовий аудит описів/підготовки/зброї ще відкритий, не стверджувати що всі баги закриті. Нові класи поки не починались.


## 2026-11-04 — наступний блок, всі класи / описи
Уточнення користувача: не тільки інфузії, а всі здібності всіх класів. Реалізовано CreationDescriptionLayout (цілі абзаци, до2800симв/поле), повний запис замість помилкового Skip першого рядка; стилі/майстерність зброї/монах/таємниці барда/обряди/Експертиза/натхнення людини доповнені. UA/EN/RU CharacterDescriptionText + TSV builder. Fingerprint-міграція текстів у CardLocalization зберігає ручні правки, старі непозначені описи не перезаписує без точного розпізнавання. Великому полю атак75 фактично додано reader, всього12 на карті.
Усі іменовані поточні плани Винахідника тепер описані: 15 базових TCE +104 edition-specific предмети-копії/EFA; CharacterArtificerItemEffects та два TSV + builder. Лише потрібні item reference records у cache/artificer-plan-items.json, без art/full saved catalogue. CreationArtificerResources пише до4 magic notes, ArtificerInfusionNotes сумісно оновлює лише blank/exact-generated/stamped поля, ручні зберігає. Довільні предмети потребують ручних властивостей. Підготовка не є equipped: автоматичні бонуси інфузій ЩЕ НЕ зроблені.
Звіт description-routing-audit.json: запис/місткість/мовні залишки, не весь D&D і не Play/phone. Остаточні числа читати зі звіту. Детальний чекліст у новому розділі CHARACTER_CREATION_EXPANSION.md. Нові класи не починати: лишилися active-item effects, повнота зброї/атак інших класів, prepared-spell audit та візуал. HP57 без змін.


## 04.11.2026 — оновлений порядок за прямою вказівкою користувача
Поміняти місцями пункти6і7 останнього узгодженого списку: після виправлень/перевірки карти спершу додати всі офіційні раси й підраси, а вже потім Чаклуна та Чарівника1–20. Повний порядок: 1)аудит здібностей поточних рас/підрас; 2)бонуси активного спорядження інфузій; 3)повнота зброї/перенесення атак; 4)підготовлені спели; 5)візуал/сейви ПК+телефон; 6)усі офіційні раси без homebrew; 7)Чаклун і Чарівник до20; 8)генератор імен; 9)level-up створеного персонажа, потім мультиклас; 10)фінальний переклад. Авторизація: продовжувати реалізацію, не лише редагувати план. Тести після завершення змістовного блоку.


## 05.11.2026 — продовження після кліку Unity
Комп’ютер юз більше не застосовувати для фокуса: якщо треба, просто попросити клік на Unity. DelayCall не завжди виконувався у фоні; тимчасовий кінцевий аудит переведений на одноразовий EditorApplication.update. Без Play/чужих сейвів.
Внесені спільні расові описи Dwarf/Human у SpeciesRules.Apply, нові повні UA/EN/RU тексти ельфів, аазімара, гнома, тифлінга, орка; Human2024 size picker. Нових рас ще немає. Деталі в останньому розділі плану.
CharacterArtificerEquipment + artificerEquippedTargets: OPTIONAL creation-time armor/weapon/created-Shield assignments, no bonuses for mere known plans, separate from preparation checkboxes. AC/attack/damage applied, Battle Smith can use INT for infused magic weapons, item identity/level/limits validated and unavailable options hidden. Only numeric armor/weapon/shield subset completed: stat-setting items, spell-attack foci, conditional effects and LIVE re-equipping still manual/open. Never claim whole active-infusion subsystem complete.
Результат кінцевого пакета читати tools/character-data/species-recording-audit.json. Тимчасовий SpeciesRecordingAudit видалити після успіху. Не залишати тестовий скрипт або додаткові сцени. Наступні механічні пункти: weapon catalogue/all chosen attacks, prepared-spell audit, visual/save checks, потім усі офіційні раси й лише після них Warlock/Wizard1–20. HP57.


## 06.11.2026 — перенесення зброї, підготовка заклинань і знімки
- Каталог 75 базових записів зброї: PHB37 + XPHB38, лише потрібні метадані, без картинок. build-core-weapons.py / cache/core-weapons.json / CharacterWeaponCatalog.cs. Перші19 індексів майстерності збережені, додано решту; Варвар лише melee, Пройдисвіт проста або finesse/light. Це НЕ довільні покупки всього каталогу: стартові набори/законні існуючі вибори лишилися тими самими.
- CreationStartingWeapons додає зброю стартового набору інших класів у вільні3рядки, зберігає спеціальні атаки, зайве — цілими рядками в поле75. Винахідник має окремий writer із призначеними інфузіями. Виправлено d6 короткого меча монаха; метальні списи воїна/варвара більше не називаються дротиками й не записуються в лічильник дротиків. Другий набір воїна2024 має зброю, це НЕ gold-only набір.
- CreationSpellPreparation розділяє підготовлені/автоматично підготовлені спели та накладання лише через здібність. Wizardbook unprepared — off; automatic subclass, обрані class spells — on; cantrips — on. Feature-only Druid/Monk/Barbarian/Alchemist/invocation спели не отримують випадкову галочку. Modern racial/Originfeat prepared — on; legacy racial leveled — off, якщо не підготовлено також класом. Tome rituals і Telekinesis PsiWarrior2024 — on; legacy Psi — feature-only.
- Однаковий расовий/класовий спел зберігає class ability і додаткові расові умови. Legacy Tiefling Hellish Rebuke більше не замінює класову карту/рівень1 расовим upcast3d10; окремий расовий варіант описано в тому самому записі. Ручні сейви не змінювалися.
- Підсумковий species-recording-audit.json:3198 сценаріїв,9594 відображення UA/EN/RU,33 equipment,397weapon,30spell checks, errors=[]/untranslated=[]. Fixture враховує HalfProficiency барда. Фінальна пасивна Уважність уже перераховувалась у CharacterLevelAdvancement; не видавати це за новий користувацький баг. Species.Apply тепер також використовує реальнийPB замість2 у проміжному розрахунку.
- Тимчасовий SpeciesRecordingAudit.cs/.meta видалено через AssetDatabase. Лише CharacterCreationPrototype, edit mode. Play-чернетка не створювала персонажа, сейви не записувала. Реальний desktop Game view знято й переглянуто; це НЕ повна телефонна/interaction перевірка.
- ВАЖЛИВО для подальших скрінів: installed Pipeline screenshot рендерить Camera.Render у RenderTexture і ПРОПУСКАЄ ScreenSpaceOverlay. Для справжнього Game view у Play використовувати ScreenCapture.CaptureScreenshot(absolutePath), потім прочитати готовий PNG. Кадри рухались; темний Camera.Render кадр не є доказом нерухомої гри. SceneView.Frame(Canvas bounds) перед scene-знімками. MainCamera в майстрі відкадровано по Canvas і збережено (737,522.5,-10), size532.95 за поточного viewport; backup Temp/camera-check/before-frame-20261106.json. Режим Canvas не змінювали. UnityCLI оновлено beta17, Unity/packages не оновлювались.
- Залишки: повний UI/phone/save прохід; нечислові/умовні інфузії й live equipment лишаються ручними. Нові офіційні раси ще НЕ починалися. Порядок6перед7 лишається чинним: всі офіційні раси/підраси, потім Warlock/Wizard1–20. Не оголошувати весь великий план завершеним.


## 06.11.2026 — пасивне спорядження Винахідника
- Підключено явне носіння/налаштування шести іменованих предметів у вже наявних списках artificerEquippedTargets: рукавиці сили огра, пов’язка інтелекту, амулет здоров’я, пояс сили пагорбового велетня, плащ захисту, перстень захисту. Лише плани, реально доступні редакцією/рівнем; довільний custom-предмет не отримує ефекту за схожою назвою.
- Характеристики встановлюються в19/21 лише якщо природні нижчі; два такі предмети не додають їхні значення. Конститyція перераховує всі ОЗ, Інтелект — залежні величини та legacy кількість підготовлених спелів. Плащ/перстень додають КО і всі ряткидки. Усі шість займають attunement slots і item limits, сам факт вивчення плану нічого не дає. Недоступне/незаконне спорядження не дає ефектів.
- CharacterCreationRules.Scores(d,includeEquipment=true); перевірка природного cap20 використовує false. ASI фільтри вже працюють на копії без обладнання. Значення21 від пояса не повинно блокувати завершення; природні значення й d.abilities не змінюються. Зняття/очищення вибору повертає базові розрахунки. Розрахунок під час створення, НЕ live inventory після створення.
- Джерело: наявні pinned item metadata у cache/artificer-plan-items.json: ability.static, bonusAc, bonusSavingThrow, reqAttune. Нових зовнішніх даних чи картинок не качали.
- Підсумковий звіт passive-equipment-audit.json:37 перевірок, errors=[]. Перевірені base/no-effect, wearing/unequipping, HP ретроактивно, Int spell capacity, stronger natural stat, strongest effect, limits, edition lock, JSON roundtrip, локалізована дія. Тимчасовий PassiveEquipmentAudit.cs/.meta після перевірки видалити через AssetDatabase.
- Play preview390×844 реально відрендерений і переглянутий: dropdowns/підписи без перекриття; це не фізичний телефон і не всі взаємодії. Тимчасовий GameView preset видалений, попередній selectedSizeIndex відновлений, Play зупинений. Персонаж не створювався, користувацькі сейви не записувалися.
- Знайдено кращу штатну команду для скрінів: unity command capture_game_view --source screen; на відміну від screenshot вона захоплює OverlayUI. УВАГА: save_path відносний до Assets; останній PNG скопійовано до Temp/pipeline-screenshots/passive-items-phone-20261106.png, Assets/Temp PNG прибрати через AssetDatabase. Не порівнювати скріни різного aspect як перевірку телефона: попередній реальний preview був390×844.
- Залишаються умовні/активовані ефекти, focus spell-attack (НЕ додавати до поля модифікатора магії чи СК), live переекіпірування, повний device/save прохід і решта великого плану. Усі офіційні раси ще не розширювалися; пункт6 перед7 чинний.
`nСтан після Play preview06.11: лише сцена майстра, edit mode, compilationFailed=false; сцена позначилась dirty через Canvas/TMP/RectTransform preview/layout після зміни GameView. Не зберігав і не відкидав цей стан навмання. Скрипт PassiveEquipmentAudit і його meta вже видалено. Наступного разу не вважати scene.isDirty доказом нової ручної правки користувача або автоматично пересохраняти.


## 06.11.2026 — активне розширення рас, НЕ зупинятися після короткого звіту
Користувач прямо попросив продовжувати роботу без зупинок після кожного малого блоку. Порядок лишається: всі офіційні раси/підраси перед Чаклуном/Чарівником20. Не оголошувати всю задачу завершеною після одного пакета.
- CharacterExpandedSpeciesRules + CharacterCreationWizard.ExpandedSpecies.cs (wizard тепер partial) + CreationExpandedSpecies + build-expanded-species.py/expanded-species.tsv. Підключено до загального dropdown, ASI, skills, languages, features/proficiencies, spellbook/free uses, resource recovery, natural attacks.
- Вже реалізовано18нових IDs: tabaxi,kenku,goblin,kalashtar,dhampir,astralelf,bugbear,hobgoblin,changeling,harengon,owlin,leonin,aarakocra,fairy,firbolg,giff,hadozee,plasmoid. Джерела pinned локальні: MPMM,VRGR,AAG,MOT,SCC; Kalashtar ERLW/EFA і Changeling MPMM/EFA відрізняються за редакцією. Жодної вигаданої модернізації traits; у2024старі раси мають позначку джерела/сумісності, ASI від background. У2014post2020рас — окремий вибір +2/+1 або трьох+1; Kalashtar ERLW WIS2CHA1, Leonin MOT CON2STR1.
- Нові поля draft: expandedBonusMode/Increases[3]/Skills[2], expandedCantrip/MagicAbility, expandedTranceProficiency; без автозаповнення. Зміна раси очищує лише залежні поля. Фільтри ASI в CharacterLevelAdvancement копіюють також expandedBonusMode/Increases, щоб не дозволяти >20.
- Навички рас/тимчасові володіння Астрального ельфа, EFAKalashtar, фіксовані мовні Quori/Leonin. Новий secondOriginSkillReplacement для двох одночасних конфліктів race/background; перша стара прив’язка лишається сумісною. BackgroundSkills обирає незалежні заміни, UI/validation/repair оновлені. Профілі навичок не дублюються в notes.
- Расові лічильники створюються в Spels через CreationMagicResources зі stable recovery. PB/longrest: Goblin,Kenku,Dhampir,AstralElf,Harengon,Firbolg,Giff,Hadozee; Hobgoblin два окремихPBпули. Leonin1/short-or-longrest. Fairy/Aarakocra/Firbolg безкоміркові спели використовують наявні FreeSpells записи. Іконок/нових пунктів головного class/race resource dropdown не додавали.
- Природні атаки Tabaxi/Dhampir/Leonin/Aarakocra йдуть у вільні attack rows, overflow75, не замінюють class/weapon attacks. Monk MartialDie й Dex для unarmed claws враховані; Dhampir лишаєтьсяCON. Giff firearm proficiency та AstralElf weapon proficiency підключені до Proficient і видимого поля володінь.
- HarengonPBinitiative без повторного legacy BardhalfPB. Dhampir/Leonin+5walk. Fly/climb/swim та обмеження armor записані в здібностях; основне полеspeed — ходьба. Plasmoid маєтипOoze, EFAKalashtarAberration, Fairy/ChangelingFey.
- Expanded racial spells (legacy traits сумісніз2024) не позначаються випадково якprepared; заговори лишаютьсяon. Власні безкоштовні накладання й можливість використання classslots пояснено. FirbolgDisguiseSelf +3ft exception та AarakocraGustnoMaterial дописані до spellcards.
- Проміжний expanded-species-audit.json для15рас (до останніх Giff/Hadozee/Plasmoid):1785сценаріїв,5355локалізованих відображень,16250checks,errors=[]. Перевірені повні12/потім30створень fighter1безRepository.Save, stats різнихкласів/рівнів, racepools+rest, природніатаки, дубльbackground,ASIcap,Bardinitiative. Останні3ще не пройшли фінальний аудит. Тимчасовий Editor ExpandedSpeciesAudit.cs ПОКИ лишений для завершення пакета; після фінальної перевірки видалити через AssetDatabase, зберегтиJSON.
- Модифікації лишекоду/Resources; sceneUIпозиції не міняли. Після попереднього phonepreview сцена майстра вжеdirty (Canvas/TMP/RectTransforms) — не зберігати/не відкидати навмання.
- Далі: завершити поточний пакет/візуальний прохід, зафіксувати й рухатися далі по офіційних расах. Все ще відсутні більшістьMPMM(Genasi4/Eladrin/Shifter/Gith/etc),AAGAutognome/Thri-kreen,VRGRHexblood/Reborn,FTDdragonborn3,GGRLoxodon/SimicHybrid/Vedalken,Kender/Verdan/Locathah,EFAKhoravar, решта PHB/SCAG/Eberron підрас/варіантів. Сира база race160/subrace98 включає копії, NPC/неперевірені джерела; НЕ відкривати все масово якготове.


## Продовження06.11 —24нові раси, поточний пакет ще завершується
- До попередніх18додано centaur,minotaur,satyr,seaelf,kender,verdan. Стандартні21доSeaElf/Kender/Verdan пройшли аудит2499сценаріїв/7497відображень/22732checks,errors=[]. Останні24й overflowlocalization ще потребують фінального запуску. Не вказувати неперевірені числа якрезультат.
- Кентавр+10walk, Сатир+5walk, MPMMприродні атакиD6 таумовнідії; Сатирдвафіксованіskills+окремийexpandedInstrument зфільтрамикласу/background/originfeat, Bard/Monk dropdownsтакожнепропонуютьйогодубль. ClassTools.includeSpeciesвраховуєSatyrінструмент.
- SeaElfмає2тимчасовіweapon/toolproficienciesPHB, AstralElf1+skill; expandedSecondTranceProficiency. Фільтруютьсявжеіснуючіволодіння SeaElf, зберігаєтьсявибірдоподальшогоручногооновленняпіслявідпочинку. Не створюватиавтосупутників/формдлязмінизовнішності.
- KenderFeatureAbilityвexpandedMagicAbilityалецеНЕspellcasting: окремийUIпідпис, КСProvocationуfeaturesвжеобчислюєтьсязобраноїхарактеристики+PB;неперезаписуєclassspellDC. Двапули:PBTaunt,1Fearless/longrest. Verdan2014Con1Cha2(НЕнавпаки!), Common/Goblin+1, Smallдо5/Mediumз5, racialдвохAdvSaveonlydescription,HitDie1/2rerollописано.
- ЧистийрозрахунокweaponproficiencyвинесенодоDomain/CharacterWeaponProficiency.cs,боDomainasmdefНЕможепосилатисянаUI/CreationStartingWeapons. CreatorProficientлишеделегує. ПопереднякомпіляціяпоказаласамеCS0103цьогопосилання,виправленнявнесенойrecompileзапущений;підтвердитиуспіхпісляreload.
- НоваCreationAttackOverflow.cs:smallmetadata3suffixes+3hashes,знімаєbaselineпісляclasswriters,проектуєлишеextraattacksіншими3моваминаCharacterDataбезRepository.Save. ServiceCapture/Recordперед/післяStartingWeapons+ExpandedSpecies;CardLocalizationfield75оновлюєтількинезміненийзгенерованийтекст,ручніправкиповністюзберігає. ТребаперевіритиновіReportchecks:modernfighternaturaloverflowмаєsnapshot,EnglishбезCyrillic,manualfieldвідхиляється. Перекладименатак+1/+2розділяютьсявідчисловогосуфікса. Підчасстворенняце3тимчасовіCharacterData,нереальніперсонажі.
- build-expanded-species.pyтепергенеруєTemplates,Presentation.FeatureTemplatesїхвключаєдляKenderDCтаіншихформатованихтекстів. SourceManifest26записівна24IDs(Kalashtar/Changelingмаютьпо2редакції). Усіновітекстиуexpanded-species.tsvUA/EN/RU.
- ТимчасовийExpandedSpeciesAudit.csщеіснує(нефінальнаперевірка),видалитипісляуспіху. ЧитатипоточнийconsolegroundTruthа неісторичніlogentries. СценаєдинаCharacterCreationPrototype/edit,доPlayвонабулаdirtyвідпопередньогоpreview—незберігатиїїнавмання.


## 06.11.2026 — продовження рас після повторного «продовжуй»
- Новий завершуваний пакет: deepgnome/duergar, githyanki/githzerai, hexblood/reborn, triton/yuanti, genasi (4 стихії), eladrin/shadarkai. Разом35 нових базових IDs понад core14; Genasi має4 підраси, Eladrin4сезони. Усі дані з локального pinnedcache, без зображень/нових завантажень.
- Genasi й Eladrin вибирають вид у dropdown; переноситься в штатне поле підраси через CharacterSpeciesRules.LineageName. EladrinDC від обраної INT/WIS/CHA+PB, це НЕ classspellDC. Шадар-кай не отримує постійний опір усій шкоді: лише описаний ефект після teleport3+.
- Githyanki1skill+1weapon/tool, Githzeraiбезтакоїнавички. Псіонічніспели мають окремі расовіумови: невидима Магічна рука/безкомпонентів. DeepGnomeNondetectionnoMaterial/Duergarself-only записані у spellcards.
- Hexblood/Rebornустворенні2skill, НЕ безплатне успадкування усіх старих расових traits. HexbloodToken1/LR, RebornPastLifePB/LR. YuantiAnimalFriendshipatwillONLYsnakes, НЕ штучний1/LRpool; Suggestion1/LRз3. Triton1/3/5FogCloud/Gust/WaterWalk. EarthGenasiBonusBladeWardPB/LRокремо від безлімітного звичайного заговору.
- TranceOptions тепер використовує весь PHBtoolcatalog AllTools, не лише MonkTools (додані thieves/nav/gaming/etc). Не видає предмети за саме володіння.
- Фактичний попередній завершений аудит32IDs:3808сценаріїв/11424відображення/34620checks,errors=[]. НовіGenasi/Eladrin/ShadarKai ще потребують остаточного запуску; НЕ приписувати їм попередній результат.
- Переглянуті справжні ScreenCapture PNG: Temp/pipeline-screenshots/gith-dropdown-20261106.png і gith-phone-20261106.png390×844. Playframesрухалися,dropdownscrollbarвидимий, накладаньUIнемає. Це не фізичний телефон/не всі interactionсценарії. Новихперсонажів/Repository.Saveнебуло. TemporaryGameViewpresetвидалено,попереднійindexвідновлено,Playзупинено.
- ExpandedSpeciesAudit.csтимчасовощеє. Після фінальногозапуску прибрати через AssetDatabase,JSONзалишити. Сцену не зберігати/відкидати навмання: вона булаdirty ще до цього ходу.
- Далі досі потрібні: Kobold/Shifter/MPMMlegacyOrc/Aasimar/Goliath, natural-armor races (Lizardfolk/Tortle/Locathah/Autognome/Thri-kreen/Loxodon), GGRSimicHybrid/Vedalken, FTDdragonborn3,EFAKhoravar, PHB/SCAG/Eberronvariants/dragonmarks. Не вважати всі160rawentriesготовими. Потім Warlock/Wizard20,імена,levelup,мультикласвкінці,UAENRUаудит.


### Продовження06.11: Кобольд і Звіролюд, фільтр підготовки
- Додано KoboldMPMM: expandedGift=craft/defiance/magic, виключний вибір, craft1з5skill; magic1SorcererCantrip+INT/WIS/CHA, UI і Serviceперевіряють classcatalog. DraconicCryPB/LR. SetSpeciesочищуєgift; Repairнепереноситьчужіtraits.
- Виправлено SpellPickers: modernexpandedracialleveledspell можна окремо обрати й підготувати класом. Старий фільтр помилково вилучав їх як PHB2024automaticprepared; expandedraces не дають автоматичногоpreparedвідлишеinnateособливості. Cantripдублі й coremodernautomaticfiltersзбережені.
- ShifterMPMM2014/EFA2024: Medium2014,Small/Medium2024;4різновиди speciesLineage,1вибірskill[Acrobatics,Athletics,Intimidation,Survival],PB/LRshifting. БазовіAC/speedнеотримуютьумовнібонуси. Longtoothзаписуєnaturalattack«Ікла(перевтілення)»,overflowіUAENRUлокалізуютьсязагальнимwriter. THPнеініціалізуютьсябезактивації.
- Тепер37новихspeciesIDs (Genasi4стихії,Eladrin4сезони,Shifter4типи). ОстаннійПІДТВЕРДЖЕНИЙзвітДОShifter:4284сценарії/12852renderings/39182checks,errors=[]. Shifterфінальнаперевіркавиконуєтьсяокремо; попереднійcallbackнеоновивJSONчерезнезаповненийfixturefighter5subclass/ASI,нечерезпродуктовийбаг. FixtureBuildпереведенийнаlevel1,statsна5лишаються.
- ЗагальнийпланНЕзавершений. Не починатиWarlock/Wizardдоофіційнихрас. TempEditorAuditприбратипісляпоточногопідсумковогопроходу;неплутатистарийJSONзновим. HP57.


### Підсумок поточного пакета06.11 (після37новихIDs)
Фінальний expanded-species-audit.json ОНОВЛЕНИЙ:4403scenarios/13209UAENRUrenderings/40311checks,errors=[]. Включає4Genasiстихії,3Koboldподарунки,4Shifterтипи, source/class/levelsampling, повніствореннябезSave, resource/recovery і attackoverflow/manualtextpreservation. Це не повний UIпрохід кожного варіанта й не фізичнийтелефон. Unitycompilepassed. Тимчасовий ExpandedSpeciesAudit.cs/.meta видалено AssetDatabase; JSONзалишено. Playзупинений,GameViewpresetвідновлено,відкрита тільки CharacterCreationPrototype. Попередніручнісейви/sceneassetsнеперезаписані. Далі НЕ повторювати вже зроблені37рас; звіряти CharacterExpandedSpeciesRules.Ids і SourceManifest. Shifter/Kobold/Genasi/Eladrin/ShadarKai вже реалізовані, старий перелікmissingвище застарів щодо них. Досі лишаються інші офіційні раси/варіанти (насамперед naturalarmor і FTD/GGR/Eberron), потім решта плану.


## 06.11 — наступний пакет, природний обладунок (активна робота)
- Додано6IDs: lizardfolk/tortleMPMM,locathahLR,loxodonGGR,autognome/thrikreenAAG; тепер43новихIDs понадcore14. Джерела зpinnedraces.json,бездодатковихзавантажень.
- CharacterNaturalArmor окремо застосовує альтернативні формулипіслявсіхclass/species/feat/levelstats. MathMaxформул,неїхсума;Lizard13DEX/Loc12DEX/Loxo12CONдопускаютькращийnaturalACприодягненомуarmor. Auto/Thri13DEXлишеunarmored;Tortle17безDEXнеможеноситиarmor. Щит/Defense/protection/armorinfusionsвідокремлені. ПрироднийACO3 не означаєдодатковийбонус+13.
- expandedUnarmored(bool)необов’язково знімає стартовийarmor,майнозалишаєтьсявінвентарі. Tortleзавждиненоситьarmor. UIнаEquipmentPage,черезнаявнийDropdownInt;поясненняUAENRU. Знятийheavyнештрафуєspeed;Monk/Barbarian/Draconic/DanceальтернативиНЕскладаютьсязnatural.
- ArtificerArmor/armor.weapontargetsприNoArmorнедоступні;CreationArtificerResourcesнепишеArcaneArmorweapon/poolsбезодягненогоarmor;infiltrator+5непостійнийбезArmor. РештурасіclasscalculationsнечіпаєWearsArmorhelper(длянеActiveповертаєtrueзадлястароїповедінки).
- Liz2skills/Tort1зsourceсписку;LocAthletics+PerceptionйфіксованаAquan;LoxoфіксованаLoxodonмова. LoclegacySTR2DEX1,LoxCON2WIS1безгнучкихASI;modernbonusesbackground. Auto2permanenttoolsновийexpandedTools[2],ClassToolsincludeSpeciesіUI/validation/repairпідключено;неплутатизтимчасовимтрансом.
- LizBite6slashing/TortClaws6naturalattacks+overflow;PB/LRpoolsLizHungryJaws/AutoBuiltForSuccess. MendingнестаєвідомимспеломAutognome;описуєлишеотриманнялікування. ShellDefense4AC/ThriCamouflage/Smell/Leviathanлишеправильніумовніописи,небезумовнібонуси.
- ТимчасовийNaturalSpeciesAudit.csпокиіснує;запущенопакетнийаудитпіслязавершенняшестирас. Reporttools/character-data/natural-species-audit.json;требапрочитатирезультат,неоголошуватизаздалегідьуспіх. ПісляфіналувидалитиEditor.cs/metaAssetDatabase,JSONзалишити. Попереднійexpanded-species-audit4403...лишеперші37,не43.
- Сценаprototypeбулаdirtyдоцьогоходу;незберігати/відкидативручну. Немаєновихкористувацькихперсонажів/Save. Далііншіофіційніраси(FTD,GGR,EFA,legacyvariants),потімWarlock/Wizard20тарештаплану.

Уточнення користувача06.11: українська назва — **Асімар** (не Аазімар/Аасімар); внутрішні IDs aasimar/aasimarlegacy не змінювати.


## 06.11 — активний стан після природного обладунку, НЕ завершення всіх рас
- Тепер53новихbaseIDs у CharacterExpandedSpeciesRules.Ids. Післяпершого37пакета доданоlizardfolk,tortle,locathah,loxodon,autognome,thrikreen,vedalken,simic,dragonbornftd,khoravar,orclegacy,aasimarlegacy,goliathlegacy,lupin,lorwynchangeling,rimekin. Genasi4/Eladrin4/Shifter4/FTD3×5предків — залежніваріанти,неокреміbaseIDs.
- Всі30базовихMPMM і6AAG тепер представлені актуальними traits; стараMPMMверсіяOrc/Aasimar/Goliath окрема відXPHB, SourceName(MPMM),і2014доступна. АсімарMPPM3+обираєОДНЕодкровення,LightfixedCHA(неcachedadditionalSpellsabilitychoose!),healingPBd4. OrcPBAdrenalineLR+1RelentlessLR;Goliath30walkAthleticscoldresistStoneEndurancePB/LR. Не підмінятиtraits2024.
- FTD: CharacterFtdDragonbornRules,одинIDdragonbornftd,Familieschromatic/metallic/gem,ancestor0..4відсвоєїтаблиці,15локалізованихheaders. BreathNativeinput47:1/2/3/4d10пороги1/5/11/17,NativecounterPB/LRвобохредакціях,неSR. Mainresource8показуєтьсяодразу. Extras5+по1/LRуSpels:ChromaticWardingAction1minimmune/GemFlightBonus1minhover/MetallicBreathспільнийпулдлядвохрежимів. НемаєPHB2024default10minflight.
- KhoravarEFAлише2024:CoreAvailableexcludesRevisedOnlyidsдля2014. DefaultFriends;можнаобратиcantripCleric/Druid/WizardпісляLR,INT/WIS/CHAпотрібновибрати. expandedGift=skill/toolрозділяє1skillабо1tool;підключенітіжexpandedTools. Lethargy1d4longrests:CreationMagicResources.Addмаєновийнеобов’язковийautomaticRecovery=true;Khoravarпередаєfalse,томуcheckboxсамнескидаєтьсякожногоLR. Срок1d4іручнезняттяпозначкипояснені,скрипттаймерневеде.
- Офіційність LFL/RHW перевірена НАПРЯМУ уDNDstaff: https://www.dndbeyond.com/posts/2069-add-a-delightful-twist-to-forgotten-realms (27.09.2025,2новіspecies+8адаптацій), https://www.dndbeyond.com/posts/2169-everything-you-need-to-know-about-ravenloft-the (16.05.2026,Lupinновийspecies). Не відкидатиRHW/LFLякhomebrew. Lupin/LorwynChangeling/Rimekinвпроваджуються зараз ілише2024 черезRevisedOnly;PhaseShift/NPC/UAнеімпортуватиавтоматично.
- Lupin:unarmedSlashingBASE1+STR,нештучніd6;Monk/FighterUnarmed/Tavernможутьпіднятикубик. Damage+Shoveраззахід;HowlPB/LR15ftWISsaveCONdc,провалDisadvantageattacks+savesдопочаткунаступногоходу. Nativeattackwriterцеобробляє.
- LorwynChangeling:fey,120DV,chooseDeception/Performance,ShapeSelfHumanoid/BeastshapeБЕЗbeaststatblock/автоформ;InitiativefreehalfSpeedmovementбезініціативногобонусу.
- Rimekin:RayFrost/IceKnife3/FlameBlade5+free1each/LR,ALWAYSPREPARED;новийCreationSpellPreparation.RacialPreparedhelper. SpellPickersнепропонуютьавтопідготовленіспелиповторноякclasschoice. ColdBlade racialexceptionпоказаназверхуопису,canonicalFlameBladeзберігаєтьсядляіншогоджерела. УсіновітекстиUAENRUвexpanded-species.tsvіbuilder.
- ПІДТВЕРДЖЕНОостаннійзвітnatural-species-audit.jsonДОостанніх3modernIDs:1756scenarios/5268renderings/16265checks,errors=[]. Включаєпопередні8+FTD15предків/5рівнів/2редакції,Khoravarобидвавибори/manualrest,MPMM3. Lupin/Lorwyn/RimekinщеНЕскомпільованіпісляостанньоїзміни/НЕаудитовані. Не приписуватинеперевіреномууспіх.
- ТимчасовийAssets/Skripts/Editor/NaturalSpeciesAudit.csщеє,післяпакетногоуспіхувидалитиAssetDatabase. СтарийExpandedSpeciesAudit.csвидалений. Editorлишенаprototype/edit;scenewasdirtyдоходу,неSave/discardнавмання. НеCreateRepository.Save/користувацькихперсонажів.
- Далі:довестипоточні3,візуальнийпрохідновихконтролівPC/phoneіcleanup. Залишкиофіційних:RHWперевиданняDhampir/Hexblood/Reborn(іншіпорівнянозVRGR);інші6LFLадаптацій;GrungOGA(требапідтвердитивидавця),CustomLineageTCE;PHB/SCAG/Eberrondragonmark/іншіопублікованіваріанти. Нестверджувативсірасиготові. ПотімWarlock/Wizard20,імена,levelup,multiclasslast,localeaudit.

06.11 steering: stop adding races for now, proceed Warlock 1–20 then Wizard. 53 expanded species IDs retained; natural audit final1966/5898/18169 errors[]. Grung NOT implemented. Use Асімар. Current work Warlock full progression; temporary NaturalSpeciesAudit still needs AssetDatabase cleanup. No new player saves.

07.11: Warlock 1–20 enabled in both editions. 248 generated builds, 744 localized renderings, 10882 checks passed in warlock-progress-audit.json. Separate saved-state check: short rest clears pact slots and preserves spent Arcanum; long rest clears both. WarlockProgressAudit removed. Ten native invocation rows now in Spels, visually unverified. Ready existing summons supported; special Chain familiars remain manual. Wizard core book, preparation and slots progression integrated, currently selectable only at level 1 pending subclasses and higher features. Unity compiles and only CharacterCreationPrototype is open. Race expansion remains paused at user's direction.

07.11: Wizard 1–20 and name generator implemented. Wizard: 8 classic / 4 revised schools, savant book choices, Scholar, mastery, signature spells, short/long rest resource bindings and existing summon templates. 96 in-memory builds passed; name roll 192 selections across UA/EN/RU and both categories. No Play Mode visual check. Active scene CharacterCreationPrototype. Next: post-creation level up using existing draft and atomic collection import, preserving identity and manual sheet state; multiclass afterward. Race scope remains paused.

07.11: кнопка підвищення рівня в шапці cartaPersonaj праворуч від підраси, під світоглядом. Відкриває майстер для наявного персонажа з +1 рівнем, бере чернетку з CreationWizard_v1, застосовує зміни до копії колекції й атомарно зберігає той самий ID. Вручну змінені поля зберігаються, а конфлікт переміщення відредагованої картки спела чи ресурсу блокує збереження. Перевірено на копії ОЗ, тимчасові ОЗ, нотатку та перенесення витраченої комірки договору. Форми залишаються в наявному стані, їхній перерахунок ще не підключено. Мультиклас після завершення цього етапу.

13.11: після завершення підвищення рівня наступна запланована функція — мультиклас. Актуальна черга в кінці CHARACTER_CREATION_EXPANSION.md.

### Computer Use

За повідомленням користувача, в іншому чаті зробили патч для Computer Use на цьому ПК, який виправив захоплення екрана. За потреби використовувати Computer Use для візуальних тестів і потрібних кліків в інтерфейсі Unity. Раніше без патча екран під час таких перевірок був недоступний.
