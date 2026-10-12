internal sealed class HealingPotionCatalog
{
    private readonly PotionDefinition[] potions =
    {
        new PotionDefinition("2d4+2", "Зілля лікування", "Potion of Healing", "Зелье лечения"),
        new PotionDefinition("4d4+4", "Велике зілля лікування", "Potion of Greater Healing", "Большое зелье лечения"),
        new PotionDefinition("8d4+8", "Покращене зілля лікування", "Potion of Superior Healing", "Улучшенное зелье лечения"),
        new PotionDefinition("10d4+20", "Найвище зілля лікування", "Potion of Supreme Healing", "Высшее зелье лечения")
    };

    internal int Count => potions.Length;

    internal string GetFormula(int index)
    {
        return potions[index].Formula;
    }

    internal string GetName(int index, AppLanguage language)
    {
        if (index < 0 || index >= potions.Length)
            return "Зілля";

        PotionDefinition potion = potions[index];
        switch (language)
        {
            case AppLanguage.English:
                return potion.EnglishName;
            case AppLanguage.Russian:
                return potion.RussianName;
            default:
                return potion.UkrainianName;
        }
    }

    private sealed class PotionDefinition
    {
        internal readonly string Formula;
        internal readonly string UkrainianName;
        internal readonly string EnglishName;
        internal readonly string RussianName;

        internal PotionDefinition(string formula, string ukrainianName, string englishName, string russianName)
        {
            Formula = formula;
            UkrainianName = ukrainianName;
            EnglishName = englishName;
            RussianName = russianName;
        }
    }
}