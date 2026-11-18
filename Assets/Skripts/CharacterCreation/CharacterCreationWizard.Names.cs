using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private bool feminineName;

        private void DrawNameControls()
        {
            TMP_InputField field = ui.Input(content, draft.characterName, "Введи ім’я…", SetCharacterName);
            Button roll = ui.Button(field.transform.parent, L("Кинути кубик"), () =>
            {
                string name = CharacterNameGenerator.Roll(shownLanguage, feminineName, draft.characterName);
                field.SetTextWithoutNotify(name);
                SetCharacterName(name);
            }, height: 72);
            roll.transform.SetSiblingIndex(field.transform.GetSiblingIndex() + 1);
            var buttonLayout = roll.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = 156;
            buttonLayout.minWidth = 116;
            var nameLayout = field.GetComponent<LayoutElement>();
            nameLayout.minWidth = 120;
            var label = roll.GetComponentInChildren<TMP_Text>();
            label.rectTransform.offsetMin = new Vector2(41, label.rectTransform.offsetMin.y);
            DrawDiceIcon(roll.transform);

            DropdownInt("Варіант імені", new[] { 0, 1 }, feminineName ? 1 : 0,
                value => feminineName = value == 1, value => value == 1 ? "Жіноче" : "Чоловіче", 210);
        }

        private void SetCharacterName(string value)
        {
            draft.characterName = value;
            if (nameError != null && !string.IsNullOrWhiteSpace(value)) nameError.text = "";
            UpdateSummary();
        }

        private void DrawDiceIcon(Transform button)
        {
            var die = ui.Rect("DiceIcon", button);
            die.anchorMin = die.anchorMax = new Vector2(0, .5f);
            die.anchoredPosition = new Vector2(25, 0);
            die.sizeDelta = new Vector2(21, 21);
            var face = die.gameObject.AddComponent<Image>();
            face.color = CreationWizardUi.Gold;
            face.raycastTarget = false;
            foreach (Vector2 position in new[] { new Vector2(-5, 5), new Vector2(5, 5), Vector2.zero, new Vector2(-5, -5), new Vector2(5, -5) })
            {
                var pip = ui.Rect("Pip", die);
                pip.anchorMin = pip.anchorMax = new Vector2(.5f, .5f);
                pip.anchoredPosition = position;
                pip.sizeDelta = new Vector2(3, 3);
                var image = pip.gameObject.AddComponent<Image>();
                image.color = new Color(.12f, .09f, .08f);
                image.raycastTarget = false;
            }
        }
    }

    internal static class CharacterNameGenerator
    {
        private static readonly string[][] Masculine =
        {
            new[]
            {
                "Аларік", "Борин", "Валдрін", "Гален", "Даріон", "Ейран", "Каел", "Лоркан",
                "Мірек", "Орен", "Рован", "Терен", "Фарен", "Ярек", "Адран", "Бранн",
                "Варек", "Гаррік", "Дорнан", "Едрік", "Зарен", "Івар", "Касіан", "Леор",
                "Малвен", "Норік", "Ордел", "Ріан", "Севран", "Талрен", "Ульмар", "Фенн",
                "Халдор", "Елден", "Астор", "Велор", "Альден", "Арвен", "Берен", "Бріан",
                "Вейрон", "Гарвен", "Дейран", "Дункан", "Ерван", "Жерар", "Кайран", "Келдор",
                "Леван", "Лукан", "Маел", "Олдрін", "Перін", "Рейнар", "Рунар", "Сайлас",
                "Тавін", "Торен", "Фінтан", "Харлан", "Цедрік", "Емріс", "Ярвен", "Зевран",
            },
            new[]
            {
                "Alaric", "Borin", "Valdrin", "Galen", "Darion", "Eiran", "Kael", "Lorcan",
                "Mirek", "Oren", "Rowan", "Teren", "Faren", "Yarek", "Adran", "Brann",
                "Varek", "Garrick", "Dornan", "Edric", "Zaren", "Ivar", "Cassian", "Leor",
                "Malven", "Norik", "Ordel", "Rian", "Sevran", "Talren", "Ulmar", "Fenn",
                "Haldor", "Elden", "Astor", "Velor", "Alden", "Arven", "Beren", "Briann",
                "Veyron", "Garven", "Deyran", "Duncan", "Ervan", "Gerard", "Kairan", "Keldor",
                "Levan", "Lucan", "Mael", "Aldrin", "Perrin", "Reynar", "Runar", "Silas",
                "Tavin", "Thoren", "Fintan", "Harlan", "Cedric", "Emrys", "Yarven", "Zevran",
            },
            new[]
            {
                "Аларик", "Борин", "Валдрин", "Гален", "Дарион", "Эйран", "Каэл", "Лоркан",
                "Мирек", "Орен", "Рован", "Терен", "Фарен", "Ярек", "Адран", "Бранн",
                "Варек", "Гаррик", "Дорнан", "Эдрик", "Зарен", "Ивар", "Кассиан", "Леор",
                "Малвен", "Норик", "Ордел", "Риан", "Севран", "Талрен", "Ульмар", "Фенн",
                "Халдор", "Элден", "Астор", "Велор", "Альден", "Арвен", "Берен", "Брианн",
                "Вейрон", "Гарвен", "Дейран", "Дункан", "Эрван", "Жерар", "Кайран", "Келдор",
                "Леван", "Лукан", "Маэл", "Олдрин", "Перрин", "Рейнар", "Рунар", "Сайлас",
                "Тавин", "Торен", "Финтан", "Харлан", "Цедрик", "Эмрис", "Ярвен", "Зевран",
            }
        };
        private static readonly string[][] Feminine =
        {
            new[]
            {
                "Аделін", "Бріна", "Вейла", "Галія", "Дарена", "Елара", "Ілліра", "Каелін",
                "Ліора", "Мейра", "Неріса", "Руна", "Селін", "Талія", "Авеліна", "Астра",
                "Бренна", "Валена", "Гвена", "Делія", "Ейла", "Зара", "Іріана", "Каліста",
                "Лавена", "Мірена", "Найра", "Орла", "Ровена", "Сілена", "Тірса", "Унна",
                "Фрейя", "Хільда", "Елісса", "Ясміра", "Альва", "Амаріс", "Аріель", "Бріель",
                "Віана", "Гейзел", "Дафна", "Ейрін", "Енна", "Жасміра", "Зоряна", "Ізольда",
                "Іона", "Кайра", "Кора", "Ліннея", "Люсена", "Меріель", "Морвен", "Найла",
                "Нів", "Оріана", "Ренна", "Саела", "Серен", "Тея", "Фіона", "Ейвен",
            },
            new[]
            {
                "Adeline", "Brina", "Veyla", "Galia", "Darena", "Elara", "Illyra", "Kaelin",
                "Liora", "Meyra", "Nerissa", "Runa", "Celine", "Talia", "Avelina", "Astra",
                "Brenna", "Valena", "Gwena", "Delia", "Ayla", "Zara", "Iriana", "Calista",
                "Lavena", "Mirena", "Naira", "Orla", "Rowena", "Silena", "Tirsa", "Unna",
                "Freya", "Hilda", "Elissa", "Yasmira", "Alva", "Amaris", "Arielle", "Brielle",
                "Viana", "Hazel", "Daphne", "Eirin", "Enna", "Jasmira", "Zoryana", "Isolde",
                "Iona", "Kaira", "Cora", "Linnea", "Lucena", "Meriel", "Morwen", "Nayla",
                "Niamh", "Oriana", "Renna", "Saela", "Seren", "Thea", "Fiona", "Aven",
            },
            new[]
            {
                "Аделин", "Брина", "Вейла", "Галия", "Дарена", "Элара", "Иллира", "Каэлин",
                "Лиора", "Мейра", "Нерисса", "Руна", "Селин", "Талия", "Авелина", "Астра",
                "Бренна", "Валена", "Гвена", "Делия", "Эйла", "Зара", "Ириана", "Калиста",
                "Лавена", "Мирена", "Найра", "Орла", "Ровена", "Силена", "Тирса", "Унна",
                "Фрейя", "Хильда", "Элисса", "Ясмира", "Альва", "Амарис", "Ариэль", "Бриэль",
                "Виана", "Хейзел", "Дафна", "Эйрин", "Энна", "Жасмира", "Зоряна", "Изольда",
                "Иона", "Кайра", "Кора", "Линнея", "Люсена", "Мериэль", "Морвен", "Найла",
                "Нив", "Ориана", "Ренна", "Саэла", "Серен", "Тея", "Фиона", "Эйвен",
            }
        };

        internal static string Roll(AppLanguage language, bool feminine, string current)
        {
            int locale = language == AppLanguage.English ? 1 : language == AppLanguage.Russian ? 2 : 0;
            string[] choices = (feminine ? Feminine : Masculine)[locale];
            int previous = Array.IndexOf(choices, current);
            int index = UnityEngine.Random.Range(0, previous < 0 ? choices.Length : choices.Length - 1);
            return choices[previous >= 0 && index >= previous ? index + 1 : index];
        }
    }
}
