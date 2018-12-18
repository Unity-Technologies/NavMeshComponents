#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NavMeshSurfaceAgentTests
{
    NavMeshSurface m_Surface;
    NavMeshAgent m_Agent;

    [SetUp]
    public void Setup()
    {
        m_Surface = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<NavMeshSurface>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_Agent.gameObject);
        Object.DestroyImmediate(m_Surface.gameObject);
        m_Agent = null;
        m_Surface = null;
    }

    [Test]
    public void AgentIdentifiesSurfaceOwner()
    {
        m_Surface.BuildNavMesh();

        m_Agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.AreEqual(m_Surface, m_Agent.navMeshOwner);
        Assert.IsTrue(m_Agent.isOnNavMesh);
    }

    [Test]
    [Ignore("1012991 : Missing functionality for notifying the NavMeshAgent about the removal of the NavMesh.")]
    public void AgentDetachesAndAttachesToSurface()
    {
        m_Surface.BuildNavMesh();

        m_Agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.AreEqual(m_Surface, m_Agent.navMeshOwner);
        Assert.IsTrue(m_Agent.isOnNavMesh);

        m_Surface.enabled = false;
        Assert.IsNull(m_Agent.navMeshOwner);
        Assert.IsFalse(m_Agent.isOnNavMesh);

        m_Surface.enabled = true;
        Assert.AreEqual(m_Surface, m_Agent.navMeshOwner);
        Assert.IsTrue(m_Agent.isOnNavMesh);
    }


/*
    [Test]
    public void AgentIsOnNavMeshWhenMatchingAgentTypeID()
    {
        m_Surface.agentTypeID = 1234;
        m_Surface.BuildNavMesh();

        m_Agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        Assert.IsFalse(m_Agent.isOnNavMesh);

        m_Agent.agentTypeID = 1234;
        Assert.IsTrue(m_Agent.isOnNavMesh);
    }
*/

    [UnityTest]
    public IEnumerator AgentAlignsToSurfaceNextFrame()
    {
        m_Surface.transform.rotation = new Quaternion(-0.679622f, 0.351242f, -0.373845f, 0.524388f);
        m_Surface.BuildNavMesh();

        m_Agent = new GameObject("Agent").AddComponent<NavMeshAgent>();

        yield return null;

        var residual = m_Surface.transform.up - m_Agent.transform.up;
        Assert.IsTrue(residual.magnitude < 0.01f);
    }

    [UnityTest]
    public IEnumerator AgentDoesNotAlignToSurfaceNextFrame()
    {
        m_Surface.transform.rotation = new Quaternion(-0.679622f, 0.351242f, -0.373845f, 0.524388f);
        m_Surface.BuildNavMesh();

        m_Agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
        m_Agent.updateUpAxis = false;

        yield return null;

        var residual = Vector3.up - m_Agent.transform.up;
        Assert.IsTrue(residual.magnitude < 0.01f);
    }
}
#endif
