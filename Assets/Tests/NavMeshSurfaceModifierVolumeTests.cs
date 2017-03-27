using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceModifierVolumeTests
{
    [Test]
    public void AreaAffectsNavMeshOverlapping()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifierVolume>();
        modifier.center = Vector3.zero;
        modifier.size = Vector3.one;
        modifier.area = 4;

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AreaDoesNotAffectsNavMeshWhenNotOverlapping()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifierVolume>();
        modifier.center = 1.1f * Vector3.right;
        modifier.size = Vector3.one;
        modifier.area = 4;

        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void BuildUsesOnlyIncludedModifierVolume()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();

        var modifierGO = new GameObject();
        var modifier = modifierGO.AddComponent<NavMeshModifierVolume>();
        modifier.center = Vector3.zero;
        modifier.size = Vector3.one;
        modifier.area = 4;
        modifierGO.layer = 7;

        surface.layerMask = ~(1 << 7);
        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));

        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(modifierGO);
    }
}
