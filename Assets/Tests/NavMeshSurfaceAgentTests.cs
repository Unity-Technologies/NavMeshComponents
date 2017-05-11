#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceAgentTests
{
    [Test]
    public void AgentIdentifiesSurfaceOwner()
    {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        var agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.AreEqual(surface, agent.navMeshOwner);
        Assert.IsTrue(agent.isOnNavMesh);

        GameObject.DestroyImmediate(agent.gameObject);
        GameObject.DestroyImmediate(surface.gameObject);
    }

    [Test]
    public void AgentDetachesAndAttachesToSurface()
    {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        var agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.AreEqual(surface, agent.navMeshOwner);
        Assert.IsTrue(agent.isOnNavMesh);

        surface.enabled = false;
        Assert.IsNull(agent.navMeshOwner);
        Assert.IsFalse(agent.isOnNavMesh);

        surface.enabled = true;
        Assert.AreEqual(surface, agent.navMeshOwner);
        Assert.IsTrue(agent.isOnNavMesh);

        GameObject.DestroyImmediate(agent.gameObject);
        GameObject.DestroyImmediate(surface.gameObject);
    }

/*
    [Test]
    public void AgentIsOnNavMeshWhenMatchingAgentTypeID()
    {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
        surface.agentTypeID = 1234;
        surface.BuildNavMesh();

        var agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.IsFalse(agent.isOnNavMesh);

        agent.agentTypeID = 1234;
        Assert.IsTrue(agent.isOnNavMesh);

        GameObject.DestroyImmediate(agent.gameObject);
        GameObject.DestroyImmediate(surface.gameObject);
    }
*/

    [UnityTest]
    public IEnumerator AgentAlignsToSurfaceNextFrame()
    {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
        surface.transform.rotation = new Quaternion(-0.679622f, 0.351242f, -0.373845f, 0.524388f);
        surface.BuildNavMesh();

        var agent = new GameObject("Agent").AddComponent<NavMeshAgent>();

        yield return null;

        var residual = surface.transform.up - agent.transform.up;
        Assert.IsTrue(residual.magnitude < 0.01f);

        GameObject.DestroyImmediate(agent.gameObject);
        GameObject.DestroyImmediate(surface.gameObject);
    }

    [UnityTest]
    public IEnumerator AgentDoesNotAlignToSurfaceNextFrame()
    {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
        surface.transform.rotation = new Quaternion(-0.679622f, 0.351242f, -0.373845f, 0.524388f);
        surface.BuildNavMesh();

        var agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        agent.updateUpAxis = false;

        yield return null;

        var residual = Vector3.up - agent.transform.up;
        Assert.IsTrue(residual.magnitude < 0.01f);

        GameObject.DestroyImmediate(agent.gameObject);
        GameObject.DestroyImmediate(surface.gameObject);
    }
}
#endif
