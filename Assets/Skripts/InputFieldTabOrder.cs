using UnityEngine;

[AddComponentMenu("UI/Input Field Tab Order")]
[DisallowMultipleComponent]
public sealed class InputFieldTabOrder : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Tab order in this scene. Use 0 to exclude this field from Tab navigation.")]
    [SerializeField] private int tabOrder;

    public int Order
    {
        get => Mathf.Max(0, tabOrder);
        set => tabOrder = Mathf.Max(0, value);
    }
}
