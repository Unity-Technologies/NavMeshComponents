//#define KEEP_ARTIFACTS_FOR_INSPECTION
//#define ENABLE_TEST_LOGS

using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[Category("PrefabsWithNavMeshComponents")]
public class NavMeshSurfaceInPrefabTests
{
    const string k_AutoSaveKey = "AutoSave";
    const string k_ParentFolder = "Assets/Tests/Editor";
    const string k_TempFolderName = "TempPrefab";
    string m_TempFolder = k_ParentFolder + "/" + k_TempFolderName;
    string m_PrefabPath;
    string m_PreviousScenePath;
    string m_TempScenePath;
    int m_TestCounter;

    const int k_GrayArea = 7;
    const int k_BrownArea = 10;
    const int k_RedArea = 18;
    const int k_OrangeArea = 26;
    const int k_YellowArea = 30;

    const int k_PrefabDefaultArea = k_YellowArea;

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
        m_TempScenePath = Path.Combine(m_TempFolder, "NavMeshSurfacePrefabTestsScene.unity");
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
        plane.name = "NavMeshSurface" + (++m_TestCounter) + "Prefab";
        var surface = plane.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;

        m_PrefabPath = Path.Combine(m_TempFolder, plane.name + ".prefab");
        var planePrefab = PrefabUtility.SaveAsPrefabAsset(plane, m_PrefabPath);
        Object.DestroyImmediate(plane);

        AssetDatabase.OpenAsset(planePrefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => prefabSurface, k_PrefabDefaultArea);
        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        NavMesh.RemoveAllNavMeshData();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            prefabStage.ClearDirtiness();

        StageUtility.GoToMainStage();

        yield return null;
    }

    static void TestNavMeshExistsAloneAtPosition(int expectedArea, Vector3 pos)
    {
        var expectedAreaMask = 1 << expectedArea;

#if ENABLE_TEST_LOGS
        var areaExists = HasNavMeshAtPosition(pos, expectedAreaMask);
        var otherAreasExist = HasNavMeshAtPosition(pos, ~expectedAreaMask);
        Debug.Log(" mask=" + expectedAreaMask.ToString("x8") + " area " + expectedArea + 
            " Exists=" + areaExists + " otherAreasExist=" + otherAreasExist + " at position " + pos);
        if (otherAreasExist)
        {
            for (var i = 0; i < 32; i++)
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
#endif
        Assert.IsTrue(HasNavMeshAtPosition(pos, expectedAreaMask), "Expected NavMesh with area {0} at position {1}.", expectedArea, pos);
        Assert.IsFalse(HasNavMeshAtPosition(pos, ~expectedAreaMask), "A NavMesh with an area other than {0} exists at position {1}.", expectedArea, pos);
    }

    [Test]
    public void NavMeshSurfacePrefab_WhenOpenedInPrefabMode_DoesNotActivateItsNavMesh()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);

        NavMeshHit hit;
        NavMesh.SamplePosition(Vector3.zero, out hit, 1000000f, new NavMeshQueryFilter { areaMask = NavMesh.AllAreas, agentTypeID = 0 });
        Assert.That(hit.hit, Is.False, "The NavMesh instance of a prefab opened for edit should not be active under any circumstances.");
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterBakingInPrefabMode_DoesNotActivateItsNavMesh()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        NavMeshAssetManager.instance.ClearSurfaces(new Object[] { prefabSurface });
        PrefabSavingUtil.SavePrefab(prefabStage);

        yield return BakeNavMeshAsync(() => prefabSurface, k_RedArea);

        NavMeshHit hit;
        NavMesh.SamplePosition(Vector3.zero, out hit, 1000000f, new NavMeshQueryFilter { areaMask = NavMesh.AllAreas, agentTypeID = 0 });
        Assert.That(hit.hit, Is.False, "The NavMesh instance of a prefab opened for edit should not be active after baking the surface.");

        prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        PrefabSavingUtil.SavePrefab(prefabStage);

        NavMesh.SamplePosition(Vector3.zero, out hit, 1000000f, new NavMeshQueryFilter { areaMask = NavMesh.AllAreas, agentTypeID = 0 });
        Assert.That(hit.hit, Is.False, "The NavMesh instance of a prefab opened for edit should not be active after baking the surface.");
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterBakingInPrefabMode_LeavesMainSceneUntouched()
    {
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        Assert.IsNotNull(prefabStage);
        Assert.IsNotNull(prefabStage.prefabContentsRoot);

        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        yield return BakeNavMeshAsync(() => prefabSurface, k_RedArea);

        Assert.AreNotSame(initialPrefabNavMeshData, prefabSurface.navMeshData);

        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, NavMesh.AllAreas, 0, 1000.0f));

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstantiated_ReferencesTheSameNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        var instanceNavMeshData = instanceSurface.navMeshData;

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "Surface" + m_TestCounter + "PrefabInstanceClone";

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        Assert.IsNotNull(prefabStage);
        Assert.IsNotNull(prefabStage.prefabContentsRoot);

        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreSame(prefabNavMeshData, instanceNavMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageUtility.GoToMainStage();

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenEmptyAndInstantiated_InstanceHasEmptyNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsTrue(instanceSurface.navMeshData != null, "NavMeshSurface in prefab instance must have NavMeshData.");

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        NavMeshAssetManager.instance.ClearSurfaces(new Object[] { prefabSurface });
        PrefabSavingUtil.SavePrefab(prefabStage);

        StageUtility.GoToMainStage();
        Assert.IsTrue(instanceSurface.navMeshData == null,
            "After the NavMeshSurface in the prefab has been cleared the prefab instance should no longer hold NavMeshData.");
        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenBakesNewNavMesh_UpdatesTheInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instanceOne = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceOne);
        instanceOne.name = "Surface" + m_TestCounter + "PrefabInstanceOne";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => prefabSurface, k_RedArea);

        PrefabSavingUtil.SavePrefab(prefabStage);

        StageUtility.GoToMainStage();

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var instanceOneSurface = instanceOne.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => instanceOneSurface, k_BrownArea);

        var instanceTwo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceTwo);
        instanceTwo.name = "Surface" + m_TestCounter + "PrefabInstanceTwo";
        // reactivate the object to apply the change of position immediately
        instanceTwo.SetActive(false);
        instanceTwo.transform.position = new Vector3(20, 0, 0);
        instanceTwo.SetActive(true);

        TestNavMeshExistsAloneAtPosition(k_BrownArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_RedArea, instanceTwo.transform.position);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instanceOne);
        Object.DestroyImmediate(instanceTwo);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRebaked_HasDifferentNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "Surface" + m_TestCounter + "PrefabInstanceClone";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        yield return BakeNavMeshAsync(() => instanceSurface, k_RedArea);
        var instanceNavMeshData = instanceSurface.navMeshData;

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreNotSame(instanceNavMeshData, prefabNavMeshData);
        Assert.AreNotSame(instanceNavMeshData, instanceCloneNavMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageUtility.GoToMainStage();

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceCleared_InstanceHasEmptyNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";

        var clonePosition = new Vector3(20, 0, 0);
        var instanceClone = Object.Instantiate(instance, clonePosition, Quaternion.identity);
        Assert.IsNotNull(instanceClone);
        instanceClone.name = "Surface" + m_TestCounter + "PrefabInstanceClone";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        NavMeshAssetManager.instance.ClearSurfaces(new Object[] { instanceSurface });

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        Assert.IsTrue(HasNavMeshAtPosition(clonePosition, expectedAreaMask));
        Assert.IsFalse(HasNavMeshAtPosition(clonePosition, ~expectedAreaMask));

        var instanceCloneSurface = instanceClone.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceCloneSurface);
        var instanceCloneNavMeshData = instanceCloneSurface.navMeshData;

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.AreNotSame(prefabNavMeshData, instanceSurface.navMeshData);
        Assert.AreNotSame(instanceCloneNavMeshData, instanceSurface.navMeshData);
        Assert.AreSame(prefabNavMeshData, instanceCloneNavMeshData);

        StageUtility.GoToMainStage();

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(instanceClone);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceCleared_PrefabKeepsNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        var initialPrefabNavMeshData = instanceSurface.navMeshData;
        NavMeshAssetManager.instance.ClearSurfaces(new Object[] { instanceSurface });

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsTrue(prefabNavMeshData != null,
            "NavMeshSurface in the prefab must still have NavMeshData even though the instance was cleared.");
        Assert.AreSame(initialPrefabNavMeshData, prefabNavMeshData);

        StageUtility.GoToMainStage();

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButInstanceModified_DoesNotChangeInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        yield return BakeNavMeshAsync(() => instanceSurface, k_RedArea);
        var instanceNavMeshData = instanceSurface.navMeshData;

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);
        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabStageReopened = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurfaceReopened = prefabStageReopened.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.IsTrue(prefabNavMeshData != null,
            "NavMeshSurface in prefab must have NavMeshData after baking, saving, closing and reopening.");
        Assert.AreNotSame(instanceNavMeshData, prefabNavMeshData);
        Assert.AreNotSame(initialPrefabNavMeshData, prefabNavMeshData);

        StageUtility.GoToMainStage();
        Assert.AreSame(instanceNavMeshData, instanceSurface.navMeshData);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButNotSaved_RevertsToTheInitialNavMeshData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        var initialPrefabNavMeshAssetPath = AssetDatabase.GetAssetPath(initialPrefabNavMeshData);
        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);
        var rebuiltPrefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsTrue(rebuiltPrefabNavMeshData != null, "NavMeshSurface must have NavMeshData after baking.");
        Assert.AreNotSame(initialPrefabNavMeshData, rebuiltPrefabNavMeshData);

        prefabStage.ClearDirtiness();
        StageUtility.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabStageReopened = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurfaceReopened = prefabStageReopened.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.AreSame(initialPrefabNavMeshData, prefabNavMeshData);
        Assert.AreNotSame(rebuiltPrefabNavMeshData, prefabNavMeshData);
        var prefabNavMeshAssetPath = AssetDatabase.GetAssetPath(prefabNavMeshData);
        StringAssert.AreEqualIgnoringCase(initialPrefabNavMeshAssetPath, prefabNavMeshAssetPath,
            "The NavMeshData asset referenced by the prefab should remain the same when exiting prefab mode without saving.");

        StageUtility.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedButNotSaved_TheRebakedAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);
        var rebakedAssetPath = AssetDatabase.GetAssetPath(prefabSurface.navMeshData);

        Assert.IsTrue(File.Exists(rebakedAssetPath), "NavMeshData file must exist. ({0})", rebakedAssetPath);

        prefabStage.ClearDirtiness();
        StageUtility.GoToMainStage();

        Assert.IsFalse(File.Exists(rebakedAssetPath), "NavMeshData file still exists after discarding the changes. ({0})", rebakedAssetPath);

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebaked_TheOldAssetExistsUntilSavingAndNotAfter()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialNavMeshData = prefabSurface.navMeshData;
        var initialAssetPath = AssetDatabase.GetAssetPath(prefabSurface.navMeshData);

        // Assert.IsNull cannot verify correctly that an UnityEngine.Object is null
        Assert.IsTrue(initialNavMeshData != null, "Prefab must have some NavMeshData.");
        Assert.IsTrue(File.Exists(initialAssetPath), "NavMeshData file must exist. ({0})", initialAssetPath);

        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);

        Assert.IsTrue(initialNavMeshData != null, "The initial NavMeshData must still exist immediately after prefab re-bake.");
        Assert.IsTrue(File.Exists(initialAssetPath), "The initial NavMeshData file must exist after prefab re-bake. ({0})", initialAssetPath);

        Assert.IsTrue(prefabSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");
        var unsavedRebakedNavMeshData = prefabSurface.navMeshData;

        yield return BakeNavMeshAsync(() => prefabSurface, k_OrangeArea);

        Assert.IsTrue(unsavedRebakedNavMeshData == null, "An unsaved NavMeshData should not exist after a re-bake.");
        Assert.IsTrue(prefabSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");

        PrefabSavingUtil.SavePrefab(prefabStage);
        Assert.IsFalse(File.Exists(initialAssetPath), "NavMeshData file still exists after saving. ({0})", initialAssetPath);
        Assert.IsTrue(initialNavMeshData == null, "The initial NavMeshData must no longer exist after saving the prefab.");

        // ReSharper disable once HeuristicUnreachableCode - initialNavMeshData is affected by BakeNavMeshAsync()
        StageUtility.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenRebakedAndAutoSaved_InstanceHasTheNewNavMeshData()
    {
        var wasAutoSave = PrefabStageAutoSavingUtil.GetPrefabStageAutoSave();
        PrefabStageAutoSavingUtil.SetPrefabStageAutoSave(true);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialPrefabNavMeshData = prefabSurface.navMeshData;
        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);
        var rebuiltPrefabNavMeshData = prefabSurface.navMeshData;
        Assert.IsTrue(rebuiltPrefabNavMeshData != null, "NavMeshSurface must have NavMeshData after baking.");
        Assert.AreNotSame(initialPrefabNavMeshData, rebuiltPrefabNavMeshData);

        StageUtility.GoToMainStage();

        AssetDatabase.OpenAsset(prefab);
        var prefabStageReopened = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurfaceReopened = prefabStageReopened.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var prefabNavMeshData = prefabSurfaceReopened.navMeshData;
        Assert.AreNotSame(initialPrefabNavMeshData, prefabNavMeshData);
        Assert.AreSame(rebuiltPrefabNavMeshData, prefabNavMeshData);

        StageUtility.GoToMainStage();

        PrefabStageAutoSavingUtil.SetPrefabStageAutoSave(wasAutoSave);

        yield return null;
    }

    [Ignore("Currently the deletion of the old asset must be done manually.")]
    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterModifiedInstanceAppliedBack_TheOldAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);

        var initialInstanceAssetPath = AssetDatabase.GetAssetPath(instanceSurface.navMeshData);

        Assert.IsTrue(File.Exists(initialInstanceAssetPath), "Prefab's NavMeshData file must exist. ({0})", initialInstanceAssetPath);

        yield return BakeNavMeshAsync(() => instanceSurface, k_RedArea);

        Assert.IsTrue(File.Exists(initialInstanceAssetPath),
            "Prefab's NavMeshData file exists after the instance has changed. ({0})", initialInstanceAssetPath);

        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);

        Assert.IsFalse(File.Exists(initialInstanceAssetPath),
            "Prefab's NavMeshData file still exists after the changes from the instance have been applied back to the prefab. ({0})",
            initialInstanceAssetPath);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterModifiedInstanceAppliedBack_UpdatedAccordingToInstance()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instanceOne = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceOne);
        instanceOne.name = "Surface" + m_TestCounter + "PrefabInstanceOne";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceTwo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instanceTwo);
        instanceTwo.name = "Surface" + m_TestCounter + "PrefabInstanceTwo";
        // reactivate the object to apply the change of position immediately
        instanceTwo.SetActive(false);
        instanceTwo.transform.position = new Vector3(20, 0, 0);
        instanceTwo.SetActive(true);

        var instanceOneSurface = instanceOne.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceOneSurface);

        yield return BakeNavMeshAsync(() => instanceOneSurface, k_RedArea);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, instanceTwo.transform.position);

        PrefabUtility.ApplyPrefabInstance(instanceOne, InteractionMode.AutomatedAction);

        TestNavMeshExistsAloneAtPosition(k_RedArea, instanceTwo.transform.position);

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => prefabSurface, k_GrayArea);
        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        TestNavMeshExistsAloneAtPosition(k_GrayArea, Vector3.zero);
        TestNavMeshExistsAloneAtPosition(k_GrayArea, instanceTwo.transform.position);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instanceOne);
        Object.DestroyImmediate(instanceTwo);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_AfterClearedInstanceAppliedBack_HasEmptyData()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);

        NavMeshAssetManager.instance.ClearSurfaces(new Object[] { instanceSurface });

        var expectedAreaMask = 1 << k_PrefabDefaultArea;
        Assert.IsFalse(HasNavMeshAtPosition(Vector3.zero, expectedAreaMask));

        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);

        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();

        Assert.IsTrue(prefabSurface.navMeshData == null,
            "Prefab should have empty NavMeshData when empty data has been applied back from the instance.");

        StageUtility.GoToMainStage();

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRevertsBack_InstanceIsLikePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        yield return BakeNavMeshAsync(() => instanceSurface, k_RedArea);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);

        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [Ignore("Deletion of the old asset is expected to be done manually for the time being.")]
    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenInstanceRevertsBack_TheInstanceAssetNoLongerExists()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);
        instance.name = "Surface" + m_TestCounter + "PrefabInstance";
        TestNavMeshExistsAloneAtPosition(k_PrefabDefaultArea, Vector3.zero);

        var instanceSurface = instance.GetComponent<NavMeshSurface>();
        Assert.IsNotNull(instanceSurface);
        yield return BakeNavMeshAsync(() => instanceSurface, k_RedArea);

        var instanceAssetPath = AssetDatabase.GetAssetPath(instanceSurface.navMeshData);

        Assert.IsTrue(File.Exists(instanceAssetPath), "Instance's NavMeshData file must exist. ({0})", instanceAssetPath);

        PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);

        Assert.IsFalse(File.Exists(instanceAssetPath), "Instance's NavMeshData file still exists after revert. ({0})", instanceAssetPath);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
#endif
        yield return null;
    }

    [Ignore("The expected behaviour has not been decided.")]
    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenDeleted_InstancesMakeCopiesOfData()
    {
        yield return null;
        Assert.Fail("not implemented yet");
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefab_WhenBakingInPreviewScene_CollectsOnlyPreviewSceneObjects()
    {
        var mainScenePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        mainScenePlane.transform.localScale = new Vector3(100, 1, 100);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(prefab);
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabSurface = prefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();

        prefabSurface.collectObjects = CollectObjects.All;
        yield return BakeNavMeshAsync(() => prefabSurface, k_RedArea);

        PrefabSavingUtil.SavePrefab(prefabStage);
        StageUtility.GoToMainStage();

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance);

        TestNavMeshExistsAloneAtPosition(k_RedArea, Vector3.zero);

        var posNearby = new Vector3(20,0,0);
        Assert.IsFalse(HasNavMeshAtPosition(posNearby, 1 << k_RedArea),
            "NavMesh with the prefab's area exists at position {1}, outside the prefab's plane. ({0})",
            k_RedArea, posNearby);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(mainScenePlane);
#endif
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

    static IEnumerator BakeNavMeshAsync(Func<NavMeshSurface> getSurface, int defaultArea)
    {
        var surface = getSurface();
        surface.defaultArea = defaultArea;
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        yield return new WaitWhile(() => NavMeshAssetManager.instance.IsSurfaceBaking(surface));
    }
}
