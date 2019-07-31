using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class NavMeshBuilder2D : MonoBehaviour
{
    public enum UpdateMethod
    {
        Manual,
        Update,
        FixedUpdate,
    }

    public bool bakeOnEnable;
    public UpdateMethod updateMethod;
    public bool updateAsync;

    Collider2D[] collider2Ds;

    List<NavMeshBuildSource> buildSources = new List<NavMeshBuildSource>();
    NavMeshData data;
    NavMeshDataInstance dataInstance;

    static readonly Quaternion Plane2DRotation = Quaternion.AngleAxis(-90f, Vector3.right);
    static readonly Quaternion Plane2DRotationInverse = Quaternion.Inverse(Plane2DRotation);

    AsyncOperation buildOperation;

    private void OnEnable()
    {
        if (bakeOnEnable)
            RebuildNavmesh(false);
    }

    private void OnDisable()
    {
        buildOperation = null;
        dataInstance.Remove();
    }

    private void Update()
    {
        var shouldUpdate = updateMethod == UpdateMethod.Update;
#if UNITY_EDITOR
        shouldUpdate |= (updateMethod == UpdateMethod.FixedUpdate) && !Application.isPlaying;
#endif
        if (shouldUpdate)
            RebuildNavmesh(updateAsync);
    }

    private void FixedUpdate()
    {
        if (updateMethod == UpdateMethod.FixedUpdate)
            RebuildNavmesh(updateAsync);
    }

    public void RebuildNavmesh(bool async)
    {
        if (async && buildOperation != null && !buildOperation.isDone)
            return;

        buildOperation = null;

        var totalBounds = new Bounds();
        NavMeshSourceTag2D.Collect(ref buildSources, ref totalBounds);
        var buildSettings = NavMesh.GetSettingsByID(0);
        var totalBoundsReversed = new Bounds(Plane2DRotationInverse * totalBounds.center, Plane2DRotationInverse * totalBounds.size); // We need to reverse the rotation that's going to be used for baking to get proper bounds
        var buildBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue); //Using enclosing and empty bounds is bugged at the moment - use arbitrarily big one 
        if (!data)
        {
            data = NavMeshBuilder.BuildNavMeshData(buildSettings, buildSources, buildBounds, totalBounds.center, Plane2DRotation);
        }
        else
        {
            if (async)
            {
                buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(data, buildSettings, buildSources, buildBounds);
            }
            else
            {
                NavMeshBuilder.UpdateNavMeshData(data, buildSettings, buildSources, buildBounds);
            }
        }

        dataInstance.Remove();
        dataInstance = NavMesh.AddNavMeshData(data);
    }

    private void OnDrawGizmosSelected()
    {
        if (!data)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.Rotate(data.rotation);
        Gizmos.DrawWireCube(data.sourceBounds.center, data.sourceBounds.size);
    }
}
