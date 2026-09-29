using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public class InputFieldTabNavigation : MonoBehaviour
{
    private sealed class InputTarget
    {
        public GameObject GameObject;
        public Selectable Selectable;
        public DoubleClickInputFieldActivator Activator;
        public Vector2 ScreenPosition;
        public float Height;
        public bool HasManualOrder;
        public int ManualOrder;
        public int AutomaticOrder;
    }

    private sealed class InputRow
    {
        public readonly List<InputTarget> Targets = new List<InputTarget>();
        public float CenterY;
        public float Tolerance;
    }

    private static InputFieldTabNavigation instance;
    private readonly List<InputTarget> orderedTargets = new List<InputTarget>();
    private readonly List<InputRow> rows = new List<InputRow>();
    private bool tabPressedThisFrame;
    private InputField protectedLegacyInput;
    private TMP_InputField protectedTmpInput;
    private bool previousLegacyReadOnly;
    private bool previousTmpReadOnly;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        InputFieldTabNavigation existing = FindAnyObjectByType<InputFieldTabNavigation>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject navigator = new GameObject(nameof(InputFieldTabNavigation));
        instance = navigator.AddComponent<InputFieldTabNavigation>();
        DontDestroyOnLoad(navigator);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab))
            return;

        tabPressedThisFrame = true;
        ProtectSelectedInputFromTabCharacter();
    }

    private void LateUpdate()
    {
        if (!tabPressedThisFrame)
            return;

        RestoreSelectedInputEditing();
        MoveToNextInput();
        tabPressedThisFrame = false;
    }

    private void ProtectSelectedInputFromTabCharacter()
    {
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        if (selected == null)
            return;

        protectedLegacyInput = selected.GetComponent<InputField>();
        if (protectedLegacyInput != null)
        {
            previousLegacyReadOnly = protectedLegacyInput.readOnly;
            protectedLegacyInput.readOnly = true;
        }

        protectedTmpInput = selected.GetComponent<TMP_InputField>();
        if (protectedTmpInput != null)
        {
            previousTmpReadOnly = protectedTmpInput.readOnly;
            protectedTmpInput.readOnly = true;
        }
    }

    private void RestoreSelectedInputEditing()
    {
        if (protectedLegacyInput != null)
            protectedLegacyInput.readOnly = previousLegacyReadOnly;

        if (protectedTmpInput != null)
            protectedTmpInput.readOnly = previousTmpReadOnly;

        protectedLegacyInput = null;
        protectedTmpInput = null;
    }

    private void MoveToNextInput()
    {
        BuildNavigationOrder();
        if (orderedTargets.Count == 0)
            return;

        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        int currentIndex = orderedTargets.FindIndex(target => target.GameObject == selected);
        int nextIndex = (currentIndex + 1) % orderedTargets.Count;
        Activate(orderedTargets[nextIndex]);
    }

    private void BuildNavigationOrder()
    {
        orderedTargets.Clear();
        rows.Clear();

        foreach (DoubleClickInputFieldActivator activator in DoubleClickInputFieldActivator.GetConfiguredInputs())
        {
            if (activator.TryGetInput(out Selectable selectable, out RectTransform rectTransform))
                AddTarget(activator, selectable, rectTransform);
        }

        orderedTargets.Sort((left, right) => right.ScreenPosition.y.CompareTo(left.ScreenPosition.y));

        foreach (InputTarget target in orderedTargets)
        {
            InputRow matchingRow = null;
            float closestDistance = float.MaxValue;

            foreach (InputRow row in rows)
            {
                float distance = Mathf.Abs(target.ScreenPosition.y - row.CenterY);
                float allowedDistance = Mathf.Max(row.Tolerance, target.Height * 0.5f);
                if (distance <= allowedDistance && distance < closestDistance)
                {
                    matchingRow = row;
                    closestDistance = distance;
                }
            }

            if (matchingRow == null)
            {
                matchingRow = new InputRow
                {
                    CenterY = target.ScreenPosition.y,
                    Tolerance = Mathf.Max(8f, target.Height * 0.5f)
                };
                rows.Add(matchingRow);
            }

            matchingRow.Targets.Add(target);
            matchingRow.CenterY = AverageY(matchingRow.Targets);
            matchingRow.Tolerance = Mathf.Max(matchingRow.Tolerance, target.Height * 0.5f);
        }

        rows.Sort((left, right) => right.CenterY.CompareTo(left.CenterY));
        orderedTargets.Clear();

        foreach (InputRow row in rows)
        {
            row.Targets.Sort((left, right) => left.ScreenPosition.x.CompareTo(right.ScreenPosition.x));
            orderedTargets.AddRange(row.Targets);
        }

        ApplyManualOrderIfConfigured();
    }

    private void ApplyManualOrderIfConfigured()
    {
        bool hasManualOrderComponents = false;

        for (int i = 0; i < orderedTargets.Count; i++)
        {
            InputTarget target = orderedTargets[i];
            target.AutomaticOrder = i;
            if (target.HasManualOrder)
                hasManualOrderComponents = true;
        }

        // Until a scene is configured, preserve the existing automatic order.
        if (!hasManualOrderComponents)
            return;

        // Once Tab Order components are present, only positive values participate.
        orderedTargets.RemoveAll(target => !target.HasManualOrder || target.ManualOrder <= 0);
        orderedTargets.Sort((left, right) =>
        {
            int orderComparison = left.ManualOrder.CompareTo(right.ManualOrder);
            return orderComparison != 0
                ? orderComparison
                : left.AutomaticOrder.CompareTo(right.AutomaticOrder);
        });
    }

    private void AddTarget(DoubleClickInputFieldActivator activator, Selectable selectable, RectTransform rectTransform)
    {
        if (selectable == null || rectTransform == null || !selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
            return;

        Canvas canvas = selectable.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled)
            return;

        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float height = Mathf.Abs(
            RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y -
            RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y);
        InputFieldTabOrder tabOrder = selectable.GetComponent<InputFieldTabOrder>();

        orderedTargets.Add(new InputTarget
        {
            GameObject = selectable.gameObject,
            Selectable = selectable,
            Activator = activator,
            ScreenPosition = screenPosition,
            Height = Mathf.Max(1f, height),
            HasManualOrder = tabOrder != null,
            ManualOrder = tabOrder != null ? tabOrder.Order : 0
        });
    }

    private static float AverageY(List<InputTarget> targets)
    {
        float total = 0f;
        foreach (InputTarget target in targets)
            total += target.ScreenPosition.y;

        return total / targets.Count;
    }

    private static void Activate(InputTarget target)
    {
        if (target.Activator != null)
        {
            target.Activator.ActivateInput();
            return;
        }

        target.Selectable.Select();
    }
}
