using System;
using System.Collections.Generic;
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
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private string healthPrefix;
        [SerializeField] private string formHealthPrefix;

        public IReadOnlyList<Entry> Entries => entries;
        internal string HealthPrefix => healthPrefix;
        internal string FormHealthPrefix => formHealthPrefix;

#if UNITY_EDITOR
        public void ReplaceEntries(Entry[] value) => entries = value;
        public void SetHealthPrefix(string value) => healthPrefix = value;
        public void SetFormHealthPrefix(string value) => formHealthPrefix = value;
#endif

        internal void Text(CharacterData character, string key, string value, string sceneOverride = null)
        {
            foreach (Entry e in entries)
            {
                if (e.key != key || e.collection != "inputData") continue;
                CharacterSceneData data = character.GetSceneData(sceneOverride ?? e.scene);
                data.SetString("StableField_v1_" + e.stableId, value);
                while (data.inputData.Count <= e.index) data.inputData.Add("");
                if (e.index >= 0) data.inputData[e.index] = value;
                data.stableFieldVersion = 1;
            }
        }
        internal void Toggle(CharacterData character, string key, bool value, string sceneOverride = null)
        {
            foreach (Entry e in entries)
            {
                if (e.key != key || e.collection != "toggleData") continue;
                CharacterSceneData data = character.GetSceneData(sceneOverride ?? e.scene);
                data.SetInt("StableField_v1_" + e.stableId, value ? 1 : 0);
                while (data.toggleData.Count <= e.index) data.toggleData.Add(false);
                if (e.index >= 0) data.toggleData[e.index] = value;
                data.stableFieldVersion = 1;
            }
        }

        internal void Dropdown(CharacterData character, string key, int value, string sceneOverride = null)
        {
            foreach (Entry e in entries)
            {
                if (e.key != key || e.collection != "dropdownData") continue;
                CharacterSceneData data = character.GetSceneData(sceneOverride ?? e.scene);
                data.SetInt("StableField_v1_" + e.stableId, value);
                while (data.dropdownData.Count <= e.index) data.dropdownData.Add(0);
                if (e.index >= 0) data.dropdownData[e.index] = value;
            }
        }
    }
}
