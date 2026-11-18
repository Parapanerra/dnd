namespace Taruck.CharacterCreation
{
    internal static class CharacterArtificerSpellText
    {
        internal static string Effect(string name)
        {
            switch (name)
            {
                case "Homunculus Servant": return CharacterArtificerText.Get("spell.homunculus");
                case "Booming Blade": return "Ближня атака зброєю по цілі за 5 футів зі звичайною шкодою. Якщо вона добровільно переміститься на 5 футів до твого наступного ходу, отримає 1к8 громової шкоди. Потрібна зброя ціною від 1 ср.";
                case "Green-Flame Blade": return "Ближня атака зброєю по цілі за 5 футів зі звичайною шкодою; інша видима істота за 5 футів від неї отримує вогняну шкоду за модифікатором магічної характеристики. Потрібна зброя ціною від 1 ср.";
                case "Lightning Lure": return "Істота за 15 футів: ряткидок Сили або підтягування до 10 футів до тебе; якщо опиниться за 5 футів, отримує 1к8 блискавичної шкоди.";
                case "Sword Burst": return "Інші істоти за 5 футів: ряткидок Спритності або 1к6 силової шкоди.";
                case "Tasha's Caustic Brew": return "Лінія кислоти 30 × 5 футів: ряткидок Спритності або 2к4 кислотної шкоди на початку кожного ходу. Дією можна змити кислоту із себе або іншої істоти.";
                default: return "";
            }
        }
    }
}
