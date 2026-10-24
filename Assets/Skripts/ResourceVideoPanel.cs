using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ResourceVideoPanel : ResourceVideoPlaybackBase
{
    private void Awake()
    {
        foreach (Toggle toggle in GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.GetComponent<ResourceToggleMarker>() == null)
                continue;

            ResourceVideoToggleClick relay = toggle.GetComponent<ResourceVideoToggleClick>();
            if (relay == null)
                relay = toggle.gameObject.AddComponent<ResourceVideoToggleClick>();
            relay.Initialize(this);
        }
    }
}
