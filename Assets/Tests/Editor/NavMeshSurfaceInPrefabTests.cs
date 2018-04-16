#define NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE

using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[Category("PrefabsWithNavMeshComponents")]
public class NavMeshSurfaceInPrefabTests
{
    const string k_AutoSaveKey = "AutoSave";
    const string k_TempFolder = "Assets/Tests/Editor/TempPrefabs";
    string m_PrefabPath = "";
    int m_TestCounter;

    const int k_BlueArea = 0;
    const int k_PinkArea = 3;
    const int k_GreenArea = 4;
    const int k_GrayArea = 7;
    const int k_BrownArea = 10;
    const int k_RedArea = 18;
    const int k_OrangeArea = 26;
    const int k_YellowArea = 30;

    const int k_PrefabDefaultArea = k_YellowArea;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        //if (System.IO.Directory.Exists(k_TempFolder))
            AssetDatabase.DeleteAsset(k_TempFolder);

        //if (!System.IO.Directory.Exists(k_TempFolder))
            AssetDatabase.CreateFolder("Assets/Tests/Editor", "TempPrefabs");

        SessionState.SetBool(k_AutoSaveKey, StageManager.instance.autoSave);
        StageManager.instance.autoSave = false;
        StageManager.instance.GoToMainStage();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        StageManager.instance.autoSave = SessionState.GetBool(k_AutoSaveKey, StageManager.instance.autoSave);
        StageManager.instance.GoToMainStage();

        if (System.IO.Directory.Exists(k_TempFolder))
            AssetDatabase.DeleteAsset(k_TempFolder);
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "NavMeshSurfacePrefab" + (++m_TestCounter);
        var surface = plane.AddComponent<NavMeshSurface>();
        surface.defaultArea = k_PrefabDefaultArea;
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
        CreateNavMeshAsset(surface);

        m_PrefabPath = Path.Combine(k_TempFolder, plane.name + ".prefab");
        PrefabUtility.CreatePrefab(m_PrefabPath, plane);

        Object.DestroyImmediate(plane);

        NavMesh.RemoveAllNavMeshData();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        //if (System.IO.File.Exists(m_PrefabPath))
        //{
        //    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        //    AssetDatabase.OpenAsset(prefab);
        //    var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        //    if (prefabScene != null && prefabScene.prefabInstanceRoot != null)
        //    {
        //        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        //        if (prefabSurface != null)
        //        {
        //            DeleteNavMeshAsset(prefabSurface);
        //        }
        //    }

        //    AssetDatabase.DeleteAsset(m_PrefabPath);
        //}

        StageManager.instance.GoToMainStage();

        yield return null;
    }

    static void TestNavMeshExistsAloneAtPosition(int expectedArea, Vector3 pos)
    {
        var expectedAreaMask = 1 << expectedArea;

        var areaExists = HasNavMeshAtPosition(pos, expectedAreaMask);
        var otherAreasExist = HasNavMeshAtPosition(pos, ~expectedAreaMask);
        Debug.Log(" mask=" + expectedAreaMask.ToString("x8") + " area " + expectedArea + " Exists=" + areaExists + " otherAreasExist=" + otherAreasExist + " at position " + pos);
        if (otherAreasExist)
        {
            for (int i = 0; i < 32; i++)
            {
                if (i == expectedArea)
                    continue;

                var thisOtherAreaExists = HasNavMeshAtPosition(pos, 1 << i);
                if (thisOtherAreaExists)
                {
                    Debug.Log(" _another area that exists here " + i);
                }
            }
        }

        Assert.IsTrue(HasNavMeshAtPosition(pos, expectedAreaMask), "Expected NavMesh with area {0} at position {1}.", expectedArea, pos);
        Assert.IsFalse(HasNavMeshAtPosition(pos, ~expectedAreaMask), "A NavMesh with an area other than {0} exists at position {1}.", expectedArea, pos);
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterEditing_LeavesMainSceneUntouched()
    {
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        Assert.IsNotNull(prefabScene);
        Assert.IsNotNull(prefabScene.prefabInstanceRoot);

        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        prefabSurface.defaultArea = k_RedArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);

        Assert.AreNotSame(prefabSurface.navMeshData, initialPrefabNavMeshData);

        prefabScene.SavePrefab();
        StageManager.instance.GoToMainStage();

        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, NavMesh.AllAreas, 0, 1000.0f));

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstantiated_ReferencesTheSameNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        var instanceNavMeshData = instanceSurface.navMeshData;

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "PrefabInstanceClone";

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        Assert.IsNotNull(prefabScene);
        Assert.IsNotNull(prefabScene.prefabInstanceRoot);

        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreSame(prefabNavMeshData, instanceNavMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageManager.instance.GoToMainStage();

        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenEmptyAndInstantiated_InstanceHasEmptyNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface.navMeshData);

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        DeleteNavMeshAsset(prefabSurface);
#endif
        prefabSurface.RemoveData();
        prefabSurface.navMeshData = null;
        prefabScene.SavePrefab();

        StageManager.instance.GoToMainStage();
        Assert.IsNull(instanceSurface.navMeshData);
        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        Object.DestroyImmediate(instance);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenBakesNewNavMesh_UpdatesTheInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instanceOne = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceOne);
        instanceOne.name = "PrefabInstanceOne" + m_TestCounter;
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();

        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        var initialNavMeshData = prefabSurface.navMeshData;
#endif
        prefabSurface.defaultArea = k_RedArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);

        prefabScene.SavePrefab();

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialNavMeshData));
#endif
        StageManager.instance.GoToMainStage();

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var instanceOneSurface = instanceOne.GetComponent<NavMeshSurface>();
        instanceOneSurface.defaultArea = k_BrownArea;
        instanceOneSurface.BuildNavMesh();
        CreateNavMeshAsset(instanceOneSurface);

        var instanceTwo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceTwo);
        instanceTwo.name = "PrefabInstanceTwo" + m_TestCounter;
        // reactivate the object to apply the change of position immediately
        instanceTwo.SetActive(false);
        instanceTwo.transform.position = new Vector3(20, 0, 0);
        instanceTwo.SetActive(true);

        TestNavMeshExistsAloneAtPosition(k_BrownArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_RedArea, instanceTwo.transform.position);

        Object.DestroyImmediate(instanceOne);
        Object.DestroyImmediate(instanceTwo);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRebaked_HasDifferentNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "PrefabInstanceClone";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        instanceSurface.defaultArea = k_RedArea;
        instanceSurface.BuildNavMesh();
        //CreateNavMeshAsset(instanceSurface);
        var instanceNavMeshData = instanceSurface.navMeshData;

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreNotSame(prefabNavMeshData, instanceNavMeshData);
        Assert.AreNotSame(instanceCloneNavMeshData, instanceNavMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageManager.instance.GoToMainStage();

        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceCleared_InstanceHasEmptyNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "PrefabInstanceClone";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        instanceSurface.RemoveData();
        instanceSurface.navMeshData = null;

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreNotSame(prefabNavMeshData, instanceSurface.navMeshData);
        Assert.AreNotSame(instanceCloneNavMeshData, instanceSurface.navMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageManager.instance.GoToMainStage();

        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceCleared_PrefabKeepsNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        var initialPrefabNavMeshData = instanceSurface.navMeshData;

        instanceSurface.RemoveData();
        instanceSurface.navMeshData = null;

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsNotNull(prefabNavMeshData);
        Assert.AreSame(initialPrefabNavMeshData, prefabNavMeshData);

        StageManager.instance.GoToMainStage();

        Object.DestroyImmediate(instance);

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButInstanceModified_DoesNotChangeInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        instanceSurface.defaultArea = k_RedArea;
        instanceSurface.BuildNavMesh();
        //CreateNavMeshAsset(instanceSurface);
        var instanceNavMeshData = instanceSurface.navMeshData;

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);
        prefabScene.SavePrefab();
        StageManager.instance.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabSceneReopened = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurfaceReopened = prefabSceneReopened.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.IsNotNull(prefabNavMeshData);
        Assert.AreNotSame(instanceNavMeshData, prefabNavMeshData);
        Assert.AreNotSame(initialPrefabNavMeshData, prefabNavMeshData);
        Assert.AreSame(instanceNavMeshData, instanceSurface.navMeshData);

        StageManager.instance.GoToMainStage();

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        Object.DestroyImmediate(instance);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButNotSaved_RevertsToTheInitialNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        //CreateNavMeshAsset(prefabSurface);
        var rebuiltPrefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsNotNull(rebuiltPrefabNavMeshData);
        Assert.AreNotSame(initialPrefabNavMeshData, rebuiltPrefabNavMeshData);

        StageManager.instance.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabSceneReopened = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurfaceReopened = prefabSceneReopened.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.AreSame(initialPrefabNavMeshData, prefabNavMeshData);
        Assert.AreNotSame(rebuiltPrefabNavMeshData, prefabNavMeshData);

        StageManager.instance.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButNotSaved_TheRebakedAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);
        var assetFolderPath = GetAndEnsureTargetPath(prefabSurface);
        var navMeshAssetName = prefabSurface.navMeshData.name + ".asset";
        var combinedAssetPath = Path.Combine(assetFolderPath, navMeshAssetName);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath), "NavMeshData file must exist. ({0})", combinedAssetPath);

        StageManager.instance.GoToMainStage();

        Assert.IsFalse(System.IO.File.Exists(combinedAssetPath), "NavMeshData file still exists after discarding the changes. ({0})", combinedAssetPath);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebaked_TheOldAssetExistsUntilSaving()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var assetFolderPath = GetAndEnsureTargetPath(prefabSurface);
        var navMeshAssetName = prefabSurface.navMeshData.name + ".asset";
        var combinedAssetPath = Path.Combine(assetFolderPath, navMeshAssetName);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath), "NavMeshData file must exist. ({0})", combinedAssetPath);

        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath), "The initial NavMeshData file must exist after prefab rebake. ({0})", combinedAssetPath);

        prefabScene.SavePrefab();
        Assert.IsFalse(System.IO.File.Exists(combinedAssetPath), "NavMeshData file still exists after saving. ({0})", combinedAssetPath);

        StageManager.instance.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedAndAutoSaved_InstanceHasTheNewNavMeshData()
    {
        var wasAutoSave = StageManager.instance.autoSave;
        StageManager.instance.autoSave = true;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);
        var rebuiltPrefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsNotNull(rebuiltPrefabNavMeshData);
        Assert.AreNotSame(initialPrefabNavMeshData, rebuiltPrefabNavMeshData);
        EditorSceneManager.MarkSceneDirty(prefabScene.scene);

        StageManager.instance.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabSceneReopened = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurfaceReopened = prefabSceneReopened.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.AreNotSame(initialPrefabNavMeshData, prefabNavMeshData);
        Assert.AreSame(rebuiltPrefabNavMeshData, prefabNavMeshData);

        StageManager.instance.GoToMainStage();

        StageManager.instance.autoSave = wasAutoSave;

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterModifiedInstanceAppliedBack_TheOldAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);

        var assetFolderPath = GetAndEnsureTargetPath(instanceSurface);
        var navMeshAssetName = instanceSurface.navMeshData.name + ".asset";
        var combinedAssetPath = Path.Combine(assetFolderPath, navMeshAssetName);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath), "Prefab's NavMeshData file must exist. ({0})", combinedAssetPath);

        instanceSurface.defaultArea = k_RedArea;
        instanceSurface.BuildNavMesh();
        CreateNavMeshAsset(instanceSurface);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath),
            "Prefab's NavMeshData file exists after the instance has changed. ({0})", combinedAssetPath);

        PrefabUtility.ApplyPrefabInstance(instance);

        Assert.IsFalse(System.IO.File.Exists(combinedAssetPath),
            "Prefab's NavMeshData file still exists after the changes from the instance have been applied back to the prefab. ({0})",
            combinedAssetPath);

        Object.DestroyImmediate(instance);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterModifiedInstanceAppliedBack_UpdatedAccordingToInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instanceOne = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceOne);
        instanceOne.name = "PrefabInstanceOne";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceTwo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceTwo);
        instanceTwo.name = "PrefabInstanceTwo";
        // reactivate the object to apply the change of position immediately
        instanceTwo.SetActive(false);
        instanceTwo.transform.position = new Vector3(20, 0, 0);
        instanceTwo.SetActive(true);

        var instanceOneSurface = instanceOne.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceOneSurface);

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        var initialPrefabNavMeshData = instanceOneSurface.navMeshData;
#endif
        instanceOneSurface.defaultArea = k_RedArea;
        instanceOneSurface.BuildNavMesh();
        CreateNavMeshAsset(instanceOneSurface);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, instanceTwo.transform.position);

        PrefabUtility.ApplyPrefabInstance(instanceOne);

        TestNavMeshExistsAloneAtPosition(k_RedArea, instanceTwo.transform.position);

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();
        prefabSurface.defaultArea = k_GrayArea;
        prefabSurface.BuildNavMesh();
        CreateNavMeshAsset(prefabSurface);
        prefabScene.SavePrefab();
        StageManager.instance.GoToMainStage();

        TestNavMeshExistsAloneAtPosition(k_GrayArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_GrayArea, instanceTwo.transform.position);

        Object.DestroyImmediate(instanceOne);
        Object.DestroyImmediate(instanceTwo);

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterClearedInstanceAppliedBack_HasEmptyData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        var initialPrefabNavMeshData = instanceSurface.navMeshData;
#endif
        instanceSurface.RemoveData();
        instanceSurface.navMeshData = null;

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        PrefabUtility.ApplyPrefabInstance(instance);

        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();

        Assert.IsNull(prefabSurface.navMeshData);

        StageManager.instance.GoToMainStage();

        Object.DestroyImmediate(instance);

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRevertsBack_InstanceIsLikePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        instanceSurface.defaultArea = k_RedArea;
        instanceSurface.BuildNavMesh();
        //CreateNavMeshAsset(instanceSurface);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        PrefabUtility.RevertPrefabInstance(instance);

        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        Object.DestroyImmediate(instance);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRevertsBack_TheInstanceAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        instanceSurface.defaultArea = k_RedArea;
        instanceSurface.BuildNavMesh();
        CreateNavMeshAsset(instanceSurface);

        var assetFolderPath = GetAndEnsureTargetPath(instanceSurface);
        var navMeshAssetName = instanceSurface.navMeshData.name + ".asset";
        var combinedAssetPath = Path.Combine(assetFolderPath, navMeshAssetName);

        Assert.IsTrue(System.IO.File.Exists(combinedAssetPath), "Instance's NavMeshData file must exist. ({0})", combinedAssetPath);

        PrefabUtility.RevertPrefabInstance(instance);

        Assert.IsFalse(System.IO.File.Exists(combinedAssetPath), "Instance's NavMeshData file still exists after revert. ({0})", combinedAssetPath);

        Object.DestroyImmediate(instance);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenDeleted_InstancesMakeCopiesOfData()
    {
        Assert.IsTrue(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenBakingInPreviewScene_CollectsOnlyPreviewSceneObjects()
    {
        var mainScenePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        mainScenePlane.transform.localScale = new Vector3(100, 1, 100);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabScene = StageManager.instance.GetCurrentPrefabScene();
        var prefabSurface = prefabScene.prefabInstanceRoot.GetComponent<NavMeshSurface>();

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
#endif
        prefabSurface.collectObjects = CollectObjects.All;
        prefabSurface.defaultArea = k_RedArea;
        prefabSurface.BuildNavMesh();

#if !NAVMESHSURFACE_PREFAB_CLEANUP_ALL_ONCE
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(initialPrefabNavMeshData));
#endif
        CreateNavMeshAsset(prefabSurface);

        prefabScene.SavePrefab();
        StageManager.instance.GoToMainStage();

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var posNearby = new Vector3(20,0,0);
        Assert.IsFalse(HasNavMeshAtPosition(posNearby, 1 << k_RedArea),
            "NavMesh with the prefab's area exists at position {1}, outside the prefab's plane. ({0})",
            k_RedArea, posNearby);

        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(mainScenePlane);

        yield return null;
    }

    public static bool HasNavMeshAtPosition(Vector3 pos, int areaMask = NavMesh.AllAreas, int agentTypeId = 0, float range = 0.1f)
    {
        NavMeshHit hit;
        var filter = new NavMeshQueryFilter
        {
            areaMask = areaMask,
            agentTypeID = agentTypeId
        };
        return NavMesh.SamplePosition(pos, out hit, range, filter);
    }

    // copied from NavMeshSurfaceEditor and adapted to better fit the purpose of the tests
    static void CreateNavMeshAsset(NavMeshSurface surface)
    {
        var targetPath = GetAndEnsureTargetPath(surface);
        var date = "" + DateTime.Now.Millisecond.ToString("000") + DateTime.Now.Second.ToString("00") +
            DateTime.Now.Minute.ToString("00") + DateTime.Now.Hour.ToString("00") + DateTime.Now.Day.ToString("00");
        //var agentName = NavMesh.GetSettingsNameFromID(surface.agentTypeID);
        var navMeshAssetName = "NavMesh-" + surface.name + "-" /*+ agentName + "-" */ +
            "A" + surface.defaultArea.ToString("00") + "_" + date + ".asset";
        var combinedAssetPath = Path.Combine(targetPath, navMeshAssetName);
        combinedAssetPath = AssetDatabase.GenerateUniqueAssetPath(combinedAssetPath);

        Debug.LogFormat("NavMeshData renamed from {0} to {1}", surface.navMeshData.name, navMeshAssetName);

        AssetDatabase.CreateAsset(surface.navMeshData, combinedAssetPath);
    }

    // copied from NavMeshSurfaceEditor
    static string GetAndEnsureTargetPath(NavMeshSurface surface)
    {
        // Create directory for the asset if it does not exist yet.
        var activeScenePath = surface.gameObject.scene.path;

        var targetPath = k_TempFolder;
        if (!string.IsNullOrEmpty(activeScenePath))
            targetPath = Path.Combine(Path.GetDirectoryName(activeScenePath), Path.GetFileNameWithoutExtension(activeScenePath));
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);
        return targetPath;
    }

    void DeleteNavMeshAsset(NavMeshSurface s)
    {
        if (s.navMeshData)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(s.navMeshData));

            // this will require the scene to be saved when closing
            //EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
        }
    }
}
