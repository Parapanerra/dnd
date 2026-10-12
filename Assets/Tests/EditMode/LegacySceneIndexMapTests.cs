using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class LegacySceneIndexMapTests
{
    private static readonly MethodInfo Lookup = Type
        .GetType("LegacySceneIndexMap, Assembly-CSharp")
        ?.GetMethod("TryGetSceneName", BindingFlags.Static | BindingFlags.NonPublic);

    [TestCase(0, "menu")]
    [TestCase(4, "petsesn 1")]
    [TestCase(7, "inventory")]
    [TestCase(42, "avtoru")]
    [TestCase(44, "spelBook")]
    [TestCase(67, "petsesn 6")]
    public void OldBuildIndicesKeepTheirDestinations(int index, string expectedScene)
    {
        Assert.NotNull(Lookup);
        object[] arguments = { index, null };
        Assert.IsTrue((bool)Lookup.Invoke(null, arguments));
        Assert.AreEqual(expectedScene, arguments[1]);
    }

    [Test]
    public void EverySerializedSceneButtonIndexHasALegacyDestination()
    {
        Assert.NotNull(Lookup);
        int checkedButtons = 0;
        foreach (string extension in new[] { "*.unity", "*.prefab" })
        {
            foreach (string file in Directory.EnumerateFiles(Application.dataPath, extension, SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                int position = 0;
                while ((position = content.IndexOf("value: LoadScenes", position, StringComparison.Ordinal)) >= 0)
                {
                    int argumentProperty = content.IndexOf(
                        "propertyPath: m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Arguments.m_IntArgument",
                        position, StringComparison.Ordinal);
                    // A prefab instance may override only the method name and inherit
                    // its integer argument from the source prefab.
                    if (argumentProperty < 0 || argumentProperty - position >= 1000)
                    {
                        position += "value: LoadScenes".Length;
                        continue;
                    }
                    Match value = Regex.Match(content.Substring(argumentProperty, Math.Min(200, content.Length - argumentProperty)),
                        @"\bvalue: (?<index>\d+)");
                    Assert.IsTrue(value.Success, file);
                    int index = int.Parse(value.Groups["index"].Value);
                    object[] arguments = { index, null };
                    Assert.IsTrue((bool)Lookup.Invoke(null, arguments), file + ": " + index);
                    checkedButtons++;
                    position = argumentProperty + value.Index + value.Length;
                }
            }
        }

        Assert.Greater(checkedButtons, 0);
    }
    [TestCase("petsesn", "petsesn")]
    [TestCase("petsesn 7", "petsesn")]
    [TestCase("petsesn 8", "petsesn 8")]
    [TestCase("mypetsesn", "mypetsesn")]
    public void OnlyKnownWildShapePagesUseWildShapeVisualScene(string source, string expected)
    {
        Type navigator = Type.GetType("load_scenes, Assembly-CSharp");
        Assert.NotNull(navigator);
        MethodInfo visualScene = navigator.GetMethod("GetVisualSceneName",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(visualScene);
        Assert.AreEqual(expected, visualScene.Invoke(null, new object[] { source }));
    }}