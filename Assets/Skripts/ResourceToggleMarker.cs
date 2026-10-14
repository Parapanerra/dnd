using UnityEngine;

// Keeps the legacy resource number stable when its GameObject is renamed.
[DisallowMultipleComponent]
[RequireComponent(typeof(UnityEngine.UI.Toggle))]
public sealed class ResourceToggleMarker : MonoBehaviour
{
    [SerializeField] private int number;
    public int Number => number;
    public void Configure(int value) => number = value;
    public static int NumberOf(UnityEngine.UI.Toggle toggle)
    {
        ResourceToggleMarker marker = toggle.GetComponent<ResourceToggleMarker>();
        return marker != null ? marker.Number : SceneObjectName.ToggleNumber(toggle.name);
    }
}
