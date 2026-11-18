using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationMulticlassResources
    {
        internal static void Write(CharacterData character, CreationSheetBindings bindings, CharacterCreationDraft draft, Func<string, string> translate)
        {
            if (draft.secondaryClass?.IsSelected != true) return;
            var track = draft.secondaryClass.ClassDraft(draft);
            bool modern = draft.edition == RulesEdition.Rules2024;
            void Add(string name, int count, bool shortRest = false, bool partial = false)
            {
                if (count <= 0) return;
                string key = CreationMagicResources.Add(character, bindings, name, count, shortRest, useLastRow: true);
                if (partial) CreationResourceRecovery.Register(character, bindings, key, shortRest, true);
            }

            switch (track.characterClass)
            {
                case "fighter":
                    Add(translate("Друге дихання"), CharacterFighterRules.WindUses(track), true, modern);
                    break;
                case "barbarian":
                    Add(translate("Лють"), CharacterBarbarianRules.Rages(track), modern, modern);
                    break;
                case "bloodhunter":
                    Add(translate("Прокляття крові"), CharacterBloodHunterRules.CurseUses(track), true);
                    CreationBloodHunterResources.Write(character, bindings, track, translate);
                    break;
                case "cleric":
                case "paladin":
                    int uses = ChannelUses(track);
                    if (!modern || ChannelUses(draft) == 0)
                    {
                        int count = modern ? uses : Math.Max(uses, ChannelUses(draft));
                        bindings.Dropdown(character, "main.channel.count", count);
                        bindings.Toggle(character, "rest.main.channel", false);
                        if (count > 0) CreationResourceRecovery.Register(character, bindings, "rest.main.channel", true, modern);
                    }
                    else
                    {
                        Add(ChannelName(track, translate), uses, true, true);
                    }
                    CreationClericResources.Write(character, bindings, track, translate);
                    if (track.characterClass == "paladin")
                    {
                        int points = CharacterPaladinRules.LayOnHands(track);
                        bindings.Text(character, "main.lay.points", points.ToString());
                        character.SetSharedString("CreationLayOnHandsMaximum", points.ToString());
                        CreationResourceRecovery.RegisterPoints(character, bindings, "main.lay.points", points);
                    }
                    break;
                case "druid":
                    CreationDruidResources.Write(character, bindings, track, translate);
                    break;
                case "ranger":
                    Add(translate("Hunter's Mark"), CharacterRangerRules.MarkUses(track));
                    break;
                case "monk":
                    int focus = CharacterMonkRules.Points(track);
                    if (focus > 0)
                    {
                        bindings.Dropdown(character, "main.ki.count", focus);
                        CreationResourceRecovery.Register(character, bindings, "rest.main.ki", true);
                        if (modern) Add(translate("Незвичайний метаболізм"), 1);
                    }
                    break;
                case "sorcerer":
                    int pointsMaximum = CharacterSorcererRules.Points(track);
                    if (pointsMaximum > 0)
                    {
                        bindings.Dropdown(character, "main.sorcery.count", pointsMaximum);
                        bindings.Toggle(character, "rest.main.sorcery", false);
                        CreationResourceRecovery.RegisterPartial(character, bindings, "rest.main.sorcery",
                            modern && track.level >= 5 ? pointsMaximum / 2 : 0, modern);
                    }
                    break;
                case "warlock":
                    CreationWarlockResources.Write(character, bindings, track, CharacterCreationRules.Calculate(draft), translate);
                    break;
                case "wizard":
                    CreationWizardMagic.WriteResources(character, bindings, track, translate);
                    break;
                case "artificer":
                    CreationArtificerResources.Write(character, bindings, track, translate);
                    break;
            }

            CreationClassMagicResources.Write(character, bindings, track,
                CharacterCreationRules.Calculate(draft), translate);
        }

        private static int ChannelUses(CharacterCreationDraft d) => d.characterClass == "cleric"
            ? CharacterClericRules.ChannelUses(d) : CharacterPaladinRules.ChannelUses(d);

        internal static string ChannelName(CharacterCreationDraft track, Func<string, string> translate)
            => translate("Божественний канал") + " · " + translate(CharacterClassCatalog.Find(track.characterClass).name);

        internal static string Proficiencies(CharacterCreationDraft draft, Func<string, string> translate)
        {
            if (draft.secondaryClass?.IsSelected != true) return "";
            var track = draft.secondaryClass.ClassDraft(draft);
            bool modern = draft.edition == RulesEdition.Rules2024;
            string granted = track.characterClass switch
            {
                "fighter" or "paladin" or "ranger" => modern
                    ? "Легкі й середні обладунки, щити та військова зброя." : "Легкі й середні обладунки, щити, проста й військова зброя.",
                "barbarian" => modern ? "Щити й військова зброя." : "Щити, проста й військова зброя.",
                "bard" or "rogue" => "Легкі обладунки",
                "druid" => modern ? "Легкі обладунки, щити" : "Легкі й середні обладунки, щити",
                "cleric" => "Легкі й середні обладунки, щити",
                "monk" => modern ? "" : "Проста зброя, короткий меч",
                "warlock" => modern ? "Легкі обладунки" : "Легкі обладунки, проста зброя",
                "artificer" => "Легкі й середні обладунки, щити",
                "bloodhunter" => "Легкі й середні обладунки, щити, проста й військова зброя.",
                _ => ""
            };
            var additions = new[] { granted, track.fighterTool,
                    track.characterClass == "rogue" || track.characterClass == "artificer" && !modern ? "Злодійські інструменти" : "",
                    track.characterClass == "artificer" ? "Інструменти лудильника" : "",
                    track.characterClass == "bloodhunter" ? "Алхімічне приладдя" : "" }
                .Concat(track.bardInstruments).Concat(track.artificerToolReplacements)
                .Concat(CharacterArtificerRules.Active(track) ? CharacterArtificerRules.SubclassTools(track) : Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().Select(translate).ToArray();
            var languages = new[] { track.rogueLanguage, track.rangerLanguage }.Concat(track.rangerDeftLanguages)
                .Concat(track.rangerExtraLanguages).Concat(track.knowledgeLanguages)
                .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().Select(translate).ToArray();
            return (additions.Length == 0 ? "" : "\n" + translate("Мультиклас") + ": " + string.Join(", ", additions)) +
                (languages.Length == 0 ? "" : "\n" + translate("Мови") + ": " + string.Join(", ", languages));
        }
    }
}
