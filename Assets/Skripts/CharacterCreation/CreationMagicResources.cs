using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationMagicResources
    {
        internal const string WideRowsKey = "CreationPreferWideMagicRows";

        internal static bool IsResourceName(CreationSheetBindings.Entry entry) =>
            entry.scene == "Spels" && entry.key.EndsWith(".name", StringComparison.Ordinal) &&
            (entry.key.StartsWith("magic.resource.", StringComparison.Ordinal) ||
             entry.key.StartsWith("magic.large.resource.", StringComparison.Ordinal));

        private static readonly (string resource, string ability)[] ResourceTitles =
        {
            ("Моторошний удар", "Моторошний нападник"),
            ("Виклик феї — без комірки", "Підкріплення фей"),
            ("Туманний крок — без комірки", "Туманний мандрівник"),
            ("Виклик дракона — без комірки", "Драконячий супутник"),
            ("Божественна кара — без комірки", "Кара паладина"),
            ("Поклик скакуна", "Вірний скакун"),
            ("Славний захист", "Славетний захист"),
            ("Телекінез — безкоштовне накладання", "Майстер телекінезу"),
            ("Гібридна форма", "Гібрид"),
            ("Створення мутагенів", "Мутагени"),
            ("Ігнорувати побічну дію", "Дивний метаболізм"),
            ("Посилення пожирання душі", "Прокляття пожирання душі"),
            ("Очки везіння", "Щасливець"),
            ("Остання стійкість", "Дар відновлення"),
            ("Кубики відновлення 1–5", "Дар відновлення"),
            ("Кубики відновлення 6–10", "Дар відновлення"),
            ("Посилення укусу", "Вампірський укус"),
            ("Камуфляж гнома", "Камуфляж свірфнебліна"),
            ("Земля: швидкий захист", "Земний ґеназі"),
            ("Підводне дихання", "Часткова амфібія"),
            ("Захист від непритомності", "Стійкість до непритомності"),
            ("Хроматичний захист", "Хроматичний захист із 5 рівня"),
            ("Металеве дихання", "Металеве дихання із 5 рівня"),
            ("Самоцвітний політ", "Самоцвітний політ із 5 рівня")
        };

        internal static string Add(CharacterData character, CreationSheetBindings bindings, string name, int uses, bool shortRest, bool automaticRecovery = true, bool useLastRow = false)
        {
            if (uses > 8) throw new InvalidOperationException("Split resource pools larger than eight into labelled rows.");
            var data = character.GetSceneData("Spels");
            bool preferWideRows = character.GetSharedString(WideRowsKey) == "1";
            for (int slot = 0; slot < 16; slot++)
            {
                // Saved characters retain the allocation order used to generate their resource rows.
                int row = useLastRow ? 15 - slot : slot;
                if (useLastRow && preferWideRows) row = (row + 8) % 16;
                if (row < 8 && uses > 5) continue;
                string prefix = row < 8 ? "magic.resource." + row : "magic.large.resource." + (row - 8);
                var field = bindings.Entries.FirstOrDefault(entry => entry.key == prefix + ".name");
                if (field == null) continue;
                string existing = data.GetString("StableField_v1_" + field.stableId, field.index < data.inputData.Count ? data.inputData[field.index] : "");
                if (!string.IsNullOrWhiteSpace(existing)) continue;
                bindings.Text(character, prefix + ".name", name);
                bindings.Dropdown(character, prefix + ".count", uses);
                string restKey = row < 8 ? "rest.magic." + row : "rest.magic.large." + (row - 8);
                bindings.Toggle(character, restKey, false);
                if(automaticRecovery)CreationResourceRecovery.Register(character, bindings, restKey, shortRest);
                return restKey;
            }
            throw new InvalidOperationException("No free magic resource rows.");
        }

        internal static string[] NoteTexts(CharacterData character, CreationSheetBindings bindings,
            CharacterCreationDraft draft, string features, Func<string, string> translate,
            params Func<string, string>[] resourceLanguages)
        {
            var state = character.GetSceneData("Spels", false);
            var labels = resourceLanguages.Length > 0 ? resourceLanguages : new[] { translate };
            bool Matches(string visible, string source) => labels.Any(label => visible == label(source));
            var paragraphs = CreationDescriptionLayout.Paragraphs(features).Where(paragraph => Title(paragraph).Length > 0).Select(paragraph =>
            {
                string heading = Title(paragraph);
                return (source: paragraph, title: heading, localizedTitles: labels.SelectMany(label =>
                    new[] { label(heading), Title(CharacterCreationPresentation.FeatureLines(paragraph, label)) }).ToArray());
            }).ToArray();
            var secondary = draft.secondaryClass?.IsSelected == true ? draft.secondaryClass.ClassDraft(draft) : null;
            CharacterCreationDraft Track(string id) => draft.characterClass == id ? draft :
                secondary?.characterClass == id ? secondary : null;
            var artificer = Track("artificer");
            var warlock = Track("warlock");
            var wizard = Track("wizard");
            var fighter = Track("fighter");
            var rogue = Track("rogue");
            var druid = Track("druid");
            var cleric = Track("cleric");
            var blood = Track("bloodhunter");
            var bard = Track("bard");
            var ranger = Track("ranger");

            var entries = new System.Collections.Generic.List<string>();
            if (artificer != null)
                entries.AddRange(CreationArtificerResources.InfusionDescriptions(artificer, translate));
            if (warlock != null)
                entries.AddRange(warlock.warlockInvocations.Where(key => key.Length > 0)
                    .Select(key => CharacterCreationPresentation.FeatureLines(CharacterWarlockInvocations.Description(warlock, key), translate)));
            if (state == null) return CreationDescriptionLayout.Pack(entries, 4);

            var resources = bindings.Entries.Where(IsResourceName)
                .Select(field => (field, name: state.GetString("StableField_v1_" + field.stableId,
                    field.index >= 0 && field.index < state.inputData.Count ? state.inputData[field.index] : "")));
            foreach (var (field, name) in resources.Where(resource => !string.IsNullOrWhiteSpace(resource.name)))
            {
                if (secondary != null && (secondary.characterClass == "cleric" || secondary.characterClass == "paladin") &&
                    labels.Any(label => name == CreationMulticlassResources.ChannelName(secondary, label)))
                {
                    entries.Add(ChannelDescription(secondary, translate));
                    continue;
                }
                bool MatchesNumbered(string template, params object[] values) =>
                    labels.Any(label => name == string.Format(label(template), values));
                string spell = null;
                string spellUsage = null;
                bool spellUsageIncludesName = false;
                string title = System.Text.RegularExpressions.Regex.Replace(
                    name.Split(new[] { " — " }, StringSplitOptions.None)[0], @" (?:1?[кd]\d+.*|\(\d+\))$", "");
                foreach (var alias in ResourceTitles)
                    if (Matches(name, alias.resource) || Matches(title, alias.resource)) title = alias.ability;
                if (fighter != null && MatchesNumbered(CharacterFighterText.Get("ui.superiority"), CharacterFighterRules.SuperiorityDie(fighter)))
                    title = "Бойова перевага";
                for (int i = 0; warlock != null && i < warlock.warlockArcana.Count; i++)
                    if (MatchesNumbered(CharacterWarlockRules.Text("pool.arcanum"), i + 6))
                    {
                        spell = warlock.warlockArcana[i];
                        spellUsage = translate(CharacterWarlockRules.Text("arcanum.cast"));
                    }
                for (int i = 0; wizard != null && i < wizard.wizardSignature.Count; i++)
                    if (MatchesNumbered(CharacterWizardText.Get("signature.pool"), i + 1))
                    {
                        spell = wizard.wizardSignature[i];
                        spellUsage = translate(CharacterWizardText.Get("signature.card"));
                    }
                if (wizard != null && CharacterWizardRules.Modern(wizard) && CharacterWizardRules.Is(wizard, "illusion", 6))
                    foreach (string summon in new[] { "Summon Beast", "Summon Fey" })
                        if (Matches(name, summon))
                        {
                            spell = summon;
                            spellUsage = CreationWizardMagic.CardNote(wizard, summon, translate);
                        }
                string[] relatedTitles = Array.Empty<string>();
                if (bard != null && MatchesNumbered(CharacterBardText.Get("ui.inspiration"), CharacterBardRules.Die(bard)))
                {
                    title = "Натхнення барда";
                    relatedTitles = new[] { "Джерело натхнення" };
                }
                if (draft.secondaryClass?.classId == "fighter" && Matches(name, "Друге дихання"))
                    relatedTitles = new[] { "Тактичний розум", "Тактичне переміщення" };
                if (draft.secondaryClass?.classId == "barbarian" && Matches(name, "Лють"))
                    relatedTitles = new[] { "Під час Люті", "Лють 2014", "Лють 2024" };
                if (draft.species == "orclegacy" && Matches(name, "Приплив адреналіну")) title = "Приплив адреналіну MPMM";
                if (draft.species == "goliathlegacy" && Matches(name, "Кам’яна витривалість")) title = "Кам’яна витривалість MPMM";
                if (draft.species == "aasimarlegacy")
                {
                    if (Matches(name, "Цілющі руки")) title = "Цілющі руки MPMM";
                    if (Matches(name, "Небесне одкровення"))
                    {
                        title = "Небесне одкровення MPMM";
                        relatedTitles = new[] { "Промениста душа", "Променисте спалення", "Некротичний покров" };
                    }
                }
                if (draft.species == "simic" && Matches(name, "Кислотний плювок")) relatedTitles = new[] { "Плювок зараз" };
                if (draft.species == "shadarkai" && Matches(name, "Благословення Королеви Воронів"))
                    relatedTitles = new[] { "Примарний захист із 3 рівня" };
                bool psiWarrior = fighter != null && CharacterFighterRules.Is(fighter, "psiwarrior");
                if (psiWarrior || rogue != null && CharacterRogueRules.Is(rogue, "soulknife"))
                {
                    int pool = psiWarrior ? CharacterFighterRules.PsiCount(fighter) : CharacterRogueRules.PsiDice(rogue);
                    int die = psiWarrior ? CharacterFighterRules.PsiDie(fighter) : CharacterRogueRules.PsiFaces(rogue);
                    for (int offset = 0; offset < pool; offset += 6)
                        if (MatchesNumbered("Псіонічні кістки к{0}: {1}–{2}", die, offset + 1, Math.Min(offset + 6, pool)))
                        {
                            title = psiWarrior ? "Псіонічна енергія" : "Псіонічна вправність";
                            relatedTitles = psiWarrior
                                ? new[] { "Захисне поле", "Псіонічний удар", "Телекінетичний поштовх", "Захищений розум" }
                                : new[] { "Психічний шепіт", "Клинки душі" };
                            break;
                        }
                }
                if (druid != null)
                {
                    if (Matches(name, CharacterDruidText.Get("pool.free"))) title = "Природне відновлення";
                    if (Matches(name, CharacterDruidText.Get("pool.resurgence"))) title = "Відродження дикості";
                    if (Matches(name, CharacterDruidText.Get("pool.arch"))) title = "Архідруїд";
                    if (Matches(name, CharacterDruidText.Get("pool.bolt"))) title = "Зоряна карта";
                }
                if (cleric != null)
                {
                    if (Matches(name, CharacterClericText.Get("pool.intervention"))) title = "Божественне втручання";
                    if (Matches(name, CharacterClericText.Get("pool.visions"))) title = "Видіння минулого";
                    if (Matches(name, CharacterClericText.Get("pool.flare"))) relatedTitles = new[] { "Поліпшений спалах" };
                }
                var matching = paragraphs.Where(p => relatedTitles.Contains(p.title) || title.Equals(p.title, StringComparison.OrdinalIgnoreCase) ||
                    p.title.StartsWith(title + " (", StringComparison.OrdinalIgnoreCase) ||
                    p.localizedTitles.Any(localized => localized.Equals(title, StringComparison.OrdinalIgnoreCase) ||
                        localized.StartsWith(title + " (", StringComparison.OrdinalIgnoreCase))).Select(p => p.source);
                bool freeLandSpell = druid != null && Matches(name, CharacterDruidText.Get("pool.free"));
                if (freeLandSpell || druid != null && Matches(name, CharacterDruidText.Get("pool.recovery")))
                {
                    string freeCast = CharacterDruidText.Get("land.free");
                    matching = matching.Where(p => (p == freeCast) == freeLandSpell);
                }
                string[] matchedDescriptions = matching.ToArray();
                string description = matchedDescriptions.Length > 0
                    ? CharacterCreationPresentation.FeatureLines(string.Join("\n", matchedDescriptions), translate) : null;
                string pactRow = character.GetSharedString("CreationPactResourceRow_v1");
                if (pactRow.Length > 0 && field.key.Replace("magic.large.resource.", "rest.magic.large.")
                        .Replace("magic.resource.", "rest.magic.") == pactRow + ".name")
                    description = name + "\n" + translate("Використання комірок договору відновлюються після короткого або довгого відпочинку.");
                if (freeLandSpell && description != null)
                {
                    int separator = description.IndexOf(':');
                    if (separator >= 0) description = translate(CharacterDruidText.Get("pool.free")) + description.Substring(separator);
                }
                if (blood != null)
                {
                    if (description != null && Matches(name, CharacterBloodHunterRules.Text("pool.step")))
                        description += "\n" + string.Format(translate("Використань за короткий або довгий відпочинок: {0}."), blood.level >= 15 ? 2 : 1);
                    int secretIndex = Matches(name, CharacterBloodHunterRules.Text("pool.revealed")) ? 0
                        : Matches(name, CharacterBloodHunterRules.Text("pool.unsealed")) ? 1 : -1;
                    if (secretIndex >= 0)
                    {
                        spell = CharacterBloodHunterRules.AutomaticSpells(blood).ElementAtOrDefault(secretIndex);
                        if (!string.IsNullOrEmpty(spell))
                        {
                            spellUsage = string.Format(translate(CharacterBloodHunterRules.Text(secretIndex == 0 ? "profane.revealed" : "profane.unsealed")), translate(spell));
                            spellUsageIncludesName = true;
                        }
                    }
                }
                if (ranger != null && CharacterRangerRules.MarkUses(ranger) > 0 && Matches(name, "Hunter's Mark"))
                {
                    spell = "Hunter's Mark";
                    spellUsage = string.Format(translate("Накладань без комірки за довгий відпочинок: {0}."), CharacterRangerRules.MarkUses(ranger));
                }
                if (artificer != null)
                    description = ArtificerDescription(artificer, name, Matches, translate) ?? description;
                if (spell != null || description == null)
                {
                    if (spell == null)
                    {
                        spell = CharacterSpeciesRules.FreeSpells(draft).FirstOrDefault(candidate => Matches(name, candidate));
                        spell ??= CharacterOriginFeatRules.Choices(draft).Where(CharacterOriginFeatRules.Magic)
                            .Select(feat => feat.spell).FirstOrDefault(candidate => labels.Any(label =>
                                name == string.Format(label("Риса: {0}"), label(candidate))));
                        if (spell != null)
                        {
                            var count = bindings.Entries.FirstOrDefault(entry => entry.key == field.key.Replace(".name", ".count"));
                            int uses = count == null ? 1 : state.GetInt("StableField_v1_" + count.stableId,
                                count.index >= 0 && count.index < state.dropdownData.Count ? state.dropdownData[count.index] : 1);
                            spellUsage = string.Format(translate("Накладань без комірки за довгий відпочинок: {0}."), uses);
                        }
                    }
                    if (!string.IsNullOrEmpty(spell))
                    {
                        string effect = CharacterSpellCatalog.CardDescription(draft.edition, spell, draft.characterClass, translate, casterLevel: draft.level);
                        if (effect.Length > 0)
                            description = (spellUsageIncludesName ? "" : translate(spell) + "\n") + spellUsage + "\n" + effect;
                    }
                }
                if (description != null && !entries.Contains(description)) entries.Add(description);
            }
            return CreationDescriptionLayout.Pack(entries, 4);
        }

        private static string Title(string paragraph)
        {
            int colon = paragraph.IndexOf(':');
            return colon < 0 ? "" : paragraph.Substring(0, colon);
        }

        private static string ChannelDescription(CharacterCreationDraft track, Func<string, string> translate)
        {
            bool cleric = track.characterClass == "cleric";
            string features = cleric ? CharacterClericRules.Features(track) : CharacterPaladinRules.Features(track);
            var effects = features.Split('\n').Where(line =>
                Title(line) == "Божественний канал" ||
                (cleric ? Title(line).EndsWith(" (канал)", StringComparison.Ordinal) :
                    line.Contains("витрать канал") || line.Contains("витрать Божественний канал")));
            string description = CharacterCreationPresentation.FeatureLines(string.Join("\n", effects), translate);
            return CreationMulticlassResources.ChannelName(track, translate) + "\n" + description;
        }

        internal static string NoteStamp(CharacterCreationDraft draft, int index)
            => (CharacterArtificerRules.Active(draft) ? "CreationInfusionNote_v1_" : "CreationMagicNote_v1_") + index;

        private static string ArtificerDescription(CharacterCreationDraft draft, string name, Func<string, string, bool> matches, Func<string, string> translate)
        {
            string edition = CharacterArtificerRules.Revised(draft) ? "24" : "14";
            var descriptions = new (string resource, string feature)[]
            {
                ("genius", "genius" + edition),
                ("store", "store" + edition), ("store.extra", "store" + edition),
                ("cannon", "artillerist.3." + edition), ("jolt", "battlesmith.9." + edition),
                ("elixirs", "alchemist.3." + edition), ("restoration", "alchemist.9." + edition),
                ("cauldron", "alchemist.15.24"), ("greater", "alchemist.15.14"), ("heal", "alchemist.15.14"),
                ("faerie", "cartographer.illuminated"), ("path", "cartographer.15.24"),
                ("giant", "model.dreadnaught.24"), ("field", "model.guardian.14"),
                ("pull", "armorer.15." + edition), ("flight", "armorer.15.24")
            };
            foreach (var entry in descriptions)
                if (matches(name, CharacterArtificerText.Get("pool." + entry.resource)))
                {
                    string effect = CharacterArtificerText.Get(entry.feature);
                    string note = translate(effect);
                    if (entry.resource == "field")
                        return note.Split(new[] { "\n\n" }, StringSplitOptions.None).Last();
                    if (entry.resource == "genius" && edition == "24" && draft.level >= 14)
                    {
                        note += "\n" + translate(CharacterArtificerText.Get("advanced24"));
                        if (draft.level >= 20) note += "\n" + translate(CharacterArtificerText.Get("soul24"));
                    }
                    if (entry.resource == "jolt" && draft.level >= 15)
                        note += "\n" + translate(CharacterArtificerText.Get("battlesmith.15." + edition));
                    if ((entry.resource == "store" || entry.resource == "store.extra") && !string.IsNullOrWhiteSpace(draft.artificerStoredSpell))
                        note += "\n" + translate("Заклинання в предметі") + ": " + translate(draft.artificerStoredSpell);
                    return note;
                }
            if (matches(name, CharacterArtificerText.Get("pool.drain")))
                return translate("Вилучення магії: раз за довгий відпочинок знищ свою створену магічну річ, щоб відновити комірку 1 рівня для звичайної речі або 2 рівня для незвичайної чи рідкісної.");
            if (CharacterArtificerRules.Is(draft, "cartographer") && matches(name, "Вогні фей") &&
                !CharacterSpeciesRules.FreeSpells(draft.PrimaryCharacter).Contains("Faerie Fire"))
                return translate(CharacterArtificerText.Get("cartographer.illuminated"));
            if (matches(name, CharacterArtificerText.Get("pool.transmute")) || matches(name, "Перетворення"))
                return translate("Перетворення предмета: раз за довгий відпочинок дією Магія перетвори свою створену магічну річ на іншу річ за відомим планом.");
            return null;
        }

        internal static void WriteNotes(CharacterData character, CreationSheetBindings bindings,
            CharacterCreationDraft draft, string features, Func<string, string> translate)
        {
            var notes = NoteTexts(character, bindings, draft, features, translate);
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i].Length == 0) continue;
                bindings.Text(character, "magic.note." + i, notes[i]);
                bindings.Toggle(character, "magic.note." + i + ".open", true);
                character.SetSharedString(NoteStamp(draft, i), CreationDescriptionLayout.Fingerprint(notes[i]));
            }
        }
    }
}
