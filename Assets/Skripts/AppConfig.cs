using UnityEngine;

public static class AppConfig
{
    internal static class Scenes
    {
        public const string Menu = "menu";
        public const string CharacterCreation = "CharacterCreationPrototype";
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
    }

    internal static class Localization
    {
        public const string LanguagePrefsKey = "DndAppLanguage";
        public const int TranslationColumnCount = 3;
        public const float SceneRefreshDelaySeconds = 0.1f;
    }

    internal static class Input
    {
        public const float DoubleClickTimeSeconds = 0.35f;
        public const float PinchZoomSensitivity = 0.01f;
        public const float DefaultMinimumZoom = 1f;
        public const float DefaultMaximumZoom = 8f;
    }

    internal static class Images
    {
        public const int TextureBootstrapSize = 2;
        public const int GalleryPreviewMaxSize = 1024;
        public const int InventoryImageSize = 256;
        public const int InventoryJpgQuality = 75;
        public const int PortraitWidth = 300;
        public const int PortraitHeight = 400;
        public const int PortraitJpgQuality = 80;
    }

    internal static class Calculator
    {
        public const int MinimumDiceCount = 1;
        public const int MaximumDiceCount = 100;
        public const int MinimumDiceSides = 1;
        public const int MaximumDiceSides = 1000;
    }

    internal static class Inventory
    {
        public const int WeaponCategory = (int)InventoryItemCategory.Weapon;
        public const int ArmorCategory = (int)InventoryItemCategory.Armor;
        public const int BagsCategory = (int)InventoryItemCategory.Bags;
        public const int MagicCategory = (int)InventoryItemCategory.Magic;
        public const int OtherCategory = (int)InventoryItemCategory.Other;
        public const int ChegerCategory = (int)InventoryItemCategory.Cheger;
        public const int CustomCategory = (int)InventoryItemCategory.Custom;
    }

    internal static class SaveData
    {
        public const float CharacterCreateDebounceSeconds = 0.5f;
        public const float AutoSaveDebounceSeconds = 0.8f;
    }

    public static class MainMenu
    {
        internal const float CharacterRowSpacing = 12f;
        internal const float CharacterButtonSpacing = 12f;
        internal const float CharacterButtonHeight = 95f;
        internal const float ContentBottomPadding = 24f;
        internal const float MenuScrollSensitivity = 18f;
        internal const float MinimumUsableRectSize = 1f;

        internal const int ImportedNameFirstSuffix = 2;
        internal const int DropdownTemplateHeight = 90;
        internal const int DropdownLabelHeight = 28;
        internal const int DropdownItemHeight = 24;
        internal const int DropdownHorizontalTextPadding = 8;
        public const int DefaultLayoutPadding = 12;
        internal const int DefaultLayoutTopPadding = 84;
        internal const int MinimumVisibleScrollSize = 320;
        internal const int DefaultVisibleScrollWidth = 620;
        internal const int DefaultVisibleScrollHeight = 980;
        internal const int CharacterNameFontSize = 28;
        internal const int CharacterNameMinimumFontSize = 16;
        internal const int CharacterTextLeftPadding = 24;
        internal const int CharacterTextRightPadding = 90;
        internal const int CharacterTextVerticalPadding = 8;
        internal const int DeleteButtonFontSize = 28;

        internal static readonly Vector2 DeleteButtonPosition = new Vector2(-45f, 0f);
        internal static readonly Vector2 DeleteButtonSize = new Vector2(58f, 58f);

        public static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
        internal static readonly Color ActiveCharacterColor = new Color(0.28f, 0.22f, 0.12f, 0.98f);
        internal static readonly Color InactiveCharacterColor = new Color(0.18f, 0.14f, 0.1f, 0.95f);
        internal static readonly Color DeleteButtonColor = new Color(0.55f, 0.12f, 0.1f, 0.95f);
    }
}
