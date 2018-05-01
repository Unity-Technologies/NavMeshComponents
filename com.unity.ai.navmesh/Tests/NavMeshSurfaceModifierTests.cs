#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

[TestFixture]
public class NavMeshSurfaceModifierTests
{
    NavMeshSurface surface;
    NavMeshModifier modifier;

    [SetUp]
    public void CreatePlaneWithModifier()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        surface = plane.AddComponent<NavMeshSurface>();
        modifier = plane.AddComponent<NavMeshModifier>();
    }

    [TearDown]
    public void DestroyPlaneWithModifier()
    {
        GameObject.DestroyImmediate(modifier.gameObject);
    }

    [Test]
    public void ModifierIgnoreAffectsSelf()
    {
        modifier.ignoreFromBuild = true;

        surface.BuildNavMesh();

        Assert.IsFalse(NavMeshSurfaceTests.HasNavMeshAtOrigin());
    }

    [Test]
    public void ModifierIgnoreAffectsChild()
    {
        modifier.ignoreFromBuild = true;
        modifier.GetComponent<MeshRenderer>().enabled = false;

        var childPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        childPlane.transform.SetParent(modifier.transform);

        surface.BuildNavMesh();

        Assert.IsFalse(NavMeshSurfaceTests.HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(childPlane);
    }

    [Test]
    public void ModifierIgnoreDoesNotAffectSibling()
    {
        modifier.ignoreFromBuild = true;
        modifier.GetComponent<MeshRenderer>().enabled = false;

        var siblingPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        surface.BuildNavMesh();

        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(siblingPlane);
    }

    [Test]
    public void ModifierOverrideAreaAffectsSelf()
    {
        modifier.area = 4;
        modifier.overrideArea = true;

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
    }

    [Test]
    public void ModifierOverrideAreaAffectsChild()
    {
        modifier.area = 4;
        modifier.overrideArea = true;
        modifier.GetComponent<MeshRenderer>().enabled = false;

        var childPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        childPlane.transform.SetParent(modifier.transform);

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
        GameObject.DestroyImmediate(childPlane);
    }

    [Test]
    public void ModifierOverrideAreaDoesNotAffectSibling()
    {
        modifier.area = 4;
        modifier.overrideArea = true;
        modifier.GetComponent<MeshRenderer>().enabled = false;

        var siblingPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
        GameObject.DestroyImmediate(siblingPlane);
    }
}
#endif
