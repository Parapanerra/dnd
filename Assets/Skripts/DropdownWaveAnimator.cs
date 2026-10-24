using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A short wave of visible options after Unity creates a dropdown list.
// Only opacity and label tint are animated; RectTransforms and selection stay untouched.
internal sealed class DropdownWaveAnimator : MonoBehaviour
{
    private const float OptionFadeSeconds = 0.17f;
    private const float OptionDelaySeconds = 0.018f;
    private const int LastStaggeredOption = 7;
    private const float CloseSeconds = OptionFadeSeconds + LastStaggeredOption * OptionDelaySeconds;
    private static readonly Color FlashColor = new Color(0.5f, 0.87f, 1f, 1f);

    private sealed class OptionVisual
    {
        internal CanvasGroup Group;
        internal Graphic Label;
        internal Color OriginalLabelColor;
    }

    private readonly List<OptionVisual> options = new List<OptionVisual>();
    private Transform currentList;
    private CanvasGroup listGroup;
    private float elapsed;
    private bool openingComplete;

    private void LateUpdate()
    {
        Transform list = transform.Find("Dropdown List");
        if (!ReferenceEquals(list, currentList))
        {
            FinishAnimation();
            currentList = list;
            if (list != null)
                StartAnimation(list);
            else
                SetFadeSpeed(0f);
        }

        if (options.Count == 0)
            return;

        if (openingComplete && listGroup != null && listGroup.alpha < 0.999f)
        {
            AnimateClose((1f - listGroup.alpha) * CloseSeconds);
            return;
        }

        if (openingComplete)
            return;

        elapsed += Time.unscaledDeltaTime;
        bool complete = true;
        for (int index = 0; index < options.Count; index++)
        {
            OptionVisual option = options[index];
            if (option.Group == null)
                continue;

            float delay = Mathf.Min(index, LastStaggeredOption) * OptionDelaySeconds;
            float progress = Mathf.Clamp01((elapsed - delay) / OptionFadeSeconds);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            option.Group.alpha = smoothProgress;

            if (option.Label != null)
                option.Label.color = Color.Lerp(FlashColor, option.OriginalLabelColor, smoothProgress);

            if (progress < 1f)
                complete = false;
        }

        if (complete)
        {
            openingComplete = true;
            RestoreOptionVisuals();
        }
    }

    private void StartAnimation(Transform list)
    {
        elapsed = 0f;
        openingComplete = false;
        listGroup = list.GetComponent<CanvasGroup>();
        SetFadeSpeed(CloseSeconds);
        foreach (Toggle toggle in list.GetComponentsInChildren<Toggle>())
        {
            CanvasGroup group = toggle.GetComponent<CanvasGroup>();
            if (group == null)
                group = toggle.gameObject.AddComponent<CanvasGroup>();

            Graphic label = null;
            foreach (Graphic graphic in toggle.GetComponentsInChildren<Graphic>())
            {
                if (graphic.name.Contains("Label") || graphic.name.Contains("Text"))
                {
                    label = graphic;
                    break;
                }
            }

            options.Add(new OptionVisual
            {
                Group = group,
                Label = label,
                OriginalLabelColor = label != null ? label.color : Color.white
            });
            group.alpha = 0f;
        }
    }

    private void AnimateClose(float closeElapsed)
    {
        for (int index = 0; index < options.Count; index++)
        {
            OptionVisual option = options[index];
            if (option.Group == null)
                continue;

            float delay = (LastStaggeredOption - Mathf.Min(index, LastStaggeredOption)) * OptionDelaySeconds;
            float progress = Mathf.Clamp01((closeElapsed - delay) / OptionFadeSeconds);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            option.Group.alpha = 1f - smoothProgress;

            if (option.Label != null)
                option.Label.color = Color.Lerp(option.OriginalLabelColor, FlashColor, smoothProgress);
        }
    }

    private void FinishAnimation()
    {
        RestoreOptionVisuals();
        options.Clear();
        listGroup = null;
        openingComplete = false;
    }

    private void RestoreOptionVisuals()
    {
        foreach (OptionVisual option in options)
        {
            if (option.Group != null)
                option.Group.alpha = 1f;
            if (option.Label != null)
                option.Label.color = option.OriginalLabelColor;
        }
    }

    private void SetFadeSpeed(float seconds)
    {
        Dropdown dropdown = GetComponent<Dropdown>();
        if (dropdown != null)
            dropdown.alphaFadeSpeed = seconds;

        TMP_Dropdown tmpDropdown = GetComponent<TMP_Dropdown>();
        if (tmpDropdown != null)
            tmpDropdown.alphaFadeSpeed = seconds;
    }

    private void OnDisable()
    {
        FinishAnimation();
        currentList = null;
        SetFadeSpeed(0f);
    }
}
