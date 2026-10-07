using System;
using System.Collections.Generic;
using UnityEngine;

// Frozen A0 aliases let a rest update stable toggle values in saves made before A6.
public static class LegacyRestToggleIdMap
{
    private static Dictionary<string, string> ids;

    public static bool TryGetStableId(string legacyKey, out string stableId)
    {
        if (ids == null)
        {
            ids = new Dictionary<string, string>(StringComparer.Ordinal);
            TextAsset source = Resources.Load<TextAsset>("LegacyRestToggleIds");
            if (source != null)
                foreach (string line in source.text.Split('\n'))
                {
                    int tab = line.IndexOf('\t');
                    if (tab > 0)
                        ids[line.Substring(0, tab)] = line.Substring(tab + 1).TrimEnd('\r');
                }
        }

        return ids.TryGetValue(legacyKey, out stableId);
    }
}
