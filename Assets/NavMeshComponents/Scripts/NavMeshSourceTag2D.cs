using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[DefaultExecutionOrder(-200)]
[ExecuteAlways]
public class NavMeshSourceTag2D : MonoBehaviour
{
    public static List<NavMeshSourceTag2D> tags = new List<NavMeshSourceTag2D>();

    public int area;
    Collider2D collider2D;

    Mesh cachedMesh;
    uint shapeHash;

    void OnEnable()
    {
        collider2D = GetComponent<Collider2D>();
        tags.Add(this);
    }

    void OnDisable()
    {
        tags.Remove(this);
        DestroyMesh();
    }

    void UpdateCachedMesh()
    {
        if (collider2D == null)
        {
            DestroyMesh();
            return;
        }
        if (cachedMesh == null)
        {
            CreateMesh();
            return;
        }
        if (collider2D.GetShapeHash() != shapeHash)
        {
            DestroyMesh();
            CreateMesh();
        }
    }

    void CreateMesh()
    {
        cachedMesh = collider2D.CreateMesh(false, false);
        shapeHash = collider2D.GetShapeHash();
    }

    void DestroyMesh()
    {
        if (cachedMesh == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(cachedMesh);
        }
        else
        {
            DestroyImmediate(cachedMesh);
        }
        shapeHash = 0;
    }

    // Collect all the navmesh build sources for enabled objects tagged by this component
    public static void Collect(ref List<NavMeshBuildSource> sources, ref Bounds bounds)
    {
        sources.Clear();
        for (var i = 0; i < tags.Count; ++i)
        {
            var tag = tags[i];
            if (tag == null) continue;

            var collider2D = tag.collider2D;
            if (collider2D == null) continue;
            if (!collider2D.enabled) continue;

            tag.UpdateCachedMesh();

            if (tag.cachedMesh == null)
                continue;

            var colliderBounds = collider2D.bounds;
            bounds.Encapsulate(colliderBounds.min);
            bounds.Encapsulate(colliderBounds.max);
            //bounds.Encapsulate(colliderBounds);

            var buildSource = new NavMeshBuildSource();
            buildSource.shape = NavMeshBuildSourceShape.Mesh;
            buildSource.sourceObject = tag.cachedMesh;
            if (collider2D.attachedRigidbody)
            {
                buildSource.transform = Matrix4x4.TRS(collider2D.transform.position, collider2D.transform.rotation, Vector3.one);
            }
            else
            {
                buildSource.transform = Matrix4x4.identity;
            }
            buildSource.area = tag.area;
            sources.Add(buildSource);

        }
    }
}
