// Keep this component type so existing scene and prefab references remain valid.
// HealthBar owns the health logic and serialized UI fields for both variants.
public class HealthBar1 : HealthBar
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void Start()
    {
        base.Start();
    }
}
