using UnityEngine;

// Assigned once by the Editor from the frozen A0 map. Never generated at runtime.
public sealed class PersistentFieldId : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private Component control;
    [SerializeField] private string legacyCollection;
    [SerializeField] private int legacyIndex = -1;
    [SerializeField] private string legacyKey;

    public string Id => id;
    public Component Control => control;
    public string LegacyCollection => legacyCollection;
    public int LegacyIndex => legacyIndex;
    public string LegacyKey => legacyKey;

#if UNITY_EDITOR
    public void Configure(string stableId, Component target, string collection, int index, string key)
    {
        id = stableId;
        control = target;
        legacyCollection = collection;
        legacyIndex = index;
        legacyKey = key;
    }
#endif

    public static PersistentFieldId For(Component target)
    {
        if (target == null) return null;
        foreach (var field in target.GetComponents<PersistentFieldId>())
            if (field.control == target && !string.IsNullOrEmpty(field.id))
                return field;
        return null;
    }
}
