using UnityEngine;

using System.Collections.Generic;

// Fill 5x5 tiles around the local position procedurally by instantiating prefabs at random positions/orientations
[DefaultExecutionOrder(-200)]
public class RandomInstancing : MonoBehaviour
{
    public GameObject m_Prefab;
    public int m_PoolSize = 250;
    public int m_InstancesPerTile = 10;
    public bool m_RandomPosition = true;
    public bool m_RandomOrientation = true;
    public float m_Height;

    public int m_BaseHash = 347652783;
    public float m_Size = 100.0f;

    List<Transform> m_Instances = new List<Transform>();
    int m_Seed;
    int m_Used;
    int m_LocX, m_LocZ;

    void Awake()
    {
        for (int i = 0; i < m_PoolSize; ++i)
        {
            var go = Instantiate(m_Prefab, Vector3.zero, Quaternion.identity) as GameObject;
            m_Instances.Add(go.transform);
        }
    }

    void OnEnable()
    {
        m_LocX = ~0;
        m_LocZ = ~0;
        UpdateInstances();
    }

    void OnDestroy()
    {
        for (int i = 0; i < m_Instances.Count; ++i)
        {
            if (m_Instances[i])
                DestroyObject(m_Instances[i].gameObject);
        }
        m_Instances.Clear();
    }

    void Update()
    {
        UpdateInstances();
    }

    void UpdateInstances()
    {
        var x = (int)Mathf.Floor(transform.position.x / m_Size);
        var z = (int)Mathf.Floor(transform.position.z / m_Size);
        if (x == m_LocX && z == m_LocZ)
            return;

        m_LocX = x;
        m_LocZ = z;

        m_Used = 0;
        for (var i = x - 2; i <= x + 2; ++i)
        {
            for (var j = z - 2; j <= z + 2; ++j)
            {
                if (m_Used >= m_PoolSize - 1)
                    return;
                UpdateTileInstances(i, j);
            }
        }
    }

    void UpdateTileInstances(int i, int j)
    {
        m_Seed = Hash2(i, j) ^ m_BaseHash;
        for (var k = 0; k < m_InstancesPerTile; ++k)
        {
            float x = 0;
            float y = 0;

            if (m_RandomPosition)
            {
                x = Random();
                y = Random();
            }
            var pos = new Vector3((i + x) * m_Size, m_Height, (j + y) * m_Size);

            if (m_RandomOrientation)
            {
                float r = 360.0f * Random();
                m_Instances[m_Used].rotation = Quaternion.AngleAxis(r, Vector3.up);
            }
            m_Instances[m_Used].position = pos;
            m_Used++;
        }
    }

    static int Hash2(int i, int j)
    {
        return (i * 73856093) ^ (j * 19349663);
    }

    float Random()
    {
        m_Seed = (m_Seed ^ 123459876);
        var k = m_Seed / 127773;
        m_Seed = 16807 * (m_Seed - k * 127773) - 2836 * k;
        if (m_Seed < 0) m_Seed = m_Seed + 2147483647;
        float ran0 = m_Seed * 1.0f / 2147483647.0f;
        m_Seed = (m_Seed ^ 123459876);
        return ran0;
    }
}
