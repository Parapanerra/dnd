using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class InventoryHierarchyOrderTests
{
    [Test]
    public void ParentOrderWinsWhenFirstParentHasMoreThanOneHundredChildren()
    {
        GameObject root = new GameObject("root");
        try
        {
            Transform firstParent = new GameObject("first").transform;
            firstParent.SetParent(root.transform);
            Transform secondParent = new GameObject("second").transform;
            secondParent.SetParent(root.transform);

            Transform lastChildOfFirst = null;
            for (int i = 0; i <= 100; i++)
            {
                lastChildOfFirst = new GameObject("cell").transform;
                lastChildOfFirst.SetParent(firstParent);
            }

            Transform firstChildOfSecond = new GameObject("cell").transform;
            firstChildOfSecond.SetParent(secondParent);

            Type managerType = Type.GetType("InventoryPageManager, Assembly-CSharp");
            Assert.NotNull(managerType);
            Component manager = root.AddComponent(managerType);
            MethodInfo compare = managerType.GetMethod(
                "CompareHierarchyOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(compare);
            int forward = (int)compare.Invoke(manager, new object[] { lastChildOfFirst, firstChildOfSecond });
            int reverse = (int)compare.Invoke(manager, new object[] { firstChildOfSecond, lastChildOfFirst });

            Assert.Less(forward, 0);
            Assert.Greater(reverse, 0);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
