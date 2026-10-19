using UnityEngine;

// A drag that begins in this scroll view belongs to the UI, not the map camera.
public sealed class CameraPanBlockOnScrollView : MonoBehaviour
{
    [SerializeField] private bool blockVerticalOnly;
    public bool BlockVerticalOnly => blockVerticalOnly;
}
