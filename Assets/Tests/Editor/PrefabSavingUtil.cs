using System;
using System.Reflection;
using UnityEditor.Experimental.SceneManagement;

public class PrefabSavingUtil {

    public static void SavePrefab(PrefabStage prefabStage)
    {
        if (prefabStage == null)
            throw new ArgumentNullException();

        var savePrefabMethod = prefabStage.GetType().GetMethod("SavePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        if (savePrefabMethod == null)
            throw new InvalidOperationException();

        savePrefabMethod.Invoke(prefabStage, null);
    }
}
