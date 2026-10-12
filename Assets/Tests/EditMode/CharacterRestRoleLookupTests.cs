using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CharacterRestRoleLookupTests
{
    [Test]
    public void RestFindsMarkedPanelAfterRenameAndLegacyPanelByName()
    {
        GameObject root = new GameObject("Resources");
        GameObject panel = new GameObject("Renamed panel");
        panel.transform.SetParent(root.transform);
        GameObject markerObject = new GameObject("Renamed icon");
        markerObject.transform.SetParent(panel.transform);
        SceneRoleMarker marker = markerObject.AddComponent<SceneRoleMarker>();
        marker.Configure(SceneRole.BloodCurse, true);

        try
        {
            Type serviceType = Type.GetType("CharacterRestService, Assembly-CSharp");
            object service = Activator.CreateInstance(serviceType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
            MethodInfo findPanel = serviceType.GetMethod("FindPanelByMarker",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreSame(panel.transform,
                findPanel.Invoke(service, new object[] { root.transform, SceneRole.BloodCurse }));

            UnityEngine.Object.DestroyImmediate(markerObject);
            GameObject legacyIcon = new GameObject("BloodCurse");
            legacyIcon.transform.SetParent(panel.transform);
            Assert.AreSame(panel.transform,
                findPanel.Invoke(service, new object[] { root.transform, SceneRole.BloodCurse }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}