using System.Collections.Generic;
using UnityEngine;

public static class AppConfig
{
    public static class Scenes
    {
        public const string Menu = "menu";
        public const string CharacterSheet = "cartaPersonaj";
        public const string Inventory = "inventory";
        public const string Spellbook = "spelBook";
        public const string Spells = "Spels";
        public const string CharacterInfo = "informForPerson";
        public const string Notes = "zapisnuk";
        public const string WildShape = "petsesn";
        public const string Authors = "avtoru";
        public const string About = "proApk";

        public const int WildShapePageCount = 7;
        public const int WildShapePagesPerCharacterGroup = 4;

        private static readonly Dictionary<int, string> LegacySceneNames = new Dictionary<int, string>
        {
            { 0, Menu },
            { 1, CharacterSheet }, { 10, CharacterSheet }, { 11, CharacterSheet },
            { 12, CharacterSheet }, { 13, CharacterSheet },
            { 2, WildShape }, { 17, WildShape },
            { 3, Spells }, { 16, Spells }, { 23, Spells }, { 30, Spells }, { 37, Spells },
            { 4, WildShape + " 1" }, { 18, WildShape + " 1" },
            { 5, WildShape + " 2" }, { 19, WildShape + " 2" },
            { 6, WildShape + " 3" }, { 20, WildShape + " 3" },
            { 7, Inventory }, { 15, Inventory }, { 22, Inventory },
            { 29, Inventory }, { 36, Inventory },
            { 8, CharacterInfo }, { 14, CharacterInfo }, { 21, CharacterInfo },
            { 28, CharacterInfo }, { 35, CharacterInfo },
            { 9, Notes },
            { 24, WildShape + " 4" }, { 31, WildShape + " 4" }, { 38, WildShape + " 4" },
            { 49, WildShape + " 4" }, { 53, WildShape + " 4" },
            { 25, WildShape + " 5" }, { 32, WildShape + " 5" }, { 39, WildShape + " 5" },
            { 50, WildShape + " 5" }, { 54, WildShape + " 5" },
            { 26, WildShape + " 6" }, { 33, WildShape + " 6" }, { 40, WildShape + " 6" },
            { 51, WildShape + " 6" }, { 55, WildShape + " 6" },
            { 27, WildShape + " 7" }, { 34, WildShape + " 7" }, { 41, WildShape + " 7" },
            { 52, WildShape + " 7" }, { 56, WildShape + " 7" },
            { 42, Authors },
            { 43, About },
            { 44, Spellbook }, { 45, Spellbook }, { 46, Spellbook },
            { 47, Spellbook }, { 48, Spellbook },
            { 57, WildShape + " 4" }, { 61, WildShape + " 4" }, { 65, WildShape + " 4" },
            { 58, WildShape + " 5" }, { 62, WildShape + " 5" }, { 66, WildShape + " 5" },
            { 59, WildShape + " 6" }, { 63, WildShape + " 6" }, { 67, WildShape + " 6" },
            { 60, WildShape + " 7" }, { 64, WildShape + " 7" }, { 68, WildShape + " 7" }
        };

        public static bool TryGetLegacySceneName(int buildIndex, out string sceneName)
        {
            return LegacySceneNames.TryGetValue(buildIndex, out sceneName);
        }
    }

    public static class Localization
    {
        public const string LanguagePrefsKey = "DndAppLanguage";
        public const int MinimumLanguageIndex = 0;
        public const int MaximumLanguageIndex = 2;
        public const int TranslationColumnCount = 3;
        public const float SceneRefreshDelaySeconds = 0.1f;
    }

    public static class Input
    {
        public const float DoubleClickTimeSeconds = 0.35f;
        public const float PinchZoomSensitivity = 0.01f;
        public const float DefaultMinimumZoom = 1f;
        public const float DefaultMaximumZoom = 8f;
    }

    public static class Images
    {
        public const int TextureBootstrapSize = 2;
        public const int GalleryPreviewMaxSize = 1024;
        public const int InventoryImageSize = 256;
        public const int InventoryJpgQuality = 75;
        public const int PortraitWidth = 300;
        public const int PortraitHeight = 400;
        public const int PortraitJpgQuality = 80;
    }

    public static class Calculator
    {
        public const int PotionTypeCount = 4;
        public const int MinimumDiceCount = 1;
        public const int MaximumDiceCount = 100;
        public const int MinimumDiceSides = 1;
        public const int MaximumDiceSides = 1000;
        public const int ExhaustionToggleLastIndex = 5;
        public const int BloodHunterPrimaryToggleLastIndex = 7;
        public const int BloodHunterSecondaryToggleFirstIndex = 8;
        public const int BloodHunterSecondaryToggleLastIndex = 11;
        public const float ShortRestDiceDivisor = 2f;
        public const string BloodHunterPanelName = "Panel (5)";

        public static readonly string[] PotionFormulas =
        {
            "2d4+2",
            "4d4+4",
            "8d4+8",
            "10d4+20"
        };
    }

    public static class Inventory
    {
        public const int WeaponCategory = 0;
        public const int ArmorCategory = 1;
        public const int BagsCategory = 2;
        public const int MagicCategory = 3;
        public const int OtherCategory = 4;
        public const int ChegerCategory = 5;
        public const int CustomCategory = 6;
        public const int PageSortMultiplier = 100;
    }

    public static class SaveData
    {
        public const float CharacterCreateDebounceSeconds = 0.5f;
    }

    public static class ToggleSequences
    {
        public const int ArtifactSkillMaximumToggle = 4;
        public const int ArtifactInfusionMaximumToggle = 7;
        public const int BloodHunterMaximumToggle = 7;
        public const int WildShapeButtonCount = 8;

        public static readonly int[] SpellCheckToggleOrder = { 2, 1, 0, 3, 4, 5, 6, 7 };
    }

    public static class MainMenu
    {
        public const float CharacterRowSpacing = 12f;
        public const float CharacterButtonSpacing = 12f;
        public const float CharacterButtonHeight = 95f;
        public const float CharacterCreateDebounceSeconds = 0.5f;
        public const float ContentBottomPadding = 24f;
        public const float MenuScrollSensitivity = 18f;
        public const float MinimumUsableRectSize = 1f;

        public const int DefaultButtonFontSize = 24;
        public const int DefaultButtonMinimumFontSize = 10;
        public const int DefaultButtonMaximumFontSize = 28;
        public const int ImportedNameFirstSuffix = 2;
        public const int DropdownTemplateHeight = 90;
        public const int DropdownLabelHeight = 28;
        public const int DropdownItemHeight = 24;
        public const int DropdownHorizontalTextPadding = 8;
        public const int DefaultLayoutPadding = 12;
        public const int DefaultLayoutTopPadding = 84;
        public const int MinimumVisibleScrollSize = 320;
        public const int DefaultVisibleScrollWidth = 620;
        public const int DefaultVisibleScrollHeight = 980;
        public const int CharacterNameFontSize = 28;
        public const int CharacterNameMinimumFontSize = 16;
        public const int CharacterTextLeftPadding = 24;
        public const int CharacterTextRightPadding = 90;
        public const int CharacterTextVerticalPadding = 8;
        public const int DeleteButtonFontSize = 28;

        public static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);
        public static readonly Vector2 CharacterScrollSize = new Vector2(560f, 520f);
        public static readonly Vector2 CharacterScrollPosition = new Vector2(0f, -165f);
        public static readonly Vector2 AddButtonSize = new Vector2(310f, 58f);
        public static readonly Vector2 AddButtonPosition = new Vector2(0f, -292f);
        public static readonly Vector2 CharacterRowSize = new Vector2(560f, 78f);
        public static readonly Vector2 RowActionButtonSize = new Vector2(66f, 66f);
        public static readonly Vector2 CharacterNameButtonSize = new Vector2(330f, 66f);
        public static readonly Vector2 InventoryButtonPosition = new Vector2(-235f, 0f);
        public static readonly Vector2 CharacterButtonPosition = new Vector2(-25f, 0f);
        public static readonly Vector2 SpellsButtonPosition = new Vector2(185f, 0f);
        public static readonly Vector2 DeleteRowButtonPosition = new Vector2(265f, 0f);
        public static readonly Vector2 DeleteButtonPosition = new Vector2(-45f, 0f);
        public static readonly Vector2 DeleteButtonSize = new Vector2(58f, 58f);

        public static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
        public static readonly Color DefaultButtonColor = new Color(0.12f, 0.09f, 0.07f, 0.9f);
        public static readonly Color DefaultButtonTextColor = new Color(0.95f, 0.55f, 0.05f, 1f);
        public static readonly Color ActiveCharacterColor = new Color(0.28f, 0.22f, 0.12f, 0.98f);
        public static readonly Color InactiveCharacterColor = new Color(0.18f, 0.14f, 0.1f, 0.95f);
        public static readonly Color DeleteButtonColor = new Color(0.55f, 0.12f, 0.1f, 0.95f);
    }

    public static class EditorLayout
    {
        public static readonly Vector2 MenuScrollPosition = new Vector2(0f, -120f);
        public static readonly Vector2 MenuScrollSize = new Vector2(620f, 980f);
    }
}
