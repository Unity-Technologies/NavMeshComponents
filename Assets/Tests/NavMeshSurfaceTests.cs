using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceTests
{
    [Test]
    public void NavMeshIsAvailableAfterBuild()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();

        Assert.IsFalse(HasNavMeshAtOrigin());

        surface.BuildNavMesh();
        Assert.IsTrue(HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void NavMeshCanBeRemovedAndAdded()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();

        surface.BuildNavMesh();
        Assert.IsTrue(HasNavMeshAtOrigin());

        surface.RemoveData();
        Assert.IsFalse(HasNavMeshAtOrigin());

        surface.AddData();
        Assert.IsTrue(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void NavMeshIsNotAvailableWhenDisabledOrDestroyed()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        surface.enabled = false;
        Assert.IsFalse(HasNavMeshAtOrigin());

        surface.enabled = true;
        Assert.IsTrue(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
        Assert.False(HasNavMeshAtOrigin());
    }

    [Test]
    public void CanBuildWithCustomArea()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.defaultArea = 4;
        var expectedAreaMask = 1 << 4;

        surface.BuildNavMesh();
        Assert.IsTrue(HasNavMeshAtOrigin(expectedAreaMask));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void CanBuildWithCustomAgentTypeID()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 1234;
        surface.BuildNavMesh();

        Assert.IsTrue(HasNavMeshAtOrigin(NavMesh.AllAreas, 1234));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void CanBuildCollidersAndIgnoreRenderMeshes()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.GetComponent<MeshRenderer>().enabled = false;

        var surface = go.AddComponent<NavMeshSurface>();

        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        Assert.IsTrue(HasNavMeshAtOrigin());

        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void CanBuildRenderMeshesAndIgnoreColliders()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.GetComponent<Collider>().enabled = false;

        var surface = go.AddComponent<NavMeshSurface>();

        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
        Assert.IsTrue(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void BuildIgnoresGeometryOutsideBounds()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(20, 0, 0);
        surface.size = new Vector3(10, 10, 10);

        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void BuildIgnoresGeometrySiblings()
    {
        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane);
    }

    [Test]
    public void BuildUsesOnlyIncludedLayers()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        go.layer = 4;
        surface.layerMask = ~(1 << 4);

        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void DefaultSettingsMatchBuiltinSettings()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        var bs = surface.GetBuildSettings();
        Assert.AreEqual(NavMesh.GetSettingsByIndex(0), bs);

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ActiveSurfacesContainsOnlyActiveAndEnabledSurface()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();
        Assert.IsTrue(NavMeshSurface.activeSurfaces.Contains(surface));
        Assert.AreEqual(1, NavMeshSurface.activeSurfaces.Count);

        surface.enabled = false;
        Assert.IsFalse(NavMeshSurface.activeSurfaces.Contains(surface));
        Assert.AreEqual(0, NavMeshSurface.activeSurfaces.Count);

        surface.enabled = true;
        go.SetActive(false);
        Assert.IsFalse(NavMeshSurface.activeSurfaces.Contains(surface));
        Assert.AreEqual(0, NavMeshSurface.activeSurfaces.Count);

        GameObject.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator NavMeshMovesToSurfacePositionNextFrame()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.transform.position = new Vector3(100, 0, 0);
        var surface = go.AddComponent<NavMeshSurface>();

        surface.BuildNavMesh();
        Assert.IsFalse(HasNavMeshAtOrigin());

        go.transform.position = Vector3.zero;
        Assert.IsFalse(HasNavMeshAtOrigin());

        yield return null;

        Assert.IsTrue(HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator UpdatingAndAddingNavMesh()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var surface = go.AddComponent<NavMeshSurface>();

        var navmeshData = new NavMeshData();
        var oper = surface.UpdateNavMesh(navmeshData);
        Assert.IsFalse(HasNavMeshAtOrigin());

        do { yield return null; } while (!oper.isDone);
        surface.RemoveData();
        surface.navMeshData = navmeshData;
        surface.AddData();

        Assert.IsTrue(HasNavMeshAtOrigin());
        GameObject.DestroyImmediate(go);
    }

    public static bool HasNavMeshAtOrigin(int areaMask = NavMesh.AllAreas, int agentTypeID = 0)
    {
        var hit = new NavMeshHit();
        var filter = new NavMeshQueryFilter();
        filter.areaMask = areaMask;
        filter.agentTypeID = agentTypeID;
        return NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
    }
}
