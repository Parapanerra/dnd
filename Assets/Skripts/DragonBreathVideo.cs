using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public sealed class DragonBreathVideo : ResourceVideoPlaybackBase
{
    private void Awake()
    {
        ResourceVideoToggleClick relay = GetComponent<ResourceVideoToggleClick>();
        if (relay == null)
            relay = gameObject.AddComponent<ResourceVideoToggleClick>();
        relay.Initialize(this);
    }
}
