using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

[ExecuteInEditMode]
[DefaultExecutionOrder(-102)]
public class NavMeshPrefabInstance : MonoBehaviour
{
    [SerializeField]
    NavMeshData m_NavMesh;
    public NavMeshData navMeshData
    {
        get { return m_NavMesh; }
        set { m_NavMesh = value; }
    }

    [SerializeField]
    bool m_FollowTransform;
    public bool followTransform
    {
        get { return m_FollowTransform; }
        set { SetFollowTransform(value); }
    }

    NavMeshDataInstance m_Instance;

    // Position Tracking
    static readonly List<NavMeshPrefabInstance> s_TrackedInstances = new List<NavMeshPrefabInstance>();
    public static List<NavMeshPrefabInstance> trackedInstances {get {return s_TrackedInstances; }}
    Vector3 m_Position;
    Quaternion m_Rotation;

    void OnEnable()
    {
        AddInstance();

        if (m_Instance.valid && m_FollowTransform)
            AddTracking();
    }

    void OnDisable()
    {
        m_Instance.Remove();
        RemoveTracking();
    }

    public void UpdateInstance()
    {
        m_Instance.Remove();
        AddInstance();
    }

    void AddInstance()
    {
#if UNITY_EDITOR
        if (m_Instance.valid)
        {
            Debug.LogError("Instance is already added: " + this);
            return;
        }
#endif
        if (m_NavMesh)
            m_Instance = NavMesh.AddNavMeshData(m_NavMesh, transform.position, transform.rotation);

        m_Rotation = transform.rotation;
        m_Position = transform.position;
    }

    void AddTracking()
    {
#if UNITY_EDITOR
        // At runtime we don't want linear lookup
        if (s_TrackedInstances.Contains(this))
        {
            Debug.LogError("Double registration of " + this);
            return;
        }
#endif
        if (s_TrackedInstances.Count == 0)
            NavMesh.onPreUpdate += UpdateTrackedInstances;
        s_TrackedInstances.Add(this);
    }

    void RemoveTracking()
    {
        s_TrackedInstances.Remove(this);
        if (s_TrackedInstances.Count == 0)
            NavMesh.onPreUpdate -= UpdateTrackedInstances;
    }

    void SetFollowTransform(bool value)
    {
        if (m_FollowTransform == value)
            return;
        m_FollowTransform = value;
        if (value)
            AddTracking();
        else
            RemoveTracking();
    }

    bool HasMoved()
    {
        return m_Position != transform.position || m_Rotation != transform.rotation;
    }

    static void UpdateTrackedInstances()
    {
        foreach (var instance in s_TrackedInstances)
        {
            if (instance.HasMoved())
                instance.UpdateInstance();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Only when the instance is valid (OnEnable is called) - we react to changes caused by serialization
        if (!m_Instance.valid)
            return;
        // OnValidate can be called several times - avoid double registration
        // We afford this linear lookup in the editor only
        if (!m_FollowTransform)
        {
            RemoveTracking();
        }
        else if (!s_TrackedInstances.Contains(this))
        {
            AddTracking();
        }
    }
#endif
}
