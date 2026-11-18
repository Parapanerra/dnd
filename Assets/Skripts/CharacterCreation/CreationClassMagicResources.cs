using System;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    internal static class CreationClassMagicResources
    {
        internal static void Write(CharacterData c, CreationSheetBindings bindings,
            CharacterCreationDraft draft, CreatedCharacterStats s, Func<string, string> translate,
            bool hasWeapon = false, int weaponMod = 0)
        {
            bool bard = CharacterBardRules.Active(draft);
            bool ranger = CharacterRangerRules.Active(draft);
            bool paladin = CharacterPaladinRules.Active(draft);
            bool sorcerer = CharacterSorcererRules.Active(draft);
            bool artificer = CharacterArtificerRules.Active(draft);
            bool monk = draft.characterClass == "monk";
            bool barbarian = CharacterBarbarianRules.Active(draft);
            bool rogue = CharacterRogueRules.Active(draft);
            bool trickster = CharacterTricksterSpells.Active(draft);
            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft);
            var sheet = c.GetSceneData("cartaPersonaj");
            void Write(int index, string value) => bindings.Text(c, "input." + index, value);
            string Add(string name, int uses, bool shortRest) =>
                CreationMagicResources.Add(c, bindings, name, uses, shortRest, useLastRow: draft.IsSecondaryTrack);
            string magicResource = paladin && draft.edition == RulesEdition.Rules2014 ? translate("Божественне чуття") :
                artificer && draft.edition == RulesEdition.Rules2024 ? translate("Створення предметів") :
                sorcerer && draft.edition == RulesEdition.Rules2024 ? translate("Вроджена магія") :
                CharacterSorcererRules.Is(draft, "wildmagic") ? translate("Хвилі хаосу") :
                bard ? string.Format(translate(CharacterBardText.Get("ui.inspiration")), CharacterBardRules.Die(draft)) : "";
            if (magicResource.Length > 0)
            {
                int uses = paladin ? Math.Max(0, 1 + CharacterCreationRules.Modifier(s.scores[5])) :
                    artificer ? Math.Max(1, CharacterCreationRules.Modifier(s.scores[3])) :
                    sorcerer ? draft.edition == RulesEdition.Rules2024 ? 2 : 1 : Math.Max(1, CharacterCreationRules.Modifier(s.scores[5]));
                Add(magicResource, uses, bard && draft.level >= 5);
            }
            if(!draft.IsSecondaryTrack && CharacterBardRules.Is(draft,"dance")) {
                int dex=CharacterCreationRules.Modifier(s.scores[1]);
                Write(69,CharacterCreationRules.Signed(proficiency+dex));Write(70,translate(CharacterBardText.Get("ui.dance.attack")));Write(71,"1d"+CharacterBardRules.Die(draft)+CharacterCreationRules.Signed(dex));
            }
            if(CharacterBardRules.Is(draft,"glamour")) {
                Add(translate(CharacterBardText.Get("glamour.3").Split(':')[0]),1,false);
                if(draft.level>=6)Add(translate(CharacterBardText.Get("glamour.6").Split(':')[0]),1,false);
                if(draft.level>=14)Add(translate(CharacterBardText.Get("glamour.14").Split(':')[0]),1,true);
            }
            if (monk && draft.level >= 6 && draft.subclass == "openhand")
                Add(translate("Цілісність тіла"), draft.edition == RulesEdition.Rules2014 ? 1 : Math.Max(1, CharacterCreationRules.Modifier(s.scores[4])), false);
            if (CharacterMonkSubclassRules.Is(draft, "mercy"))
            {
                if (draft.level >= 11) Add(translate("Шквал зцілення й шкоди"), Math.Max(1, CharacterCreationRules.Modifier(s.scores[4])), false);
                if (draft.level >= 17) Add(translate("Долоня найвищого милосердя"), 1, false);
            }
            if (barbarian && draft.edition == RulesEdition.Rules2024)
            {
                if (draft.level >= 15) Add(translate("Стійка лють — відновлення при ініціативі"), 1, false);
                if (CharacterBarbarianRules.Is(draft, "berserker") && draft.level >= 14) Add(translate("Загрозлива присутність"), 1, false);
                if (CharacterBarbarianRules.Is(draft, "zealot"))
                {
                    Add(translate("Воїн богів — кубики к12"), CharacterBarbarianRules.HealingDice(draft), false);
                    if (draft.level >= 10) Add(translate("Ревна присутність"), 1, false);
                    if (draft.level >= 14) Add(translate("Лють богів"), 1, false);
                }
            }
            if (sorcerer && draft.edition == RulesEdition.Rules2024) {
                int charisma = Math.Max(1, CharacterCreationRules.Modifier(s.scores[5]));
                if (CharacterSorcererRules.Is(draft, "wildmagic")) Add(translate("Хвилі хаосу"), 1, false);
                if (CharacterSorcererRules.Is(draft, "clockwork")) {
                    Add(translate(CharacterSorcererText.Get("clockwork.3").Split(':')[0]), charisma, false);
                    if (draft.level >= 14) Add(translate(CharacterSorcererText.Get("clockwork.14").Split(':')[0]), 1, false);
                    if (draft.level >= 18) Add(translate(CharacterSorcererText.Get("clockwork.18").Split(':')[0]), 1, false);
                }
                if (CharacterSorcererRules.Is(draft, "aberrant") && draft.level >= 18) Add(translate(CharacterSorcererText.Get("aberrant.18").Split(':')[0]), 1, false);
                if (CharacterSorcererRules.Is(draft, "draconic")) {
                    if (draft.level >= 14) Add(translate(CharacterSorcererText.Get("dragon.wings24").Split(':')[0]), 1, false);
                    if (draft.level >= 18) Add(translate("Виклик дракона — без комірки"), 1, false);
                }
                if (CharacterSorcererRules.Is(draft, "wildmagic") && draft.level >= 18) Add(translate(CharacterSorcererText.Get("wild.18.24").Split(':')[0]), 1, false);
            }
            if (ranger && CharacterRangerRules.Revised(draft))
            {
                int wisdom = Math.Max(1, CharacterRangerRules.Wisdom(draft));
                if (draft.level >= 10) Add(translate(CharacterRangerText.Get("tireless").Split(':')[0]), wisdom, false);
                if (draft.level >= 14) Add(translate(CharacterRangerText.Get("veil").Split(':')[0]), wisdom, false);
                if (CharacterRangerRules.Is(draft, "gloomstalker")) Add(translate("Моторошний удар"), wisdom, false);
                if (CharacterRangerRules.Is(draft, "feywanderer") && draft.level >= 11) Add(translate("Виклик феї — без комірки"), 1, false);
                if (CharacterRangerRules.Is(draft, "feywanderer") && draft.level >= 15) Add(translate("Туманний крок — без комірки"), wisdom, false);
                if (draft.level >= 10) c.SetSharedString("CreationTireless", "1");
            }
            if (paladin)
            {
                int channel = CharacterPaladinRules.ChannelUses(draft);
                if (!draft.IsSecondaryTrack)
                {
                bindings.Dropdown(c, "main.channel.count", channel);
                bindings.Toggle(c, "rest.main.channel", false);
                if (channel > 0) {
                    CreationResourceRecovery.Register(c, bindings, "rest.main.channel", true, CharacterPaladinRules.Revised(draft));
                }
                }
                if (CharacterPaladinRules.Revised(draft) && draft.level >= 2) Add(translate("Божественна кара — без комірки"), 1, false);
                if (CharacterPaladinRules.Revised(draft) && draft.level >= 5) Add(translate("Поклик скакуна"), 1, false);
                if (!CharacterPaladinRules.Revised(draft) && draft.level >= 14) Add(translate("Очисний дотик"), Math.Max(1, CharacterCreationRules.Modifier(s.scores[5])), false);
                if (draft.level >= 15 && (CharacterPaladinRules.Is(draft, "ancients") || CharacterPaladinRules.Is(draft, "glory"))) Add(translate(CharacterPaladinRules.Is(draft, "ancients") ? "Невмирущий вартовий" : "Славний захист"), CharacterPaladinRules.Is(draft, "glory") ? Math.Max(1, CharacterCreationRules.Modifier(s.scores[5])) : 1, false);
                if (draft.level >= 20) Add(translate(CharacterPaladinText.Get(draft.subclass + ".20." + (CharacterPaladinRules.Revised(draft) ? "24" : "14")).Split(':')[0]), 1, false);
                if (draft.level >= 11 && hasWeapon && !draft.IsSecondaryTrack) Write(68, sheet.inputData[68] + " +1d8");
                if (draft.level >= 2 && draft.fightingStyle == FighterStyle.Dueling && hasWeapon && !draft.IsSecondaryTrack) Write(68, "1d8" + CharacterCreationRules.Signed(weaponMod + 2) + (draft.level >= 11 ? " +1d8" : ""));
            }
            if (rogue && draft.level >= 20) Add(translate("Удача"), 1, true);
            if (!draft.IsSecondaryTrack && draft.characterClass == "fighter" && CharacterFighterRules.HasStyle(draft, FighterStyle.Unarmed))
            {
                Write(69, CharacterCreationRules.Signed(proficiency + CharacterCreationRules.Modifier(s.scores[0])));
                Write(70, translate("Беззбройний удар")); Write(71, "1d6" + CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[0])));
            }
            if (draft.characterClass == "fighter")
            {
                if (CharacterFighterRules.SurgeUses(draft) > 0) Add(translate("Сплеск дії"), CharacterFighterRules.SurgeUses(draft), true);
                if (CharacterFighterRules.IndomitableUses(draft) > 0) Add(translate("Незламність"), CharacterFighterRules.IndomitableUses(draft), false);
                if (CharacterFighterRules.Is(draft, "battlemaster"))
                {
                    Add(string.Format(translate("Кістки переваги к{0}"), CharacterFighterRules.SuperiorityDie(draft)), CharacterFighterRules.SuperiorityCount(draft), true);
                    if (draft.edition == RulesEdition.Rules2024 && draft.level >= 7) Add(translate("Пізнай ворога"), 1, false);
                }
                if (CharacterFighterRules.Is(draft, "psiwarrior"))
                {
                    if (!draft.IsSecondaryTrack)
                    {
                    int psiMod = CharacterCreationRules.Modifier(s.scores[3]);
                    Write(44, CharacterCreationRules.Signed(psiMod)); Write(48, (8 + proficiency + psiMod).ToString());
                    c.SetSharedString("SharedInput_magMod", CharacterCreationRules.Signed(psiMod)); c.SetSharedString("SharedInput_slogSpas", (8 + proficiency + psiMod).ToString());
                    }
                    int pool = CharacterFighterRules.PsiCount(draft); var keys = new List<string>();
                    for (int offset = 0; offset < pool; offset += 6)
                        keys.Add(Add(string.Format(translate("Псіонічні кістки к{0}: {1}–{2}"), CharacterFighterRules.PsiDie(draft), offset + 1, Math.Min(offset + 6, pool)), Math.Min(6, pool - offset), false));
                    CreationResourceRecovery.RegisterPool(c, bindings, "rest.fighter.psi", keys.ToArray(), true, true);
                    Add(translate("Телекінетичне переміщення"), 1, true);
                    if (draft.level >= 7) Add(translate("Псіонічний стрибок"), 1, true);
                    if (draft.level >= 15) Add(translate("Силова твердиня"), 1, false);
                    if (draft.level >= 18) Add(translate("Телекінез — безкоштовне накладання"), 1, false);
                }
            }
            if (trickster && draft.level >= 17) Add(translate("Крадій спелів"), 1, false);
            if (CharacterRogueRules.Is(draft, "soulknife"))
            {
                int pool = CharacterRogueRules.PsiDice(draft); var restKeys = new List<string>();
                for (int offset = 0; offset < pool; offset += 6)
                    restKeys.Add(Add(string.Format(translate("Псіонічні кістки к{0}: {1}–{2}"), CharacterRogueRules.PsiFaces(draft), offset + 1, Math.Min(offset + 6, pool)), Math.Min(6, pool - offset), false));
                CreationResourceRecovery.RegisterPool(c, bindings, "rest.psi.pool", restKeys.ToArray(), true, true);
                Add(translate("Психічний шепіт — безкоштовне використання"), 1, false);
                if (draft.level >= 13) Add(translate("Психічна завіса"), 1, false);
                if (draft.level >= 17) Add(translate("Розрив розуму"), 1, false);
            }
        }
    }
}
