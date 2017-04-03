using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceLinkTests
{
    [Test]
    public void NavMeshLinkCanConnectTwoSurfaces()
    {
        var plane1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var plane2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane1.transform.position = 11.0f * Vector3.right;

        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        Assert.IsFalse(HasPathConnecting(plane1, plane2));

        var linkGO = new GameObject();
        var link = linkGO.AddComponent<NavMeshLink>();
        link.startPoint = plane1.transform.position;
        link.endPoint = plane2.transform.position;
        link.UpdateLink();

        Assert.IsTrue(HasPathConnecting(plane1, plane2));

        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane1);
        GameObject.DestroyImmediate(plane2);
        GameObject.DestroyImmediate(linkGO);
    }

    [Test]
    public void ChangingCostModifierAffectsRoute()
    {
        var plane1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var plane2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane1.transform.position = 11.0f * Vector3.right;

        var go = new GameObject();
        var surface = go.AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        Assert.IsFalse(HasPathConnecting(plane1, plane2));

        var linkGO = new GameObject();

        var link1 = linkGO.AddComponent<NavMeshLink>();
        link1.startPoint = plane1.transform.position;
        link1.endPoint = plane2.transform.position + Vector3.forward;
        link1.UpdateLink();

        var link2 = linkGO.AddComponent<NavMeshLink>();
        link2.startPoint = plane1.transform.position;
        link2.endPoint = plane2.transform.position - Vector3.forward;
        link2.UpdateLink();

        link1.costModifier = -1;
        link2.costModifier = 100;
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, link1.endPoint));
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, link2.endPoint));

        link1.costModifier = 100;
        link2.costModifier = -1;
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, link1.endPoint));
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, link2.endPoint));

        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(plane1);
        GameObject.DestroyImmediate(plane2);
        GameObject.DestroyImmediate(linkGO);
    }

    public static bool HasPathConnecting(GameObject a, GameObject b, int areaMask = NavMesh.AllAreas, int agentTypeID = 0)
    {
        var path = new NavMeshPath();
        var filter = new NavMeshQueryFilter();
        filter.areaMask = areaMask;
        filter.agentTypeID = agentTypeID;
        NavMesh.CalculatePath(a.transform.position, b.transform.position, filter, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }

    public static bool HasPathConnectingViaPoint(GameObject a, GameObject b, Vector3 point, int areaMask = NavMesh.AllAreas, int agentTypeID = 0)
    {
        var path = new NavMeshPath();
        var filter = new NavMeshQueryFilter();
        filter.areaMask = areaMask;
        filter.agentTypeID = agentTypeID;
        NavMesh.CalculatePath(a.transform.position, b.transform.position, filter, path);
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        for (int i = 0; i < path.corners.Length; ++i)
            if (Vector3.Distance(path.corners[i], point) < 0.1f)
                return true;
        return false;
    }
}
