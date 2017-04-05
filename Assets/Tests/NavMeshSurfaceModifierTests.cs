using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceModifierTests
{
    [Test]
    public void ModifierIgnoreAffectsSelf()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        surface.BuildNavMesh();

        Assert.IsFalse(NavMeshSurfaceTests.HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ModifierIgnoreAffectsChild()
    {
        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.SetParent(modifier.transform);

        surface.BuildNavMesh();

        Assert.IsFalse(NavMeshSurfaceTests.HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane);
    }

    [Test]
    public void ModifierIgnoreDoesNotAffectSibling()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        surface.BuildNavMesh();

        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane);
    }

    [Test]
    public void ModifierOverrideAreaAffectsSelf()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.area = 4;
        modifier.overrideArea = true;

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ModifierOverrideAreaAffectsChild()
    {
        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.area = 4;
        modifier.overrideArea = true;

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.SetParent(modifier.transform);

        surface.BuildNavMesh();

        var expectedAreaMask = 1 << 4;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane);
    }

    [Test]
    public void ModifierOverrideAreaDoesNotAffectSibling()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.area = 4;
        modifier.overrideArea = true;

        surface.BuildNavMesh();

        var expectedAreaMask = 1;
        Assert.IsTrue(NavMeshSurfaceTests.HasNavMeshAtOrigin(expectedAreaMask));
        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane);
    }
}
