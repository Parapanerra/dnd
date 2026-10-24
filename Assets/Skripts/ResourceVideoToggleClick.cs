using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public sealed class ResourceVideoToggleClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private ResourceVideoPlaybackBase panel;
    private Toggle resourceToggle;
    private bool armed;

    internal void Initialize(ResourceVideoPlaybackBase owner)
    {
        if (resourceToggle != null)
            resourceToggle.onValueChanged.RemoveListener(OnValueChanged);

        panel = owner;
        resourceToggle = GetComponent<Toggle>();
        resourceToggle.onValueChanged.AddListener(OnValueChanged);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        armed = !resourceToggle.isOn;
    }

    private void OnValueChanged(bool isOn)
    {
        if (armed && isOn)
            panel.PlayOnce();
        armed = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartCoroutine(ClearArmAfterClick());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        armed = false;
    }

    private IEnumerator ClearArmAfterClick()
    {
        yield return null;
        armed = false;
    }

    private void OnDisable()
    {
        armed = false;
    }

    private void OnDestroy()
    {
        if (resourceToggle != null)
            resourceToggle.onValueChanged.RemoveListener(OnValueChanged);
    }
}
