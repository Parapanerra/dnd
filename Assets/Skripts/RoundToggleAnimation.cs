using UnityEngine;
using UnityEngine.UI;

// Visual feedback only. Toggle state, order, and persistence remain with the existing controllers.
[RequireComponent(typeof(Toggle))]
[DisallowMultipleComponent]
public sealed class RoundToggleAnimation : MonoBehaviour
{
    private const float Duration = 0.34f;

    private Toggle toggle;
    private RectTransform mark;
    private RectTransform frame;
    private Vector3 markScale;
    private Vector3 frameScale;
    private bool lastState;
    private bool animating;
    private bool appearing;
    private float elapsed;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        mark = toggle.graphic != null ? toggle.graphic.rectTransform : null;
        frame = toggle.targetGraphic != null ? toggle.targetGraphic.rectTransform : null;
        markScale = mark != null ? mark.localScale : Vector3.one;
        frameScale = frame != null ? frame.localScale : Vector3.one;
        lastState = toggle.isOn;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        if (toggle.graphic != null)
            toggle.graphic.canvasRenderer.SetAlpha(lastState ? 1f : 0f);
    }

    private void LateUpdate()
    {
        if (toggle.isOn != lastState)
        {
            appearing = toggle.isOn;
            lastState = appearing;
            elapsed = 0f;
            animating = true;
        }

        if (!animating)
            return;

        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, Duration);
        float progress = elapsed / Duration;
        float eased = progress * progress * (3f - 2f * progress);
        float pulse = Mathf.Sin(progress * Mathf.PI);
        if (mark != null)
            mark.localScale = markScale * (appearing ? Mathf.Lerp(0.55f, 1f, eased) + 0.25f * pulse
                : Mathf.Lerp(1f, 0.55f, eased));
        if (frame != null)
            frame.localScale = frameScale * (1f + 0.14f * pulse);
        if (toggle.graphic != null)
            toggle.graphic.canvasRenderer.SetAlpha(appearing ? eased : 1f - eased);

        if (elapsed < Duration)
            return;

        animating = false;
        if (mark != null)
            mark.localScale = markScale;
        if (frame != null)
            frame.localScale = frameScale;
    }

    private void OnDisable()
    {
        if (mark != null)
            mark.localScale = markScale;
        if (frame != null)
            frame.localScale = frameScale;
        if (toggle != null && toggle.graphic != null)
            toggle.graphic.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
        animating = false;
    }
}
