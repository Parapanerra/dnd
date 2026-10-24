using UnityEngine;
using UnityEngine.UI;

// Plays only for the newly marked exhaustion level; it does not change Toggle.isOn.
[RequireComponent(typeof(Toggle))]
[DisallowMultipleComponent]
public sealed class ExhaustionToggleAnimation : MonoBehaviour
{
    private const float Duration = 0.38f;

    private Toggle toggle;
    private Image mark;
    private Outline frameGlow;
    private Outline skullGlow;
    private Vector3 normalMarkScale;
    private Color glowColor;
    private bool isSkull;
    private bool initialized;
    private bool pulsing;
    private bool appearing;
    private float elapsed;

    private void Initialize()
    {
        if (initialized)
            return;

        toggle = GetComponent<Toggle>();
        mark = toggle.graphic as Image;
        Image frame = toggle.targetGraphic as Image;
        if (mark == null || frame == null)
            return;

        normalMarkScale = mark.rectTransform.localScale;
        isSkull = mark.sprite != null && mark.sprite.name == "_706e47a1-617d-47af-9d3c-0e59290ddaa4_0";
        Color markColor = mark.color;
        float peak = Mathf.Max(markColor.r, Mathf.Max(markColor.g, markColor.b));
        glowColor = peak > 0f ? new Color(markColor.r / peak, markColor.g / peak, markColor.b / peak, 0f)
            : new Color(1f, 0.9f, 0.7f, 0f);
        if (isSkull)
            glowColor = new Color(1f, 0.88f, 0.65f, 0f);

        frameGlow = frame.GetComponent<Outline>();
        if (frameGlow == null)
            frameGlow = frame.gameObject.AddComponent<Outline>();
        frameGlow.effectDistance = new Vector2(3f, 3f);
        frameGlow.useGraphicAlpha = false;
        frameGlow.enabled = false;

        if (isSkull)
        {
            skullGlow = mark.GetComponent<Outline>();
            if (skullGlow == null)
                skullGlow = mark.gameObject.AddComponent<Outline>();
            skullGlow.effectDistance = new Vector2(2f, 2f);
            skullGlow.useGraphicAlpha = false;
            skullGlow.enabled = false;
        }

        initialized = true;
    }

    public void Pulse()
    {
        Initialize();
        if (!initialized || !toggle.isOn)
            return;

        appearing = true;
        elapsed = 0f;
        pulsing = true;
        frameGlow.enabled = true;
        if (skullGlow != null)
            skullGlow.enabled = true;
        mark.canvasRenderer.SetAlpha(0f);
        mark.rectTransform.localScale = normalMarkScale * 0.7f;
    }

    public void PulseRemoval()
    {
        Initialize();
        if (!initialized || toggle.isOn)
            return;

        appearing = false;
        elapsed = 0f;
        pulsing = true;
        frameGlow.enabled = true;
        if (skullGlow != null)
            skullGlow.enabled = true;
        mark.canvasRenderer.SetAlpha(1f);
        mark.rectTransform.localScale = normalMarkScale;
    }

    private void LateUpdate()
    {
        if (!pulsing)
            return;

        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, Duration);
        float progress = elapsed / Duration;
        float eased = progress * progress * (3f - 2f * progress);
        float pulse = Mathf.Sin(progress * Mathf.PI);
        mark.canvasRenderer.SetAlpha(appearing ? eased : 1f - eased);
        mark.rectTransform.localScale = normalMarkScale * (appearing
            ? Mathf.Lerp(0.7f, 1f, eased) + 0.18f * pulse
            : Mathf.Lerp(1f, 0.7f, eased));

        Color color = glowColor;
        color.a = (appearing ? 0.75f : 0.5f) * pulse;
        frameGlow.effectColor = color;
        if (skullGlow != null)
        {
            color.a = (appearing ? 0.5f : 0.35f) * pulse;
            skullGlow.effectColor = color;
        }

        if (elapsed < Duration)
            return;

        pulsing = false;
        frameGlow.enabled = false;
        if (skullGlow != null)
            skullGlow.enabled = false;
        mark.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
        mark.rectTransform.localScale = normalMarkScale;
    }

    private void OnDisable()
    {
        pulsing = false;
        if (!initialized)
            return;
        frameGlow.enabled = false;
        if (skullGlow != null)
            skullGlow.enabled = false;
        mark.rectTransform.localScale = normalMarkScale;
        mark.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
    }
}
