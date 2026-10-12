using UnityEngine;
using UnityEngine.UI;

internal sealed class HealthBarView
{
    private readonly SceneRoleLookup roleLookup = new SceneRoleLookup();
    private readonly InputField maxHealthInputField;
    private readonly Slider healthSlider;
    private Slider temporaryHealthSlider;
    private readonly Text healthText;
    private Text temporaryHealthText;

    internal HealthBarView(InputField maxHealthInputField, Slider healthSlider,
        Slider temporaryHealthSlider, Text healthText, Text temporaryHealthText)
    {
        this.maxHealthInputField = maxHealthInputField;
        this.healthSlider = healthSlider;
        this.temporaryHealthSlider = temporaryHealthSlider;
        this.healthText = healthText;
        this.temporaryHealthText = temporaryHealthText;
    }

    internal void Render(int currentHealth, int maxHealth, int currentTemporaryHealth, int maxTemporaryHealth)
    {
        if (maxHealthInputField != null && maxHealthInputField.text != maxHealth.ToString())
            maxHealthInputField.SetTextWithoutNotify(maxHealth.ToString());

        if (healthSlider != null)
            UpdateSliderPercent(healthSlider, currentHealth, maxHealth);

        if (temporaryHealthSlider == null)
            temporaryHealthSlider = FindTemporaryHealthSlider();
        if (temporaryHealthSlider != null)
            UpdateSliderPercent(temporaryHealthSlider, currentTemporaryHealth, maxTemporaryHealth);

        if (temporaryHealthText == null)
            temporaryHealthText = FindTemporaryHealthText();
        if (temporaryHealthText != null)
            temporaryHealthText.text = $"{currentTemporaryHealth} / {maxTemporaryHealth}";
        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }

    private void UpdateSliderPercent(Slider slider, int current, int max)
    {
        if (slider == null)
            return;

        bool visible = current > 0 && max > 0;
        float percent = visible ? Mathf.Clamp01((float)current / max) : 0f;

        RectTransform fillRoot = GetSliderFillRoot(slider);

        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = null;
        slider.SetValueWithoutNotify(percent);

        SetSliderHandleVisible(slider, visible);
        SetSliderFillVisible(fillRoot, visible, percent);
    }

    private void SetSliderHandleVisible(Slider slider, bool visible)
    {
        if (slider == null || slider.handleRect == null)
            return;

        Graphic[] graphics = slider.handleRect.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            if (graphic != null)
                graphic.enabled = visible;
    }

    private void SetSliderFillVisible(RectTransform fillRoot, bool visible, float percent)
    {
        if (fillRoot == null)
            return;

        Graphic[] graphics = fillRoot.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic != null)
            {
                graphic.enabled = visible;

                if (graphic is Image image)
                {
                    image.type = Image.Type.Filled;
                    image.fillMethod = Image.FillMethod.Horizontal;
                    image.fillOrigin = (int)Image.OriginHorizontal.Left;
                    image.fillAmount = visible ? percent : 0f;
                }
            }
        }
    }

    private RectTransform GetSliderFillRoot(Slider slider)
    {
        if (slider == null)
            return null;

        if (slider.fillRect != null)
            return slider.fillRect;

        RectTransform namedFill = FindChildRect(slider.transform, "fild");
        if (namedFill != null)
            return namedFill;

        Image[] images = slider.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image != null && image.transform != slider.transform)
                return image.rectTransform;
        }

        return null;
    }

    private RectTransform FindChildRect(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect != null && string.Equals(rect.gameObject.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return rect;
        }

        return null;
    }

    private Slider FindSliderByName(string objectName)
    {
        Slider[] sliders = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
        foreach (Slider slider in sliders)
        {
            if (slider != null && string.Equals(slider.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return slider;
        }

        foreach (Slider slider in sliders)
        {
            if (slider != null && HasParentNamed(slider.transform, objectName))
                return slider;
        }

        Transform namedObject = FindTransformByName(objectName);
        if (namedObject != null)
        {
            Slider childSlider = namedObject.GetComponentInChildren<Slider>(true);
            if (childSlider != null)
                return childSlider;
        }

        return null;
    }

    private Slider FindTemporaryHealthSlider()
    {
        SceneRoleMarker marker = roleLookup.Find(SceneRole.TemporaryHealth);
        if (marker != null)
            return marker.GetComponent<Slider>() ?? marker.GetComponentInChildren<Slider>(true);

        return FindSliderByName("folslive");
    }

    private Text FindTemporaryHealthText()
    {
        if (temporaryHealthSlider != null)
        {
            Text text = temporaryHealthSlider.GetComponentInChildren<Text>(true);
            if (text != null)
                return text;
        }

        Transform namedObject = FindTransformByName("folslive");
        return namedObject != null ? namedObject.GetComponentInChildren<Text>(true) : null;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
        {
            if (candidate != null && string.Equals(candidate.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    private bool HasParentNamed(Transform transform, string objectName)
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.parent;
        }

        return false;
    }
}
