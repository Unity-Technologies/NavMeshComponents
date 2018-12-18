//#define KEEP_ARTIFACTS_FOR_INSPECTION

using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[Category("PrefabsWithNavMeshModifierVolume")]
public class NavMeshModifierVolumeInPrefabTests
{
    const string k_AutoSaveKey = "AutoSave";
    const string k_ParentFolder = "Assets/Tests/Editor";
    const string k_TempFolderName = "TempPrefabAndModifiers";
    string m_TempFolder = k_ParentFolder + "/" + k_TempFolderName;
    string m_PrefabPath;
    string m_PreviousScenePath;
    string m_TempScenePath;
    int m_TestCounter;

    const int k_PinkArea = 3;
    const int k_GreenArea = 4;
    const int k_RedArea = 18;

    const int k_PrefabDefaultArea = k_GreenArea;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        AssetDatabase.DeleteAsset(m_TempFolder);

        var folderGUID = AssetDatabase.CreateFolder(k_ParentFolder, k_TempFolderName);
        m_TempFolder = AssetDatabase.GUIDToAssetPath(folderGUID);

        SessionState.SetBool(k_AutoSaveKey, PrefabStageAutoSavingUtil.GetPrefabStageAutoSave());
        PrefabStageAutoSavingUtil.SetPrefabStageAutoSave(false);
        StageUtility.GoToMainStage();

        m_PreviousScenePath = SceneManager.GetActiveScene().path;
        m_TempScenePath = Path.Combine(m_TempFolder, "NavMeshModifierVolumePrefabTestsScene.unity");
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(tempScene, m_TempScenePath);
        EditorSceneManager.OpenScene(m_TempScenePath);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        PrefabStageAutoSavingUtil.SetPrefabStageAutoSave(SessionState.GetBool(k_AutoSaveKey, PrefabStageAutoSavingUtil.GetPrefabStageAutoSave()));
        StageUtility.GoToMainStage();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        if (string.IsNullOrEmpty(m_PreviousScenePath))
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        AssetDatabase.DeleteAsset(m_TempFolder);
#endif
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "SurfaceSeekingModVol" + (++m_TestCounter) + "Prefab";
        var surface = plane.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;

        m_PrefabPath = Path.Combine(m_TempFolder, plane.name + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(plane, m_PrefabPath);
        Object.DestroyImmediate(plane);

        NavMesh.RemoveAllNavMeshData();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        StageUtility.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator ModifierVolume_WhenInsidePrefabMode_ModifiesTheNavMeshInPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "SurfaceSeekingModVol" + m_TestCounter + "PrefabInstance";

        NavMeshHit hit;
        var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
        NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
        Assert.That(hit.hit, Is.False, "Prefab should not have a NavMesh in the beginning.");

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var modifierVolume = prefabStage.prefabContentsRoot.AddComponent<NavMeshModifierVolume>();
        modifierVolume.area = k_RedArea;
        modifierVolume.center = Vector3.zero;
        modifierVolume.size = Vector3.one;
        yield return BakeNavMeshAsync(() => prefabSurface, k_PrefabDefaultArea);
        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        NavMeshHit hitCenter;
        NavMesh.SamplePosition(Vector3.zero, out hitCenter, 0.1f, filter);
        Assert.That(hitCenter.hit, Is.True, "A NavMesh should have been baked in the center of the prefab.");
        Assert.That(hitCenter.mask, Is.EqualTo(1 << k_RedArea),
            "Area type (0x{0:x8}) found in the center should be 0x{1:x8}.", hitCenter.mask, 1 << k_RedArea);

        NavMeshHit hitSides;
        NavMesh.SamplePosition(new Vector3(0.6f, 0, 0.6f), out hitSides, 0.1f, filter);
        Assert.That(hitSides.hit, Is.True, "A NavMesh should have been baked in the outer sides of the prefab.");
        Assert.That(hitSides.mask, Is.EqualTo(1 << k_PrefabDefaultArea),
            "Area type (0x{0:x8}) found on the sides should be 0x{1:x8}.", hitSides.mask, 1 << k_PrefabDefaultArea);

        Assert.That(hitCenter.mask, Is.Not.EqualTo(hitSides.mask),
            "Area type (0x{0:x8}) in the center should be different than on the sides.", hitCenter.mask);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
    }

    [UnityTest]
    public IEnumerator ModifierVolume_WhenInsidePrefabMode_DoesNotAffectTheNavMeshInMainScene()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "SurfaceOutsidePrefab";
        var mainSceneSurface = go.AddComponent<NavMeshSurface>();
        mainSceneSurface.defaultArea = k_PinkArea;
        mainSceneSurface.agentTypeID = 0;
        mainSceneSurface.collectObjects = CollectObjects.All;

        NavMeshHit hit;
        var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
        NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
        Assert.That(hit.hit, Is.False, "Prefab should not have a NavMesh in the beginning.");

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabModVol = prefabStage.prefabContentsRoot.AddComponent<NavMeshModifierVolume>();
        prefabModVol.area = k_PrefabDefaultArea;
        prefabModVol.center = Vector3.zero;
        prefabModVol.size = new Vector3(100, 100, 100);

        // bake the NavMeshSurface from the main scene while the prefab mode is open
        yield return BakeNavMeshAsync(() => mainSceneSurface, mainSceneSurface.defaultArea);

        PrefabSavingUtil.SavePrefab(prefabStage);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        StageUtility.GoToMainStage();

        NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
        Assert.That(hit.hit, Is.True, "A NavMesh should have been baked by the surface in the main scene.");
        Assert.That(hit.mask, Is.EqualTo(1 << mainSceneSurface.defaultArea),
            "NavMesh has the area type 0x{0:x8} instead of the expected 0x{1:x8}.", hit.mask, 1 << mainSceneSurface.defaultArea);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(go);
#endif
    }

    [UnityTest]
    public IEnumerator ModifierVolume_WhenOutsidePrefabMode_DoesNotAffectTheNavMeshInPrefab()
    {
        var go = new GameObject("ModifierVolumeOutsidePrefab");
        var modifierVolume = go.AddComponent<NavMeshModifierVolume>();
        modifierVolume.area = k_RedArea;
        modifierVolume.center = Vector3.zero;
        modifierVolume.size = new Vector3(20, 20, 20);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "SurfaceSeekingModVol" + m_TestCounter + "PrefabInstance";

        NavMeshHit hit;
        var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
        NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
        Assert.That(hit.hit, Is.False, "Prefab should not have a NavMesh in the beginning.");

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => prefabSurface, k_PrefabDefaultArea);
        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        NavMesh.SamplePosition(Vector3.zero, out hit, 0.1f, filter);
        Assert.That(hit.hit, Is.True, "A NavMesh should have been baked in the prefab.");
        Assert.That(hit.mask, Is.EqualTo(1 << k_PrefabDefaultArea),
            "A different area type (0x{0:x8}) was found instead of the expected one (0x{1:x8}).", hit.mask, 1 << k_PrefabDefaultArea);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(instance);
#endif
    }

    static IEnumerator BakeNavMeshAsync(Func<NavMeshSurface> getSurface, int defaultArea)
    {
        var surface = getSurface();
        surface.defaultArea = defaultArea;
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        yield return new WaitWhile(() => NavMeshAssetManager.instance.IsSurfaceBaking(surface));
    }
}
