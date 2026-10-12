using UnityEngine;

// Resolves the two persistent scene managers without exposing a mutable static singleton.
internal sealed class RuntimeManagerResolver
{
    internal DndSaveManager SaveManager()
    {
        DndSaveManager existing = FindSaveManager();
        if (existing != null)
            return existing;

        return new GameObject("DndSaveManager").AddComponent<DndSaveManager>();
    }

    internal RuntimeLocalization Localization()
    {
        RuntimeLocalization existing = FindLocalization();
        if (existing != null)
            return existing;

        return new GameObject("RuntimeLocalization").AddComponent<RuntimeLocalization>();
    }

    internal DndSaveManager FindSaveManager()
    {
        DndSaveManager[] candidates = Object.FindObjectsByType<DndSaveManager>(FindObjectsInactive.Include);
        foreach (DndSaveManager candidate in candidates)
            if (candidate != null && candidate.IsInitialized)
                return candidate;

        return candidates.Length > 0 ? candidates[0] : null;
    }

    internal RuntimeLocalization FindLocalization()
    {
        RuntimeLocalization[] candidates = Object.FindObjectsByType<RuntimeLocalization>(FindObjectsInactive.Include);
        foreach (RuntimeLocalization candidate in candidates)
            if (candidate != null && candidate.IsInitialized)
                return candidate;

        return candidates.Length > 0 ? candidates[0] : null;
    }
}
