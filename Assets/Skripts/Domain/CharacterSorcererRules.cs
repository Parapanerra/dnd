using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterSorcererRules
    {
        public static readonly string[] DragonNames = { "Чорний (кислота)", "Синій (блискавка)", "Латунний (вогонь)", "Бронзовий (блискавка)", "Мідний (кислота)", "Золотий (вогонь)", "Зелений (отрута)", "Червоний (вогонь)", "Срібний (холод)", "Білий (холод)" };
        public static readonly string[] Classic = { "draconic", "wildmagic" };
        public static readonly string[] Modern = { "draconic", "wildmagic", "aberrant", "clockwork" };
        public static readonly string[] Names = { "Драконяче походження", "Дика магія", "Аберантна магія", "Заводна магія" };
        public static readonly string[] ClassicMetamagic = { "careful", "distant", "empowered", "extended", "heightened", "quickened", "subtle", "twinned" };
        public static readonly string[] ModernMetamagic = ClassicMetamagic.Concat(new[] { "seeking", "transmuted" }).ToArray();
        public static readonly string[] Elements = { "Кислота", "Холод", "Вогонь", "Блискавка", "Отрута" };
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "sorcerer";
        public static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static int SubclassLevel(CharacterCreationDraft d) => Revised(d) ? 3 : 1;
        public static bool Is(CharacterCreationDraft d, string subclass) => Active(d) && d.level >= SubclassLevel(d) && d.subclass == subclass;
        // The old Draconic helper also controls the 2014 language grant.
        public static bool Draconic(CharacterCreationDraft d) => Is(d, "draconic") && !Revised(d);
        public static string[] Options(CharacterCreationDraft d) => Revised(d) ? Modern : Classic;
        public static int Points(CharacterCreationDraft d) => Active(d) && d.level >= 2 ? d.level : 0;
        internal static int Slots(CharacterCreationDraft d, int circle) => Active(d) ? CharacterFullCasterSlots.Count(d.level, circle) : 0;
        public static int MaximumCircle(CharacterCreationDraft d) => d.level < 1 ? 0 : Math.Min(9, (d.level + 1) / 2);
        private static readonly int[] Known = { 2,3,4,5,6,7,8,9,10,11,12,12,13,13,14,14,15,15,15,15 };
        private static readonly int[] Prepared = { 2,4,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22 };
        internal static int SpellCount(CharacterCreationDraft d) => Active(d) && d.level >= 1 ? (Revised(d) ? Prepared : Known)[Math.Clamp(d.level, 1, 20) - 1] : 0;
        internal static int Cantrips(CharacterCreationDraft d) => !Active(d) || d.level < 1 ? 0 : d.level >= 10 ? 6 : d.level >= 4 ? 5 : 4;
        public static int MetamagicCount(CharacterCreationDraft d) => !Active(d) || d.level < (Revised(d) ? 2 : 3) ? 0 : Revised(d) ? d.level >= 17 ? 6 : d.level >= 10 ? 4 : 2 : d.level >= 17 ? 4 : d.level >= 10 ? 3 : 2;
        public static string[] MetamagicOptions(CharacterCreationDraft d) => Revised(d) ? ModernMetamagic : ClassicMetamagic;
        public static string MetamagicText(CharacterCreationDraft d, string id) => CharacterSorcererText.Get("mm." + id + (new[] { "careful", "extended", "heightened", "quickened", "subtle", "twinned" }.Contains(id) ? Revised(d) ? ".24" : ".14" : ""));
        public static string[] AutomaticSpells(CharacterCreationDraft d)
        {
            if (!Active(d) || !Revised(d) || d.level < 3) return Array.Empty<string>();
            string[] spells = d.subclass == "aberrant" ? new[] { "Arms of Hadar", "Calm Emotions", "Detect Thoughts", "Dissonant Whispers", "Mind Sliver", "Hunger of Hadar", "Sending", "Evard's Black Tentacles", "Summon Aberration", "Rary's Telepathic Bond", "Telekinesis" }
                : d.subclass == "clockwork" ? new[] { "Aid", "Alarm", "Lesser Restoration", "Protection from Evil and Good", "Dispel Magic", "Protection from Energy", "Freedom of Movement", "Summon Construct", "Greater Restoration", "Wall of Force" }
                : d.subclass == "draconic" ? new[] { "Alter Self", "Chromatic Orb", "Command", "Dragon's Breath", "Fear", "Fly", "Arcane Eye", "Charm Monster", "Legend Lore", "Summon Dragon" } : Array.Empty<string>();
            int first = d.subclass == "aberrant" ? 5 : 4;
            return spells.Take(first + (d.level >= 9 ? 6 : d.level >= 7 ? 4 : d.level >= 5 ? 2 : 0)).ToArray();
        }
        // Every new spell and the optional replacement must have been available at that level.
        // Nested circle limits are the capacities of those learning opportunities.
        public static bool CanLearn(CharacterCreationDraft d, System.Collections.Generic.IEnumerable<int> spellCircles)
        {
            int[] circles = spellCircles.ToArray();
            if (d.level < 1 || d.level > 20) return circles.Length == 0;
            if (circles.Any(circle => circle < 1 || circle > MaximumCircle(d))) return false;
            var counts = Revised(d) ? Prepared : Known;
            for (int circle = 2; circle <= MaximumCircle(d); circle++) {
                int unlocked = circle * 2 - 1;
                int opportunities = counts[d.level - 1] - counts[unlocked - 2] + d.level - unlocked + 1;
                if (circles.Count(value => value >= circle) > opportunities) return false;
            }
            return true;
        }
        public static bool WeaponProficient(CharacterCreationDraft d, string weapon) => Revised(d) || new[] { "Кинджал", "Дротик", "Праща", "Бойовий посох", "Легкий арбалет" }.Contains(weapon) || CharacterSpeciesRules.IsDwarf(d) && weapon == "Ручна сокира";
        internal static void Repair(CharacterCreationDraft d)
        {
            d.sorcererMetamagic ??= new System.Collections.Generic.List<string>();
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < d.sorcererMetamagic.Count; i++) if (!MetamagicOptions(d).Contains(d.sorcererMetamagic[i]) || !seen.Add(d.sorcererMetamagic[i])) d.sorcererMetamagic[i] = "";
            int limit = MetamagicCount(d);
            if (d.sorcererMetamagic.Count > limit) d.sorcererMetamagic.RemoveRange(limit, d.sorcererMetamagic.Count - limit);
            if (!Active(d)) { d.sorcererMetamagic.Clear(); d.sorcererAffinity = -1; return; }
            if (!Is(d, "draconic") || !Revised(d) || d.level < 6) d.sorcererAffinity = -1;
            var automatic = AutomaticSpells(d);
            if (d.preparedSpells != null) for (int i = 0; i < d.preparedSpells.Count; i++) if (automatic.Contains(d.preparedSpells[i])) d.preparedSpells[i] = "";
            if (d.cantrips != null) for (int i = 0; i < d.cantrips.Count; i++) if (automatic.Contains(d.cantrips[i])) d.cantrips[i] = "";
        }
        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Active(d)) return null;
            if (page == 0 && d.level >= SubclassLevel(d) && !Options(d).Contains(d.subclass)) return CharacterSorcererText.Get("error.subclass");
            if (page == 0 && Draconic(d) && (d.dragonAncestor < 0 || d.dragonAncestor >= DragonNames.Length)) return CharacterSorcererText.Get("error.ancestor");
            if (page == 0 && Is(d, "draconic") && Revised(d) && d.level >= 6 && (d.sorcererAffinity < 0 || d.sorcererAffinity >= Elements.Length)) return CharacterSorcererText.Get("error.affinity");
            if (page == 4 && (d.sorcererMetamagic == null || d.sorcererMetamagic.Count != MetamagicCount(d) || d.sorcererMetamagic.Distinct().Count() != MetamagicCount(d) || d.sorcererMetamagic.Any(x => !MetamagicOptions(d).Contains(x)))) return CharacterSorcererText.Get("error.metamagic");
            if (page == 4 && !d.IsSecondaryTrack && !Revised(d) && (!CharacterCreationRules.MonkSimpleWeapons.Contains(d.sorcererWeapon) || d.sorcererFocus != "Сумка компонентів" && d.sorcererFocus != "Арканний фокус" || d.sorcererPack < 0 || d.sorcererPack > 1)) return "Обери зброю, фокус і набір чародія.";
            return null;
        }
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d); int Mod(int a) => CharacterCreationRules.Modifier(scores[a]);
            bool dragon = Is(d, "draconic");
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if (Revised(d) && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            return new CreatedCharacterStats { scores = scores, skills = skills.Distinct().ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, Mod(2)) + (dragon ? d.level : 0),
                armorClass = dragon ? Revised(d) ? 10 + Mod(1) + Math.Max(0, Mod(5)) : 13 + Mod(1) : 10 + Mod(1),
                initiative = Mod(1), speed = CharacterSpeciesRules.IsDwarf(d) && !Revised(d) ? 25 : 30,
                passivePerception = 10 + Mod(4) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = CharacterSpeciesRules.Name(d), backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "", equipment = Equipment(d), features = Features(d) };
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var ids = new System.Collections.Generic.List<string>(); string v = Revised(d) ? "24" : "14";
            if (Revised(d)) ids.Add("innate");
            if (d.level >= 2) ids.Add("font" + v);
            if (MetamagicCount(d) > 0) { ids.Add("metamagic"); foreach (string option in d.sorcererMetamagic ?? new System.Collections.Generic.List<string>()) if (MetamagicOptions(d).Contains(option)) ids.Add("mm." + option + (new[] { "careful", "extended", "heightened", "quickened", "subtle", "twinned" }.Contains(option) ? "." + v : "")); }
            if (Revised(d) && d.level >= 5) ids.Add("rest24");
            if (Revised(d) && d.level >= 7) ids.Add("incarnate");
            if (d.level >= 20) ids.Add(Revised(d) ? "apotheosis" : "rest14");
            if (Is(d, "draconic")) {
                ids.Add("dragon.resilience" + v);
                if (!Revised(d)) ids.Add("dragon.language");
                if (d.level >= 6) ids.Add("dragon.affinity" + v);
                if (d.level >= 14) ids.Add("dragon.wings" + v);
                if (d.level >= 18) ids.Add("dragon.18." + v);
            }
            if (Is(d, "wildmagic")) {
                ids.Add("wild.tides" + v); ids.Add("wild.surge" + v);
                if (d.level >= 6) ids.Add("wild.luck" + v);
                if (d.level >= 14) ids.Add("wild.chaos");
                if (d.level >= 18) ids.Add("wild.18." + v);
            }
            foreach (string sub in new[] { "aberrant", "clockwork" }) if (Is(d, sub)) foreach (int level in new[] { 3, 6, 14, 18 }) if (d.level >= level) ids.Add(sub + "." + level);
            var lines = ids.Select(CharacterSorcererText.Get).ToList();
            if (Draconic(d) && d.dragonAncestor >= 0 && d.dragonAncestor < DragonNames.Length) lines.Add("Драконячий предок: " + DragonNames[d.dragonAncestor] + ".");
            if (Is(d, "draconic") && Revised(d) && d.level >= 6 && d.sorcererAffinity >= 0 && d.sorcererAffinity < Elements.Length) lines.Add("Стихійна спорідненість: " + Elements[d.sorcererAffinity] + ".");
            if (CharacterSpeciesRules.IsDwarf(d)) lines.Add(Revised(d) ? CharacterFighterText.Get("dwarf24") : CharacterSpeciesRules.Dwarf2014Features(d).TrimStart('\n'));
            if (Revised(d) && d.species == "human") lines.Add("Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.");
            return string.Join("\n", lines) + CharacterBackgroundRules.Features(d);
        }

        internal static string Equipment(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024
            ? d.equipment == 0 ? "Спис; 2 кинджали; арканний фокус (кристал); набір підземельника; 28 зм." : "50 зм замість класового спорядження."
            : d.sorcererWeapon + (d.sorcererWeapon == "Легкий арбалет" ? "; 20 болтів" : "") + "; " + d.sorcererFocus + "; 2 кинджали; " + (d.sorcererPack == 0 ? "набір підземельника." : "набір мандрівника.");
    }
}
