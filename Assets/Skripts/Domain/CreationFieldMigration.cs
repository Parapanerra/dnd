namespace Taruck.CharacterCreation
{
    internal static class CreationFieldMigration
    {
        internal static void MigrateBloodDieBinding(CharacterData character)
        {
            if (character == null || character.GetSharedString("CreationBloodDieBinding_v2", "") == "1") return;
            string json = character.GetSharedString("CreationWizard_v1", "");
            if (!json.Contains("\"characterClass\":\"bloodhunter\"")) return;
            const string dieKey = "StableField_v1_dnd.cartapersonaj.inputfield.inputfield-legacy.1e98bc89680f";
            const string breathKey = "StableField_v1_dnd.cartapersonaj.inputfield.inputfield-legacy.37c97c5670cd";
            var sheet = character.GetSceneData("cartaPersonaj", false);
            if (sheet == null) return;
            string die = sheet.GetString(dieKey, sheet.inputData.Count > 45 ? sheet.inputData[45] : "");
            string breath = sheet.GetString(breathKey, sheet.inputData.Count > 47 ? sheet.inputData[47] : "");
            // Correct only the exact generated mistake, preserving subsequent manual edits.
            if (string.IsNullOrWhiteSpace(die) && breath == "1d4")
            {
                sheet.SetString(dieKey, breath); sheet.SetString(breathKey, "");
                while (sheet.inputData.Count <= 47) sheet.inputData.Add("");
                sheet.inputData[45] = breath; sheet.inputData[47] = "";
            }
            character.SetSharedString("CreationBloodDieBinding_v2", "1");
        }

        internal static void MigrateRageCountBinding(CharacterData character)
        {
            if (character == null || character.GetSharedString("CreationRageCountBinding_v2", "") == "1") return;
            var sheet = character.GetSceneData("cartaPersonaj", false);
            const string oldKey = "StableField_v1_creation.rage.count";
            const string newKey = "StableField_v1_dnd.cartapersonaj.dropdown.dropdown-legacy-2.0a3575706a02";
            if (sheet != null && sheet.HasInt(oldKey))
            {
                int value = System.Math.Clamp(sheet.GetInt(oldKey), 0, 7);
                sheet.SetInt(newKey, value);
                if (sheet.dropdownData.Count > 6) sheet.dropdownData[6] = value;
            }
            character.SetSharedString("CreationRageCountBinding_v2", "1");
        }

    }
}
