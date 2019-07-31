using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NavMeshBuilder2D))]
public class NavMeshBuilder2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Bake"))
        {
            (target as NavMeshBuilder2D).RebuildNavmesh(false);
            SceneView.RepaintAll();
        }
    }
}
