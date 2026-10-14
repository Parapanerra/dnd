using UnityEngine;

// Serialized by the scene migration; the order is independent of visual names.
[DisallowMultipleComponent]
public sealed class ToggleSequenceConfig : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Toggle[] toggles;
    [SerializeField] private bool clampToActive;
    [SerializeField] private bool appliesExhaustion;
    public UnityEngine.UI.Toggle[] Toggles => toggles;
    public void Configure(UnityEngine.UI.Toggle[] ordered, bool clamp, bool exhaustion)
    {
        toggles = ordered;
        clampToActive = clamp;
        appliesExhaustion = exhaustion;
    }
    internal void Install(ExhaustionEffects exhaustion)
    {
        DeathSaveToggleSequence sequence = GetComponent<DeathSaveToggleSequence>();
        if (sequence == null) sequence = gameObject.AddComponent<DeathSaveToggleSequence>();
        sequence.Configure(toggles, clampToActive, appliesExhaustion ? exhaustion : null);
    }
}
