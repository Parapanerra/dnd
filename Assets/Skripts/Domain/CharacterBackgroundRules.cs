using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public sealed class CharacterBackgroundOption
    {
        public string id, name, feature, equipment, toolKind, feat;
        public int[] skills, increases;
        public string[] tools, items;
        public int languages, gold;
        public bool modern;
    }
    public static class CharacterBackgroundRules
    {
        private static CharacterBackgroundOption Classic(string id, string name, int a, int b, string feature, string equipment, string[] tools = null, string kind = "", int languages = 0, params string[] items) => new CharacterBackgroundOption {
            id = id, name = name, skills = new[] { a, b }, feature = feature, equipment = equipment,
            tools = tools ?? Array.Empty<string>(), toolKind = kind, languages = languages, items = items, increases = Array.Empty<int>(),
            gold = id == "hermit" ? 5 : id == "noble" ? 25 : new[] { "acolyte", "charlatan", "criminal", "entertainer", "guildartisan" }.Contains(id) ? 15 : 10
        };
        private static readonly CharacterBackgroundOption[] Options = {
            Classic("acolyte", "Послушник", 6, 14, "Притулок вірних: прихильний храм дає тобі скромне утримання, а групі — лікування й догляд; матеріали спелів оплачуються окремо.", "Священний символ; {1}; 5 пахощів; ритуальні шати; звичайний одяг; 15 зм.", languages: 2, items: new[] { "Молитовник", "Молитовне колесо" }),
            Classic("charlatan", "Шарлатан", 4, 15, "Фальшива особа: маєш друге посвідчення та зв’язки; можеш підробляти знайомі документи й почерк.", "Вишуканий одяг; набір гримувальника; {1}; 15 зм.", new[] { "Набір гримувальника", "Набір фальсифікатора" }, items: new[] { "10 пляшечок кольорової рідини", "Шахрайські кості", "Мічені карти", "Фальшивий перстень-печатка" }),
            Classic("criminal", "Злочинець", 4, 16, "Кримінальний зв’язок: надійний посередник передає повідомлення через злочинну мережу.", "Ломик; темний одяг із каптуром; гаманець із 15 зм.", new[] { "Злодійські інструменти" }, "gaming"),
            Classic("entertainer", "Артист", 0, 12, "Улюбленець публіки: нічні виступи дають житло й харчування у закладі; місцева публіка впізнає тебе.", "{0}; подарунок шанувальника; сценічний одяг; 15 зм.", new[] { "Набір гримувальника" }, "instrument"),
            Classic("folkhero", "Народний герой", 1, 17, "Сільська гостинність: прості люди прихистять і сховають тебе та групу, але не ризикуватимуть життям.", "{0}; лопата; залізний казан; звичайний одяг; 10 зм.", new[] { "Наземний транспорт" }, "artisan"),
            Classic("guildartisan", "Гільдійний ремісник", 6, 13, "Членство в гільдії: допомога, житло й зв’язки гільдії; внесок 5 зм на місяць, борги треба сплатити.", "{0}; рекомендаційний лист гільдії; дорожній одяг; 15 зм.", kind: "artisan", languages: 1),
            Classic("hermit", "Самітник", 9, 14, "Відкриття: узгодь з майстром особливе знання, здобуте в усамітненні, та його роль у пригоді.", "Футляр із записами; зимова ковдра; звичайний одяг; набір травника; 5 зм.", new[] { "Набір травника" }, languages: 1),
            Classic("noble", "Шляхтич", 5, 13, "Привілейоване становище: доступ до вищого світу й аудієнції місцевого дворянина; простолюд прагне догодити.", "Вишуканий одяг; перстень-печатка; родовід; 25 зм.", kind: "gaming", languages: 1),
            Classic("outlander", "Чужинець", 3, 17, "Мандрівник: пам’ятаєш карти й місцевість; знаходиш їжу та воду собі й ще п’ятьом, якщо природа це дозволяє.", "Посох; мисливська пастка; трофей звіра; дорожній одяг; 10 зм.", kind: "instrument", languages: 1),
            Classic("sage", "Мудрець", 2, 5, "Дослідник: якщо не знаєш потрібних відомостей, зазвичай знаєш, де або в кого їх шукати; доступність визначає майстер.", "Чорнило; перо; малий ніж; лист колеги; звичайний одяг; 10 зм.", languages: 2),
            Classic("sailor", "Моряк", 3, 11, "Морський перехід: дружній корабель може безкоштовно перевезти групу за допомогу команді; маршрут і строки визначає майстер.", "Дубина; шовкова мотузка 50 футів; щасливий талісман; звичайний одяг; 10 зм.", new[] { "Інструменти навігатора", "Водний транспорт" }),
            Classic("soldier", "Солдат", 3, 7, "Військове звання: особливість передісторії солдата.", "Знак звання; трофей; {0}; звичайний одяг; гаманець із 10 зм.", new[] { "Наземний транспорт" }, "gaming"),
            Classic("urchin", "Безпритульний", 15, 16, "Таємниці міста: поза боєм проводиш групу між місцями в місті вдвічі швидше.", "Малий ніж; карта рідного міста; ручна миша; пам’ятка батьків; звичайний одяг; 10 зм.", new[] { "Набір гримувальника", "Злодійські інструменти" }),
            new CharacterBackgroundOption { id="acolyte", name="Послушник", modern=true, gold=8, feat="magiccleric", skills=new[] { 6, 14 }, increases=new[] { 3, 4, 5 }, tools=new[] { "Приладдя каліграфа" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Приладдя каліграфа; молитовник; священний символ; 10 аркушів пергаменту; мантія; 8 зм." },
            new CharacterBackgroundOption { id="artisan", name="Ремісник", modern=true, gold=32, feat="crafter", skills=new[] { 8, 13 }, increases=new[] { 0, 1, 3 }, tools=Array.Empty<string>(), items=Array.Empty<string>(), toolKind="artisan", feature="", equipment="{0}; 2 гаманці; дорожній одяг; 32 зм." },
            new CharacterBackgroundOption { id="charlatan", name="Шарлатан", modern=true, gold=15, feat="skilled", skills=new[] { 4, 15 }, increases=new[] { 1, 2, 5 }, tools=new[] { "Набір фальсифікатора" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Набір фальсифікатора; костюм; вишуканий одяг; 15 зм." },
            new CharacterBackgroundOption { id="criminal", name="Злочинець", modern=true, gold=16, feat="alert", skills=new[] { 15, 16 }, increases=new[] { 1, 2, 3 }, tools=new[] { "Злодійські інструменти" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="2 кинджали; злодійські інструменти; ломик; 2 гаманці; дорожній одяг; 16 зм." },
            new CharacterBackgroundOption { id="entertainer", name="Артист", modern=true, gold=11, feat="musician", skills=new[] { 0, 12 }, increases=new[] { 0, 1, 5 }, tools=Array.Empty<string>(), items=Array.Empty<string>(), toolKind="instrument", feature="", equipment="{0}; 2 костюми; дзеркало; парфуми; дорожній одяг; 11 зм." },
            new CharacterBackgroundOption { id="farmer", name="Фермер", modern=true, gold=30, feat="tough", skills=new[] { 1, 10 }, increases=new[] { 0, 2, 4 }, tools=new[] { "Інструменти теслі" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Серп; інструменти теслі; набір цілителя; залізний казан; лопата; дорожній одяг; 30 зм." },
            new CharacterBackgroundOption { id="guard", name="Вартовий", modern=true, gold=12, feat="alert", skills=new[] { 3, 11 }, increases=new[] { 0, 3, 4 }, tools=Array.Empty<string>(), items=Array.Empty<string>(), toolKind="gaming", feature="", equipment="Спис; легкий арбалет; 20 болтів; {0}; критий ліхтар; кайдани; сагайдак; дорожній одяг; 12 зм." },
            new CharacterBackgroundOption { id="guide", name="Провідник", modern=true, gold=3, feat="magicdruid", skills=new[] { 16, 17 }, increases=new[] { 1, 2, 4 }, tools=new[] { "Інструменти картографа" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Короткий лук; 20 стріл; інструменти картографа; спальник; сагайдак; намет; дорожній одяг; 3 зм." },
            new CharacterBackgroundOption { id="hermit", name="Самітник", modern=true, gold=16, feat="healer", skills=new[] { 9, 14 }, increases=new[] { 2, 4, 5 }, tools=new[] { "Набір травника" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Посох; набір травника; спальник; книга філософії; лампа; 3 фляги олії; дорожній одяг; 16 зм." },
            new CharacterBackgroundOption { id="merchant", name="Торговець", modern=true, gold=22, feat="lucky", skills=new[] { 1, 13 }, increases=new[] { 2, 3, 5 }, tools=new[] { "Інструменти навігатора" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Інструменти навігатора; 2 гаманці; дорожній одяг; 22 зм." },
            new CharacterBackgroundOption { id="noble", name="Шляхтич", modern=true, gold=29, feat="skilled", skills=new[] { 5, 13 }, increases=new[] { 0, 3, 5 }, tools=Array.Empty<string>(), items=Array.Empty<string>(), toolKind="gaming", feature="", equipment="{0}; вишуканий одяг; парфуми; 29 зм." },
            new CharacterBackgroundOption { id="sage", name="Мудрець", modern=true, gold=8, feat="magicwizard", skills=new[] { 2, 5 }, increases=new[] { 2, 3, 4 }, tools=new[] { "Приладдя каліграфа" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Посох; приладдя каліграфа; книга історії; 8 аркушів пергаменту; мантія; 8 зм." },
            new CharacterBackgroundOption { id="sailor", name="Моряк", modern=true, gold=20, feat="tavern", skills=new[] { 0, 11 }, increases=new[] { 0, 1, 4 }, tools=new[] { "Інструменти навігатора" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Кинджал; інструменти навігатора; мотузка; дорожній одяг; 20 зм." },
            new CharacterBackgroundOption { id="scribe", name="Писар", modern=true, gold=23, feat="skilled", skills=new[] { 8, 11 }, increases=new[] { 1, 3, 4 }, tools=new[] { "Приладдя каліграфа" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="Приладдя каліграфа; вишуканий одяг; лампа; 3 фляги олії; 12 аркушів пергаменту; 23 зм." },
            new CharacterBackgroundOption { id="soldier", name="Солдат", modern=true, gold=14, feat="savage", skills=new[] { 3, 7 }, increases=new[] { 0, 1, 2 }, tools=Array.Empty<string>(), items=Array.Empty<string>(), toolKind="gaming", feature="", equipment="Спис; короткий лук; 20 стріл; {0}; набір цілителя; сагайдак; дорожній одяг; 14 зм." },
            new CharacterBackgroundOption { id="wayfarer", name="Мандрівний бідняк", modern=true, gold=16, feat="lucky", skills=new[] { 6, 16 }, increases=new[] { 1, 4, 5 }, tools=new[] { "Злодійські інструменти" }, items=Array.Empty<string>(), toolKind="", feature="", equipment="2 кинджали; злодійські інструменти; ігровий набір; спальник; 2 гаманці; дорожній одяг; 16 зм." }
        };
        internal static string Features(CharacterCreationDraft d) => Find(d) == null || Find(d).modern ? "" : "\n" + Find(d).feature;
        public static int Gold(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024 && d.backgroundEquipmentChoice == 1 ? 50 : Find(d)?.gold ?? 0;
        public static CharacterBackgroundOption[] Available(RulesEdition edition) => Options.Where(option => option.modern == (edition == RulesEdition.Rules2024)).ToArray();
        public static CharacterBackgroundOption Find(CharacterCreationDraft d) => Available(d.edition).FirstOrDefault(option => option.id == d.background);
        internal static int[] Skills(CharacterCreationDraft d) => Find(d)?.skills ?? Array.Empty<int>();
        internal static int[] Increases(CharacterCreationDraft d) => Find(d)?.increases ?? Array.Empty<int>();
        public static bool NeedsGaming(CharacterCreationDraft d) => Find(d)?.toolKind == "gaming";
        public static string[] ToolOptions(CharacterCreationDraft d) => Find(d)?.toolKind == "artisan" ? CharacterCreationRules.MonkTools.Take(17).ToArray() : Find(d)?.toolKind == "instrument" ? CharacterCreationRules.MonkTools.Skip(17).ToArray() : Array.Empty<string>();
        public static string ToolChoice(CharacterCreationDraft d) => NeedsGaming(d) ? d.gamingSet : d.backgroundTool;
        public static string[] ClassTools(CharacterCreationDraft d, bool includeSpecies = true) => (d.characterClass == "artificer" ? new[] { "Злодійські інструменти", "Інструменти лудильника", d.artificerTool } :
            CharacterFighterRules.Is(d, "battlemaster") ? new[] { d.fighterTool } : d.characterClass == "rogue" ? CharacterRogueRules.Tools(d) : d.characterClass == "bard" ? d.bardInstruments.ToArray() : d.characterClass == "monk" ? (CharacterMonkSubclassRules.Is(d, "mercy") ? new[] { d.monkTool, "Набір травника" } : new[] { d.monkTool }) : d.characterClass == "druid" ? new[] { "Набір травника" } : d.characterClass == "bloodhunter" ? new[] { "Алхімічне приладдя" } : Array.Empty<string>())
            .Concat(CharacterSpeciesRules.IsDwarf(d) && d.edition == RulesEdition.Rules2014 ? new[] { d.artisanTool } : CharacterGnomeRules.ClassicRock(d) ? new[] { "Інструменти лудильника", d.originToolReplacement } : Array.Empty<string>())
            .Concat(includeSpecies?CharacterExpandedSpeciesRules.PermanentTools(d):Array.Empty<string>()).Concat(includeSpecies&&d.species=="satyr"?new[]{d.expandedInstrument}:Array.Empty<string>())
            .Concat(includeSpecies && CharacterWarforgedRules.Warforged(d) ? new[] { d.speciesTool } : Array.Empty<string>()).Where(tool => tool.Length > 0).Distinct().ToArray();
        public static string[] AllTools => CharacterCreationRules.MonkTools.Concat(new[] { "Злодійські інструменти", "Набір травника", "Набір гримувальника", "Набір фальсифікатора", "Інструменти навігатора", "Гральні кості", "Карти", "Шахи", "Наземний транспорт", "Водний транспорт" }).ToArray();
        public static string[] DuplicateTools(CharacterCreationDraft d) => (Find(d)?.tools ?? Array.Empty<string>()).Where(ClassTools(d).Contains).ToArray();
        public static string[] Tools(CharacterCreationDraft d) => (Find(d)?.tools ?? Array.Empty<string>()).Where(tool => !ClassTools(d).Contains(tool))
            .Concat((d.backgroundToolReplacements ?? Array.Empty<string>()).Take(DuplicateTools(d).Length)).Concat(ToolChoice(d).Length > 0 ? new[] { ToolChoice(d) } : Array.Empty<string>()).Where(tool => tool.Length > 0).Distinct().ToArray();
        public static string ChoiceError(CharacterCreationDraft d, int page = 3)
        {
            var background = Find(d); if (background == null) return "Обери передісторію.";
            if (page == 4) return background.items.Length > 0 && !background.items.Contains(d.backgroundItem) ? "Обери предмет передісторії." : null;
            if (NeedsGaming(d) && !new[] { "Гральні кості", "Карти", "Шахи" }.Contains(d.gamingSet)) return "Обери ігровий набір.";
            if (ToolOptions(d).Length > 0 && (!ToolOptions(d).Contains(d.backgroundTool) || ClassTools(d).Contains(d.backgroundTool))) return "Обери інструмент передісторії без повторів.";
            var duplicate = DuplicateTools(d);
            string[] replacements = (d.backgroundToolReplacements ?? Array.Empty<string>()).Take(duplicate.Length).ToArray();
            if (replacements.Length != duplicate.Length || replacements.Distinct().Count() != duplicate.Length || replacements.Any(tool => !AllTools.Contains(tool) || ClassTools(d).Contains(tool) || background.tools.Contains(tool) || tool == ToolChoice(d))) return "Заміни повторні володіння інструментами передісторії.";
            string[] languages = (d.backgroundLanguages ?? Array.Empty<string>()).Take(background.languages).ToArray();
            if (languages.Length != background.languages || languages.Distinct().Count() != background.languages || languages.Any(language => language.Length == 0 || BaseLanguages(d).Contains(language))) return "Обери різні мови передісторії без повторів.";
            return null;
        }
        public static string[] BaseLanguages(CharacterCreationDraft d) => CharacterSpeciesRules.AutomaticLanguages(d)
            .Concat(d.edition == RulesEdition.Rules2024 ? new[] { d.language, d.secondLanguage } : CharacterSpeciesRules.NeedsExtraLanguage(d) ? new[] { d.language } : new[] { CharacterSpeciesRules.FixedLanguage(d) })
            .Concat(CharacterLegacySpeciesRules.HalfElf(d) || CharacterElfRules.Elf(d) && !CharacterElfRules.Modern(d) && d.speciesLineage == "high" ? new[] { d.speciesLanguage } : Array.Empty<string>())
            .Concat(d.characterClass == "rogue" && d.edition == RulesEdition.Rules2024 ? new[] { d.rogueLanguage } : Array.Empty<string>())
            .Concat(d.characterClass == "ranger" && d.edition == RulesEdition.Rules2014 ? new[] { d.rangerLanguage } : Array.Empty<string>())
            .Concat(CharacterClericRules.Classic(d) && d.subclass == "knowledge" ? d.knowledgeLanguages ?? new List<string>() : Enumerable.Empty<string>())
            .Concat(CharacterSorcererRules.Draconic(d) ? new[] { "Драконяча" } : Array.Empty<string>()).Where(s => s.Length > 0).Distinct().ToArray();
        public static IEnumerable<string> Weapons(CharacterCreationDraft draft)
        {
            if (draft.edition != RulesEdition.Rules2024 || draft.backgroundEquipmentChoice != 0)
                return Array.Empty<string>();
            return draft.background switch
            {
                "criminal" or "sailor" or "wayfarer" => new[] { "Кинджал" },
                "farmer" => new[] { "Серп" },
                "guard" => new[] { "Спис", "Легкий арбалет" },
                "guide" => new[] { "Короткий лук" },
                "hermit" or "sage" => new[] { "Бойовий посох" },
                "soldier" => new[] { "Спис", "Короткий лук" },
                _ => Array.Empty<string>()
            };
        }

        public static string Equipment(CharacterCreationDraft d, Func<string,string> translate)
        {
            var background = Find(d); if (background == null) return "";
            if (background.modern && d.backgroundEquipmentChoice == 1) return translate("50 зм замість спорядження передісторії.");
            return string.Format(translate(background.equipment), translate(ToolChoice(d)), translate(d.backgroundItem));
        }
    }
}
