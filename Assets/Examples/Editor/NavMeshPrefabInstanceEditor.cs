using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CanEditMultipleObjects]
[CustomEditor(typeof(NavMeshPrefabInstance))]
class NavMeshPrefabInstanceEditor : Editor
{
    SerializedProperty m_FollowTransformProp;
    SerializedProperty m_NavMeshDataProp;

    public void OnEnable()
    {
        m_FollowTransformProp = serializedObject.FindProperty("m_FollowTransform");
        m_NavMeshDataProp = serializedObject.FindProperty("m_NavMesh");
    }

    public override void OnInspectorGUI()
    {
        var instance = (NavMeshPrefabInstance)target;
        var go = instance.gameObject;

        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(m_NavMeshDataProp);
        GUI.enabled = true;
        EditorGUILayout.PropertyField(m_FollowTransformProp);

        EditorGUILayout.Space();

        OnInspectorGUIPrefab(go);

        serializedObject.ApplyModifiedProperties();
    }

    string GetAssetPath(GameObject go)
    {
        var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.IsPartOfPrefabContents(go))
        {
            return stage.prefabAssetPath;
        }
        return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
    }

    void OnInspectorGUIPrefab(GameObject go)
    {
        var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || !stage.IsPartOfPrefabContents(go))
        {
            if (GUILayout.Button("Edit the Prefab asset to bake or clear the navmesh", EditorStyles.helpBox))
            {
                Selection.activeObject = PrefabUtility.GetCorrespondingObjectFromSource(go);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            return;
        }

        var path = GetAssetPath(go);
        if (string.IsNullOrEmpty(path))
            return;

        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);

        if (GUILayout.Button("Clear"))
            OnClear();

        if (GUILayout.Button("Bake"))
            OnBake();

        GUILayout.EndHorizontal();
    }

    NavMeshData Build(NavMeshPrefabInstance instance)
    {
        var root = instance.transform;
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();

        UnityEditor.AI.NavMeshBuilder.CollectSourcesInStage(
            root, ~0, NavMeshCollectGeometry.RenderMeshes, 0, markups, instance.gameObject.scene, sources);
        var settings = NavMesh.GetSettingsByID(0);
        var bounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
        var navmesh = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, root.position, root.rotation);
        navmesh.name = "Navmesh";
        return navmesh;
    }

    void OnClear()
    {
        foreach (var tgt in targets)
        {
            var instance = (NavMeshPrefabInstance)tgt;
            var go = instance.gameObject;
            var path = GetAssetPath(go);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("GameObject: " + go + " has no valid prefab path");
                continue;
            }

            DestroyNavMeshData(path);
            AssetDatabase.SaveAssets();
        }
    }

    void OnBake()
    {
        foreach (var tgt in targets)
        {
            var instance = (NavMeshPrefabInstance)tgt;
            var go = instance.gameObject;
            var path = GetAssetPath(go);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("GameObject: " + go + " has no valid prefab path");
                continue;
            }

            DestroyNavMeshData(path);

            // Store navmesh as a sub-asset of the prefab
            var navmesh = Build(instance);
            AssetDatabase.AddObjectToAsset(navmesh, prefab);

            instance.navMeshData = navmesh;
            AssetDatabase.SaveAssets();
        }
    }

    void DestroyNavMeshData(string path)
    {
        // Destroy and remove all existing NavMeshData sub-assets
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var o in assets)
        {
            var data = o as NavMeshData;
            if (data != null)
                DestroyImmediate(o, true);
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
    static void RenderGizmo(NavMeshPrefabInstance instance, GizmoType gizmoType)
    {
        if (!EditorApplication.isPlaying)
            instance.UpdateInstance();
    }
}
