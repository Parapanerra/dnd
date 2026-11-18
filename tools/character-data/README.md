# Локальний довідник для генератора персонажів

`fetch-reference.ps1` завантажує лише JSON для створення персонажа з
[`5etools-src`](https://github.com/5etools-mirror-3/5etools-src) на зафіксованому
коміті `8790755429c346d20020eed7826d312d484c393f` (v2.39.0).
Завантажені файли лежать у `cache/` поза Unity `Assets` і не додаються до Git.
Список, розміри й SHA-256 записуються в `cache/manifest.json`.

Для повторного завантаження:

```powershell
./tools/character-data/fetch-reference.ps1
```

У наборі є файли 13 класів із рівнями й підкласами, раси/види,
передісторії, риси, класові варіанти (зокрема інфузії артифайсера),
навички, мови, заклинання PHB/XPHB/TCE/EFA та `spells/sources.json`
для прив'язки заклинань до класів. Каталоги зображень, істот і предметів
не завантажуються. Blood Hunter відсутній у списку класів цього джерела.

Сирий `cache/` не входить у збірку гри. Метадані 13 класів уже перенесено до
`CharacterClassCatalog`, але вибираються лише класи з реалізованими правилами.
Індекс заклинань 0–1 рівнів скопійовано у Unity Resources і читається через
`CharacterSpellCatalog`. Вибір самих заклинань, переклади й запис у книгу
заклинань ще належить підключити до майстра.

`build-class-index.ps1` створює `class-index.json` із 26 записами (13 класів
у двох редакціях): кістка ОЗ, ряткидки, навички, магічна характеристика та
назви можливостей 1 рівня. Це робочий індекс для реалізації; самі записи ще
не відкривають вибір класу в майстрі, доки правила й перенесення не готові.

`build-spell-index.ps1` створює `spell-index-level-0-1.json` для заговорів і
заклинань 1 рівня з PHB/XPHB. Індекс містить 187 записів та прив'язку до
класів. Заклинання артифісера мають окреме джерело й сюди ще не входять.

## Перевірений вміст цього знімка

- 13 файлів класів: у кожному є запис `edition: classic` і `edition: one`;
  там само лежать `classFeature`, `subclass` і `subclassFeature`.
- `backgrounds.json`, `races.json`, `feats.json`: походження, раси/види й риси.
- `optionalfeatures.json`: додаткові можливості класів, серед них 16 записів
  з типом `AI` для інфузій артифайсера.
- `spells-phb.json`: 361 заклинання; `spells-xphb.json`: 391;
  `spells-tce.json`: 21; `spells-efa.json`: 1.
- `spells/sources.json` прив'язує заклинання до класу і джерела класу.

Зафіксоване джерело: `5etools-src` v2.39.0, коміт
`8790755429c346d20020eed7826d312d484c393f`. Локальний набір займає
приблизно 5,4 МБ. Усі JSON-файли пройшли перевірку синтаксису при завантаженні.

## Для наступного етапу

1. Будувати власний каталог із ключами `edition + source + name`, щоб не
   плутати однойменні можливості різних редакцій.
2. Спершу підключити класові рівні й вибори до наявного `CharacterCreationDraft`.
   Не вставляти сирі описи в нотатки картки замість механіки.
3. Потім підключити передісторії, види, риси й зв'язки заклинань; окремо
   зіставити їх з українською, англійською та російською локалізацією.
4. Інфузії в джерелі є як `optionalfeature` типу `AI`; для карти розділити
   вивчені/підготовлені та фактично активні предмети/можливості.
5. Blood Hunter у цьому наборі немає. Користувач надав окремий PDF:
   `C:/Users/Tom/Desktop/DnD/Кровавый охотник [Blood hunter] _ Классы D&D 5 _ Homebrew, Matt Mercer _ Critical Role.pdf`.
   Це збережена сторінка DnD.su від 27.01.2024, у ній є таблиця рівнів,
   класові вміння, ордени та прокляття крові. Джерело перевірено візуально;
   PDF не копіювали до проєкту. SHA-256:
   `3670F6A5FDCBFB7B295FE8938C57DA2DB117AC4DB6C807091E68AE5D118AEB78`.
   Для редакції 2024 це не окреме оновлення Blood Hunter, а домашній клас,
   який треба зіставити з правилами обраної редакції.
6. Якщо пізніше знадобляться додаткові книги заклинань, завантажити
   лише відповідні `spells-*.json` за потреби.


## Описові виправлення 04.11.2026
`build-description-fixes.py` + `description-fixes.tsv`: UA/EN/RU виклади здібностей, умови бойових стилів і восьми властивостей майстерності зброї. Зіставлено з офіційними Basic Rules 2024: [equipment](https://www.dndbeyond.com/sources/dnd/br-2024/equipment/), [feats](https://www.dndbeyond.com/sources/dnd/br-2024/feats), [glossary](https://www.dndbeyond.com/sources/dnd/br-2024/rules-glossary/).
`fetch-artificer-item-reference.py` залишає тільки записи, на які посилаються вже реалізовані плани Винахідника, з того самого pinned commit; повний каталог предметів і зображення не зберігаються. `build-artificer-item-effects.py` перевіряє покриття іменованих планів та генерує локалізовані короткі пояснення з двох TSV. Редакційні зміни тримаються окремо; наявність вивченого плану не означає активне спорядження.
`description-routing-audit.json` — завершений описовий прохід: 5136 відображень, 61898 перевірок абзаців,357 перекладів планів; без втрат, перевищення місткості й знайдених мовних залишків. Перевірено захист ручної нотатки при оновленні згенерованих текстів. Це не підтвердження кожного правила або візуалу на телефоні.


## 06.11.2026 — core weapons / preparation audit
`build-core-weapons.py` retains only 75 PHB/XPHB weapon metadata entries in `cache/core-weapons.json` (pinned source + SHA). It generates CharacterWeaponCatalog; no images or full item download. `species-recording-audit.json`:3198 scenarios,9594 localized renderings,33 equipment,397weapon,30spell checks; errors/untranslated empty. This is data/recording verification, not complete real-device UI testing. Preparation policy separates always-prepared/selected spells from feature-only casting. Temporary Editor audit removed after completion.
