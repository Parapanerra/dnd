using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class MainMenuLanguageDropdown : UnityEngine.UI.Dropdown
{
    private bool listIsOpen;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (listIsOpen)
            Hide();
        else
            base.OnPointerClick(eventData);
    }

    protected override GameObject CreateBlocker(Canvas rootCanvas)
    {
        GameObject blocker = base.CreateBlocker(rootCanvas);
        listIsOpen = true;
        return blocker;
    }

    protected override void DestroyBlocker(GameObject blocker)
    {
        listIsOpen = false;
        base.DestroyBlocker(blocker);
    }
}
