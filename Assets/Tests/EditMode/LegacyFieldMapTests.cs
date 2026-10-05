using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class LegacyFieldMapTests
{
    [Serializable]
    private sealed class CountManifest
    {
        public int totalRows;
        public List<SceneCount> scenes;
    }

    [Serializable]
    private sealed class SceneCount
    {
        public string sceneName;
        public int inputData;
        public int toggleData;
        public int sliderData;
        public int dropdownData;
    }

    [Test]
    public void FrozenLegacyMap_MatchesGoldenSceneAndCollectionCounts()
    {
        string csvPath = Path.Combine(
            Application.dataPath,
            "Refactoring",
            "Baseline",
            "LegacyFieldMap_v1.csv");
        string manifestPath = Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "Legacy",
            "legacy_control_counts_v1.json");

        string[] lines = File.ReadAllLines(csvPath);
        CountManifest manifest = JsonUtility.FromJson<CountManifest>(File.ReadAllText(manifestPath));
        List<string> header = ParseCsvLine(lines[0]);
        int sceneColumn = header.IndexOf("sceneName");
        int collectionColumn = header.IndexOf("legacyCollection");
        int pathColumn = header.IndexOf("hierarchyPath");
        int idColumn = header.IndexOf("proposedStableId");
        Assert.That(sceneColumn, Is.GreaterThanOrEqualTo(0));
        Assert.That(collectionColumn, Is.GreaterThanOrEqualTo(0));
        Assert.That(pathColumn, Is.GreaterThanOrEqualTo(0));
        Assert.That(idColumn, Is.GreaterThanOrEqualTo(0));

        var counts = new Dictionary<string, Dictionary<string, int>>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            List<string> row = ParseCsvLine(lines[lineIndex]);
            string scene = row[sceneColumn];
            string collection = row[collectionColumn];
            Assert.That(row[pathColumn], Is.Not.Empty, "Empty path at CSV line " + (lineIndex + 1));
            Assert.That(row[idColumn], Is.Not.Empty, "Empty stable ID at CSV line " + (lineIndex + 1));
            Assert.That(ids.Add(row[idColumn]), Is.True, "Duplicate stable ID: " + row[idColumn]);

            if (!counts.TryGetValue(scene, out Dictionary<string, int> sceneCounts))
            {
                sceneCounts = new Dictionary<string, int>();
                counts.Add(scene, sceneCounts);
            }

            sceneCounts.TryGetValue(collection, out int current);
            sceneCounts[collection] = current + 1;
        }

        Assert.That(lines.Length - 1, Is.EqualTo(manifest.totalRows));
        Assert.That(ids.Count, Is.EqualTo(manifest.totalRows));
        Assert.That(counts.Count, Is.EqualTo(manifest.scenes.Count));
        foreach (SceneCount expected in manifest.scenes)
        {
            Assert.That(counts.ContainsKey(expected.sceneName), Is.True, expected.sceneName);
            Dictionary<string, int> actual = counts[expected.sceneName];
            Assert.That(GetCount(actual, "inputData"), Is.EqualTo(expected.inputData), expected.sceneName);
            Assert.That(GetCount(actual, "toggleData"), Is.EqualTo(expected.toggleData), expected.sceneName);
            Assert.That(GetCount(actual, "sliderData"), Is.EqualTo(expected.sliderData), expected.sceneName);
            Assert.That(GetCount(actual, "dropdownData"), Is.EqualTo(expected.dropdownData), expected.sceneName);
        }
    }

    private static int GetCount(Dictionary<string, int> counts, string key)
    {
        return counts.TryGetValue(key, out int value) ? value : 0;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        bool quoted = false;
        for (int index = 0; index < line.Length; index++)
        {
            char value = line[index];
            if (value == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (value == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Length = 0;
            }
            else
            {
                current.Append(value);
            }
        }

        values.Add(current.ToString());
        return values;
    }
}
