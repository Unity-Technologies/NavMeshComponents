#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

[TestFixture]
public class NavMeshSurfaceLinkTests
{
    public GameObject plane1, plane2;
    public NavMeshLink link;
    public NavMeshSurface surface;

    [SetUp]
    public void CreatesPlanesAndLink()
    {
        plane1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane1.transform.position = 11.0f * Vector3.right;

        surface = new GameObject().AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        Assert.IsFalse(HasPathConnecting(plane1, plane2));
        Assert.IsFalse(HasPathConnecting(plane2, plane1));

        link = new GameObject().AddComponent<NavMeshLink>();
        link.startPoint = plane1.transform.position;
        link.endPoint = plane2.transform.position;

        Assert.IsTrue(HasPathConnecting(plane1, plane2));
        Assert.IsTrue(HasPathConnecting(plane2, plane1));
    }

    [TearDown]
    public void DestroyPlanesAndLink()
    {
        GameObject.DestroyImmediate(surface.gameObject);
        GameObject.DestroyImmediate(link.gameObject);
        GameObject.DestroyImmediate(plane1);
        GameObject.DestroyImmediate(plane2);
    }

    [Test]
    public void NavMeshLinkCanConnectTwoSurfaces()
    {
        Assert.IsTrue(HasPathConnecting(plane1, plane2));
    }

    [Test]
    public void DisablingBidirectionalMakesTheLinkOneWay()
    {
        link.bidirectional = false;
        Assert.IsTrue(HasPathConnecting(plane1, plane2));
        Assert.IsFalse(HasPathConnecting(plane2, plane1));
    }

    [Test]
    public void ChangingAreaTypeCanBlockPath()
    {
        var areaMask = ~(1 << 4);
        Assert.IsTrue(HasPathConnecting(plane1, plane2, areaMask));

        link.area = 4;
        Assert.IsFalse(HasPathConnecting(plane1, plane2, areaMask));
    }

    [Test]
    public void EndpointsMoveRelativeToLinkOnUpdate()
    {
        link.transform.position += Vector3.forward;
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, plane1.transform.position + Vector3.forward));
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, plane2.transform.position + Vector3.forward));

        link.UpdateLink();

        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, plane1.transform.position + Vector3.forward));
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, plane2.transform.position + Vector3.forward));
    }

    [UnityTest]
    public IEnumerator EndpointsMoveRelativeToLinkNextFrameWhenAutoUpdating()
    {
        link.transform.position += Vector3.forward;
        link.autoUpdate = true;

        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, plane1.transform.position + Vector3.forward));
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, plane2.transform.position + Vector3.forward));

        yield return null;

        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, plane1.transform.position + Vector3.forward));
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, plane2.transform.position + Vector3.forward));
    }

    [Test]
    public void ChangingCostModifierAffectsRoute()
    {
        var link1 = link;
        link1.startPoint = plane1.transform.position;
        link1.endPoint = plane2.transform.position + Vector3.forward;

        var link2 = link.gameObject.AddComponent<NavMeshLink>();
        link2.startPoint = plane1.transform.position;
        link2.endPoint = plane2.transform.position - Vector3.forward;

        link1.costModifier = -1;
        link2.costModifier = 100;
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, link1.endPoint));
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, link2.endPoint));

        link1.costModifier = 100;
        link2.costModifier = -1;
        Assert.IsFalse(HasPathConnectingViaPoint(plane1, plane2, link1.endPoint));
        Assert.IsTrue(HasPathConnectingViaPoint(plane1, plane2, link2.endPoint));
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
#endif
