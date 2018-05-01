#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

[TestFixture]
public class NavMeshSurfaceModifierVolumeTests
{
    NavMeshSurface surface;
    NavMeshModifierVolume modifier;

    [SetUp]
    public void CreatePlaneAndModifierVolume()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        surface = go.AddComponent<NavMeshSurface>();

        modifier = new GameObject().AddComponent<NavMeshModifierVolume>();
    }

    [TearDown]
    public void DestroyPlaneAndModifierVolume()
    {
        GameObject.DestroyImmediate(surface.gameObject);
        GameObject.DestroyImmediate(modifier.gameObject);
    }

    [Test]
    public void AreaAffectsNavMeshOverlapping()
    {
        modifier.center = Vector3.zero;
        modifier.size = Vector3.one;
        modifier.area = 4;

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
    }

    [Test]
    public void AreaDoesNotAffectsNavMeshWhenNotOverlapping()
    {
        modifier.center = 1.1f * Vector3.right;
        modifier.size = Vector3.one;
        modifier.area = 4;

        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
    }

    [Test]
    public void BuildUsesOnlyIncludedModifierVolume()
    {
        modifier.center = Vector3.zero;
        modifier.size = Vector3.one;
        modifier.area = 4;
        modifier.gameObject.layer = 7;

        surface.layerMask = ~(1 << 7);
        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
    }
}
#endif
