public static class CalculatorTextCatalog
{
    public static string Get(string key, AppLanguage language)
    {
        bool english = language == AppLanguage.English;
        bool russian = language == AppLanguage.Russian;

        switch (key)
        {
            case "choosePotion":
                return english ? "Choose potion" : russian ? "Выберите зелье" : "Оберіть зілля";
            case "noPotion":
                return english ? "No potion" : russian ? "Нет зелья" : "Немає зілля";
            case "potionError":
                return english ? "Potion error" : russian ? "Ошибка зелья" : "Помилка зілля";
            case "hpBarNotFound":
                return english ? "HP bar not found" : russian ? "HP бар не найден" : "HP бар не знайдено";
            case "tempHp":
                return english ? "Temp HP" : russian ? "Врем. HP" : "Тимч. HP";
            case "damageDone":
                return english ? "Damage taken" : russian ? "Получено урона" : "Отримано урону";
            case "healed":
                return english ? "Healed" : russian ? "Исцелено" : "Зцілено";
            case "longRest":
                return english ? "Long rest" : russian ? "Долгий отдых" : "Довгий відпочинок";
            case "shortRest":
                return english ? "Short rest" : russian ? "Короткий отдых" : "Короткий відпочинок";
            case "hitDiceNotFound":
                return english ? "Hit dice not found" : russian ? "Кости хитов не найдены" : "Кістки хітів не знайдено";
        }

        return key;
    }

}
