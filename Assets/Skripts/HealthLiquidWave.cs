using UnityEngine;
using UnityEngine.UI;

// Animates the existing health sprite without changing saved health or its numeric label.
[DisallowMultipleComponent]
public sealed class HealthLiquidWave : MonoBehaviour
{
    private const float FillSpeed = 2.5f;
    private const float PhaseSpeed = 2.2f;

    private Image fillImage;
    private Material liquidMaterial;
    private float targetFill;
    private float displayedFill;
    private float phase;
    private float motion;
    private float impact;
    private bool hasTarget;

    internal void Initialize(Image image)
    {
        if (fillImage != null || image == null)
            return;

        Material source = Resources.Load<Material>("HealthLiquidFill");
        if (source == null)
        {
            Debug.LogError("HealthLiquidFill material is missing from Resources.", this);
            enabled = false;
            return;
        }

        fillImage = image;
        liquidMaterial = new Material(source);
        fillImage.material = liquidMaterial;
        fillImage.type = Image.Type.Simple;
    }

    internal void SetTarget(float percent)
    {
        float nextFill = Mathf.Clamp01(percent);
        if (fillImage == null)
            return;

        if (!hasTarget)
        {
            displayedFill = nextFill;
            hasTarget = true;
        }
        else if (!Mathf.Approximately(nextFill, targetFill))
        {
            motion = 1f;
            impact = nextFill > targetFill ? 1f : -1f;
        }

        targetFill = nextFill;
        Render();
    }

    internal void PulseDamage()
    {
        if (liquidMaterial == null)
            return;

        motion = 1f;
        impact = -1f;
        Render();
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        float deltaTime = Time.unscaledDeltaTime;
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, FillSpeed * deltaTime);
        phase = Mathf.Repeat(phase + PhaseSpeed * deltaTime, Mathf.PI * 2f);
        motion = Mathf.MoveTowards(motion, 0f, 1.5f * deltaTime);
        impact = Mathf.MoveTowards(impact, 0f, 0.8f * deltaTime);
        Render();
    }

    private void Render()
    {
        fillImage.enabled = displayedFill > 0f;
        liquidMaterial.SetFloat("_Fill", displayedFill);
        liquidMaterial.SetFloat("_Phase", phase);
        liquidMaterial.SetFloat("_Motion", motion);
        liquidMaterial.SetFloat("_Impact", impact);
    }

    private void OnDestroy()
    {
        if (fillImage != null && fillImage.material == liquidMaterial)
            fillImage.material = null;
        if (liquidMaterial != null)
            Destroy(liquidMaterial);
    }
}
