//#define KEEP_ARTIFACTS_FOR_INSPECTION

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
public class NavMeshSurfaceInPrefabVariantTests
{
    const string k_AutoSaveKey = "AutoSave";
    const string k_ParentFolder = "Assets/Tests/Editor";
    const string k_TempFolderName = "TempPrefabVariants";
    string m_TempFolder = k_ParentFolder + "/" + k_TempFolderName;
    string m_PrefabPath;
    string m_PrefabVariantPath;
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
        m_TempScenePath = Path.Combine(m_TempFolder, "NavMeshSurfacePrefabVariantTestsScene.unity");
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
        plane.name = "NavMeshSurfacePrefab" + (++m_TestCounter);
        var surface = plane.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;

        m_PrefabPath = Path.Combine(m_TempFolder, plane.name + ".prefab");
        m_PrefabVariantPath = Path.Combine(m_TempFolder, plane.name + "Variant.prefab");

        var planePrefab = PrefabUtility.SaveAsPrefabAsset(plane, m_PrefabPath);
        Object.DestroyImmediate(plane);

        AssetDatabase.OpenAsset(planePrefab);
        var theOriginalPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var theOriginalPrefabSurface = theOriginalPrefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        yield return BakeNavMeshAsync(() => theOriginalPrefabSurface, k_PrefabDefaultArea);
        PrefabSavingUtil.SavePrefab(theOriginalPrefabStage);
        StageUtility.GoToMainStage();

        var instanceForVariant = PrefabUtility.InstantiatePrefab(planePrefab) as GameObject;
        PrefabUtility.SaveAsPrefabAsset(instanceForVariant, m_PrefabVariantPath);

#if !KEEP_ARTIFACTS_FOR_INSPECTION
        Object.DestroyImmediate(instanceForVariant);
#endif
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
    public IEnumerator NavMeshSurfacePrefabVariant_WhenFreshAndRebaked_ParentAssetUnchanged()
    {
        var theOriginalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
        AssetDatabase.OpenAsset(theOriginalPrefab);
        var theOriginalPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var theOriginalPrefabSurface = theOriginalPrefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var theOriginalPrefabNavMeshData = theOriginalPrefabSurface.navMeshData;
        var theOriginalPrefabAssetPath = AssetDatabase.GetAssetPath(theOriginalPrefabSurface.navMeshData);

        Assert.IsTrue(theOriginalPrefabNavMeshData != null, "Original prefab must have some NavMeshData.");
        Assert.IsTrue(File.Exists(theOriginalPrefabAssetPath), "NavMeshData file must exist for the original prefab. ({0})", theOriginalPrefabAssetPath);

        var prefabVariant = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabVariantPath);
        AssetDatabase.OpenAsset(prefabVariant);
        var prefabVariantStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabVariantSurface = prefabVariantStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var initialVariantNavMeshData = prefabVariantSurface.navMeshData;
        var initialVariantAssetPath = AssetDatabase.GetAssetPath(prefabVariantSurface.navMeshData);

        Assert.AreEqual(theOriginalPrefabNavMeshData, initialVariantNavMeshData, "Fresh variant must have the same NavMeshData as the original prefab.");

        Assert.IsTrue(initialVariantNavMeshData != null, "Prefab must have some NavMeshData.");
        Assert.IsTrue(File.Exists(initialVariantAssetPath), "NavMeshData file must exist. ({0})", initialVariantAssetPath);

        yield return BakeNavMeshAsync(() => prefabVariantSurface, k_GrayArea);

        Assert.IsTrue(initialVariantNavMeshData != null, "The initial NavMeshData (from original prefab) must still exist immediately after prefab variant re-bake.");
        Assert.IsTrue(File.Exists(initialVariantAssetPath), "The initial NavMeshData file (from original prefab) must exist after prefab variant re-bake. ({0})", initialVariantAssetPath);

        Assert.IsTrue(prefabVariantSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");
        var unsavedRebakedNavMeshData = prefabVariantSurface.navMeshData;

        yield return BakeNavMeshAsync(() => prefabVariantSurface, k_BrownArea);

        Assert.IsTrue(unsavedRebakedNavMeshData == null, "An unsaved NavMeshData should not exist after a re-bake.");
        Assert.IsTrue(prefabVariantSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");

        PrefabSavingUtil.SavePrefab(prefabVariantStage);

        var theNewVariantNavMeshData = prefabVariantSurface.navMeshData;
        var theNewVariantAssetPath = AssetDatabase.GetAssetPath(theNewVariantNavMeshData);

        Assert.IsTrue(File.Exists(theNewVariantAssetPath), "Variant's own NavMeshData exists in a file after saving. ({0})", theNewVariantAssetPath);
        Assert.IsTrue(File.Exists(theOriginalPrefabAssetPath), "NavMeshData file of the original prefab still exists after saving the variant. ({0})", theOriginalPrefabAssetPath);
        Assert.IsTrue(theOriginalPrefabNavMeshData != null, "Original prefab must still have NavMeshData.");

        StageUtility.GoToMainStage();

        yield return null;
    }

    [UnityTest]
    public IEnumerator NavMeshSurfacePrefabVariant_WhenCustomizedAndRebaked_OldAssetDiscardedAndParentAssetUnchanged()
    {
        var prefabVariant = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabVariantPath);
        var theOriginalPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabVariant);
        var theOriginalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(theOriginalPrefabPath);

        AssetDatabase.OpenAsset(theOriginalPrefab);
        var theOriginalPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var theOriginalPrefabSurface = theOriginalPrefabStage.prefabContentsRoot.GetComponent<NavMeshSurface>();
        var theOriginalPrefabNavMeshData = theOriginalPrefabSurface.navMeshData;
        var theOriginalPrefabAssetPath = AssetDatabase.GetAssetPath(theOriginalPrefabSurface.navMeshData);

        Assert.IsTrue(theOriginalPrefabNavMeshData != null, "Original prefab must have some NavMeshData.");
        Assert.IsTrue(File.Exists(theOriginalPrefabAssetPath), "NavMeshData file must exist for the original prefab. ({0})", theOriginalPrefabAssetPath);

        AssetDatabase.OpenAsset(prefabVariant);
        var prefabVariantStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabVariantSurface = prefabVariantStage.prefabContentsRoot.GetComponent<NavMeshSurface>();

        yield return BakeNavMeshAsync(() => prefabVariantSurface, k_GrayArea);
        PrefabSavingUtil.SavePrefab(prefabVariantStage);

        var modifiedVariantNavMeshData = prefabVariantSurface.navMeshData;
        var modifiedVariantAssetPath = AssetDatabase.GetAssetPath(prefabVariantSurface.navMeshData);

        Assert.IsTrue(modifiedVariantNavMeshData != null, "Prefab must have some NavMeshData.");
        Assert.IsTrue(File.Exists(modifiedVariantAssetPath), "NavMeshData file for modifier variant must exist. ({0})", modifiedVariantAssetPath);
        Assert.AreNotEqual(theOriginalPrefabNavMeshData, modifiedVariantNavMeshData, "Modified variant must have a NavMeshData different than that of the original prefab.");

        yield return BakeNavMeshAsync(() => prefabVariantSurface, k_OrangeArea);

        Assert.IsTrue(modifiedVariantNavMeshData != null, "The initial NavMeshData of a modified variant must still exist immediately after prefab variant re-bake.");
        Assert.IsTrue(File.Exists(modifiedVariantAssetPath), "The initial NavMeshData file of a modified variant must exist after prefab variant re-bake. ({0})", modifiedVariantAssetPath);

        Assert.IsTrue(prefabVariantSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");
        var unsavedRebakedNavMeshData = prefabVariantSurface.navMeshData;

        yield return BakeNavMeshAsync(() => prefabVariantSurface, k_RedArea);
        Assert.IsTrue(unsavedRebakedNavMeshData == null, "An unsaved NavMeshData should not exist after a re-bake.");
        Assert.IsTrue(prefabVariantSurface.navMeshData != null, "NavMeshSurface must have NavMeshData after baking.");

        PrefabSavingUtil.SavePrefab(prefabVariantStage);
        var theNewVariantNavMeshData = prefabVariantSurface.navMeshData;
        var theNewVariantAssetPath = AssetDatabase.GetAssetPath(theNewVariantNavMeshData);

        Assert.IsTrue(modifiedVariantNavMeshData == null, "Initial NavMeshData of the modified variant must no longer exist after saving the variant.");
        // ReSharper disable once HeuristicUnreachableCode - modifiedVariantNavMeshData is affected by BakeNavMeshAsync()
        Assert.IsFalse(File.Exists(modifiedVariantAssetPath), "Initial NavMeshData file of the modified and saved variant must no longer exist after saving the variant. ({0})", modifiedVariantAssetPath);
        Assert.IsTrue(File.Exists(theNewVariantAssetPath), "Variant's own NavMeshData exists in a file after saving. ({0})", theNewVariantAssetPath);
        Assert.IsTrue(File.Exists(theOriginalPrefabAssetPath), "NavMeshData file of the original prefab still exists after saving the variant. ({0})", theOriginalPrefabAssetPath);
        Assert.AreNotEqual(theOriginalPrefabNavMeshData, theNewVariantNavMeshData, "Re-baked modified variant must have a NavMeshData different than that of the original prefab.");

        StageUtility.GoToMainStage();

        yield return null;
    }

    static IEnumerator BakeNavMeshAsync(Func<NavMeshSurface> getSurface, int defaultArea)
    {
        var surface = getSurface();
        surface.defaultArea = defaultArea;
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        yield return new WaitWhile(() => NavMeshAssetManager.instance.IsSurfaceBaking(surface));
    }
}
