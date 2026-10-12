using UnityEngine;

// Keeps the legacy hierarchy key identical wherever scene controls are persisted.
public static class StableFieldPath
{
    public static string For(Transform transform)
    {
        string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name + "/" + path;
        }

        return path;
    }
}
