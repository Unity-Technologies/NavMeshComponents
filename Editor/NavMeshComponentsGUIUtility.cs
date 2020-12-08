using UnityEngine;
using UnityEngine.AI;

namespace UnityEditor.AI
{
    /// <summary> Class containing a set of utility functions meant for presenting information from the NavMeshComponents into the GUI. </summary>
    public static class NavMeshComponentsGUIUtility
    {
        /// <summary> Displays a GUI element for selecting the area type used by a <see cref="NavMeshSurface"/>, <see cref="NavMeshLink"/>, <see cref="NavMeshModifier"/> or <see cref="NavMeshModifierVolume"/>. </summary>
        /// <param name="labelName"></param>
        /// <param name="areaProperty"></param>
        public static void AreaPopup(string labelName, SerializedProperty areaProperty)
        {
            var areaIndex = -1;
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            for (var i = 0; i < areaNames.Length; i++)
            {
                var areaValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
                if (areaValue == areaProperty.intValue)
                    areaIndex = i;
            }
            ArrayUtility.Add(ref areaNames, "");
            ArrayUtility.Add(ref areaNames, "Open Area Settings...");

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, GUIContent.none, areaProperty);

            EditorGUI.BeginChangeCheck();
            areaIndex = EditorGUI.Popup(rect, labelName, areaIndex, areaNames);

            if (EditorGUI.EndChangeCheck())
            {
                if (areaIndex >= 0 && areaIndex < areaNames.Length - 2)
                    areaProperty.intValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[areaIndex]);
                else if (areaIndex == areaNames.Length - 1)
                    NavMeshEditorHelpers.OpenAreaSettings();
            }

            EditorGUI.EndProperty();
        }

        /// <summary> Displays a GUI element for selecting the agent type used by a <see cref="NavMeshSurface"/> or <see cref="NavMeshLink"/>. </summary>
        /// <param name="labelName"></param>
        /// <param name="agentTypeID"></param>
        public static void AgentTypePopup(string labelName, SerializedProperty agentTypeID)
        {
            var index = -1;
            var count = NavMesh.GetSettingsCount();
            var agentTypeNames = new string[count + 2];
            for (var i = 0; i < count; i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var name = NavMesh.GetSettingsNameFromID(id);
                agentTypeNames[i] = name;
                if (id == agentTypeID.intValue)
                    index = i;
            }
            agentTypeNames[count] = "";
            agentTypeNames[count + 1] = "Open Agent Settings...";

            bool validAgentType = index != -1;
            if (!validAgentType)
            {
                EditorGUILayout.HelpBox("Agent Type invalid.", MessageType.Warning);
            }

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, GUIContent.none, agentTypeID);

            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(rect, labelName, index, agentTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (index >= 0 && index < count)
                {
                    var id = NavMesh.GetSettingsByIndex(index).agentTypeID;
                    agentTypeID.intValue = id;
                }
                else if (index == count + 1)
                {
                    NavMeshEditorHelpers.OpenAgentSettings(-1);
                }
            }

            EditorGUI.EndProperty();
        }

        // Agent mask is a set (internally array/list) of agentTypeIDs.
        // It is used to describe which agents modifiers apply to.
        // There is a special case of "None" which is an empty array.
        // There is a special case of "All" which is an array of length 1, and value of -1.
        /// <summary> Displays a GUI element for selecting multiple agent types for which a <see cref="NavMeshModifier"/> or <see cref="NavMeshModifierVolume"/> can influence the NavMesh. </summary>
        /// <param name="labelName"></param>
        /// <param name="agentMask"></param>
        public static void AgentMaskPopup(string labelName, SerializedProperty agentMask)
        {
            // Contents of the dropdown box.
            string popupContent = "";

            if (agentMask.hasMultipleDifferentValues)
                popupContent = "\u2014";
            else
                popupContent = GetAgentMaskLabelName(agentMask);

            var content = new GUIContent(popupContent);
            var popupRect = GUILayoutUtility.GetRect(content, EditorStyles.popup);

            EditorGUI.BeginProperty(popupRect, GUIContent.none, agentMask);
            popupRect = EditorGUI.PrefixLabel(popupRect, 0, new GUIContent(labelName));
            bool pressed = GUI.Button(popupRect, content, EditorStyles.popup);

            if (pressed)
            {
                var show = !agentMask.hasMultipleDifferentValues;
                var showNone = show && agentMask.arraySize == 0;
                var showAll = show && IsAll(agentMask);

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None"), showNone, SetAgentMaskNone, agentMask);
                menu.AddItem(new GUIContent("All"), showAll, SetAgentMaskAll, agentMask);
                menu.AddSeparator("");

                var count = NavMesh.GetSettingsCount();
                for (var i = 0; i < count; i++)
                {
                    var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                    var sname = NavMesh.GetSettingsNameFromID(id);

                    var showSelected = show && AgentMaskHasSelectedAgentTypeID(agentMask, id);
                    var userData = new object[] { agentMask, id, !showSelected };
                    menu.AddItem(new GUIContent(sname), showSelected, ToggleAgentMaskItem, userData);
                }

                menu.DropDown(popupRect);
            }

            EditorGUI.EndProperty();
        }

        /// <summary> Creates a new GameObject as a child of another one and selects it immediately. </summary>
        /// <param name="suggestedName"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static GameObject CreateAndSelectGameObject(string suggestedName, GameObject parent)
        {
            var parentTransform = parent != null ? parent.transform : null;
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(parentTransform, suggestedName);
            var child = new GameObject(uniqueName);

            Undo.RegisterCreatedObjectUndo(child, "Create " + uniqueName);
            if (parentTransform != null)
                Undo.SetTransformParent(child.transform, parentTransform, "Parent " + uniqueName);

            Selection.activeGameObject = child;

            return child;
        }

        /// <summary> Checks whether a serialized property has all the bits set when intepreted as a bitmask. </summary>
        /// <param name="agentMask"></param>
        /// <returns></returns>
        static bool IsAll(SerializedProperty agentMask)
        {
            return agentMask.arraySize == 1 && agentMask.GetArrayElementAtIndex(0).intValue == -1;
        }

        /// <summary> Marks one agent type as being selected or not. </summary>
        /// <param name="userData"></param>
        static void ToggleAgentMaskItem(object userData)
        {
            var args = (object[])userData;
            var agentMask = (SerializedProperty)args[0];
            var agentTypeID = (int)args[1];
            var value = (bool)args[2];

            ToggleAgentMaskItem(agentMask, agentTypeID, value);
        }

        /// <summary> Marks one agent type as being selected or not. </summary>
        /// <param name="agentMask"></param>
        /// <param name="agentTypeID"></param>
        /// <param name="value"></param>
        static void ToggleAgentMaskItem(SerializedProperty agentMask, int agentTypeID, bool value)
        {
            if (agentMask.hasMultipleDifferentValues)
            {
                agentMask.ClearArray();
                agentMask.serializedObject.ApplyModifiedProperties();
            }

            // Find which index this agent type is in the agentMask array.
            int idx = -1;
            for (var j = 0; j < agentMask.arraySize; j++)
            {
                var elem = agentMask.GetArrayElementAtIndex(j);
                if (elem.intValue == agentTypeID)
                    idx = j;
            }

            // Handle "All" special case.
            if (IsAll(agentMask))
            {
                agentMask.DeleteArrayElementAtIndex(0);
            }

            // Toggle value.
            if (value)
            {
                if (idx == -1)
                {
                    agentMask.InsertArrayElementAtIndex(agentMask.arraySize);
                    agentMask.GetArrayElementAtIndex(agentMask.arraySize - 1).intValue = agentTypeID;
                }
            }
            else
            {
                if (idx != -1)
                {
                    agentMask.DeleteArrayElementAtIndex(idx);
                }
            }

            agentMask.serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Marks all agent types as not being selected. </summary>
        /// <param name="data"></param>
        static void SetAgentMaskNone(object data)
        {
            var agentMask = (SerializedProperty)data;
            agentMask.ClearArray();
            agentMask.serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Marks all agent types as being selected. </summary>
        /// <param name="data"></param>
        static void SetAgentMaskAll(object data)
        {
            var agentMask = (SerializedProperty)data;
            agentMask.ClearArray();
            agentMask.InsertArrayElementAtIndex(0);
            agentMask.GetArrayElementAtIndex(0).intValue = -1;
            agentMask.serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Obtains one string that represents the current selection of agent types. </summary>
        /// <param name="agentMask"></param>
        /// <returns> One string that represents the current selection of agent types.</returns>
        static string GetAgentMaskLabelName(SerializedProperty agentMask)
        {
            if (agentMask.arraySize == 0)
                return "None";

            if (IsAll(agentMask))
                return "All";

            if (agentMask.arraySize <= 3)
            {
                var labelName = "";
                for (var j = 0; j < agentMask.arraySize; j++)
                {
                    var elem = agentMask.GetArrayElementAtIndex(j);
                    var settingsName = NavMesh.GetSettingsNameFromID(elem.intValue);
                    if (string.IsNullOrEmpty(settingsName))
                        continue;

                    if (labelName.Length > 0)
                        labelName += ", ";
                    labelName += settingsName;
                }
                return labelName;
            }

            return "Mixed...";
        }

        /// <summary> Checks whether a certain agent type is selected. </summary>
        /// <param name="agentMask"></param>
        /// <param name="agentTypeID"></param>
        /// <returns></returns>
        static bool AgentMaskHasSelectedAgentTypeID(SerializedProperty agentMask, int agentTypeID)
        {
            for (var j = 0; j < agentMask.arraySize; j++)
            {
                var elem = agentMask.GetArrayElementAtIndex(j);
                if (elem.intValue == agentTypeID)
                    return true;
            }
            return false;
        }
    }
}
