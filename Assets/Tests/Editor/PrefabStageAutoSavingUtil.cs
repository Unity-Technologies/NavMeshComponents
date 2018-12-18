using System;
using System.Reflection;
using UnityEditor;

public static class PrefabStageAutoSavingUtil
{
    public static bool GetPrefabStageAutoSave()
    {
        var stageNavMgrInstance = GetStageNavigationManagerInstance();
        var autoSaveProperty = GetAutoSaveProperty(stageNavMgrInstance);
        return (bool)autoSaveProperty.GetValue(stageNavMgrInstance, null);
    }

    public static void SetPrefabStageAutoSave(bool value)
    {
        var stageNavMgrInstance = GetStageNavigationManagerInstance();
        var autoSaveProperty = GetAutoSaveProperty(stageNavMgrInstance);
        autoSaveProperty.SetValue(stageNavMgrInstance, value, null);
    }

    static object GetStageNavigationManagerInstance()
    {
        var editorAssemblyName = typeof(EditorWindow).Assembly.FullName;
        var t = Type.GetType("UnityEditor.SceneManagement.StageNavigationManager, " + editorAssemblyName, true, true);
        if (t == null)
            throw new ArgumentException();

        var instanceProperty = t.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (instanceProperty == null)
            throw new ArgumentException();

        var stageNavMgrInstance = instanceProperty.GetValue(null, null);
        return stageNavMgrInstance;
    }

    static PropertyInfo GetAutoSaveProperty(object stageNavigationManagerInstance)
    {
        var autoSaveProperty = stageNavigationManagerInstance.GetType().GetProperty("autoSave", BindingFlags.Instance | BindingFlags.NonPublic);
        if (autoSaveProperty == null)
            throw new ArgumentException();

        return autoSaveProperty;
    }
}
