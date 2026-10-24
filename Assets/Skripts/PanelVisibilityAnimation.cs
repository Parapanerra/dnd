using UnityEngine;

// Animates the panel's presentation while its controller owns the open/closed state.
[DisallowMultipleComponent]
public sealed class PanelVisibilityAnimation : MonoBehaviour
{
    private const float Duration = 0.22f;

    private CanvasGroup group;
    private float normalAlpha;
    private bool normalInteractable;
    private bool normalBlocksRaycasts;
    private bool initialized;
    private bool targetVisible;
    private bool animating;
    private float elapsed;
    private float startingAlpha;

    public bool IsOpen => targetVisible;

    public void Initialize()
    {
        if (initialized)
            return;

        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        normalAlpha = group.alpha;
        normalInteractable = group.interactable;
        normalBlocksRaycasts = group.blocksRaycasts;
        targetVisible = gameObject.activeSelf;
        initialized = true;
    }

    public void SetVisible(bool visible, bool animate)
    {
        Initialize();
        if (targetVisible == visible && (!animating || animate))
            return;

        targetVisible = visible;
        if (!animate)
        {
            animating = false;
            group.alpha = normalAlpha;
            group.interactable = normalInteractable;
            group.blocksRaycasts = normalBlocksRaycasts;
            gameObject.SetActive(visible);
            return;
        }

        if (visible && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            group.alpha = 0f;
        }

        startingAlpha = group.alpha;
        group.interactable = visible && normalInteractable;
        group.blocksRaycasts = visible && normalBlocksRaycasts;
        elapsed = 0f;
        animating = true;
    }

    private void Update()
    {
        if (!animating)
            return;

        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, Duration);
        float progress = elapsed / Duration;
        float eased = progress * progress * (3f - 2f * progress);
        group.alpha = Mathf.Lerp(startingAlpha, targetVisible ? normalAlpha : 0f, eased);

        if (elapsed < Duration)
            return;

        animating = false;
        if (!targetVisible)
        {
            gameObject.SetActive(false);
            group.alpha = normalAlpha;
        }
    }

    private void OnDisable()
    {
        animating = false;
        if (!initialized)
            return;
        group.alpha = normalAlpha;
        group.interactable = normalInteractable;
        group.blocksRaycasts = normalBlocksRaycasts;
    }
}
