using System;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    public sealed class CreationSheetBindings : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string key, scene, stableId, collection;
            public int index;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public string healthPrefix;

        public void Text(CharacterData character, string key, string value)
        {
            foreach (Entry e in entries)
            {
                if (e.key != key || e.collection != "inputData") continue;
                CharacterSceneData data = character.GetSceneData(e.scene);
                data.SetString("StableField_v1_" + e.stableId, value);
                while (data.inputData.Count <= e.index) data.inputData.Add("");
                if (e.index >= 0) data.inputData[e.index] = value;
                data.stableFieldVersion = 1;
            }
        }
        public void Toggle(CharacterData character, string key, bool value)
        {
            foreach (Entry e in entries)
            {
                if (e.key != key || e.collection != "toggleData") continue;
                CharacterSceneData data = character.GetSceneData(e.scene);
                data.SetInt("StableField_v1_" + e.stableId, value ? 1 : 0);
                while (data.toggleData.Count <= e.index) data.toggleData.Add(false);
                if (e.index >= 0) data.toggleData[e.index] = value;
                data.stableFieldVersion = 1;
            }
        }
    }
}
