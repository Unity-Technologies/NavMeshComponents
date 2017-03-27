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

    public static bool HasPathConnecting(GameObject a, GameObject b, int areaMask = NavMesh.AllAreas, int agentTypeID = 0)
    {
        var path = new NavMeshPath();
        var filter = new NavMeshQueryFilter();
        filter.areaMask = areaMask;
        filter.agentTypeID = agentTypeID;
        NavMesh.CalculatePath(a.transform.position, b.transform.position, filter, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }
}
