using UnityEngine;
using UnityEngine.AI;

namespace UnityEditor.AI
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof (NavMeshLink))]
    class NavMeshLinkEditor : Editor
    {
        SerializedProperty m_AgentTypeID;
        SerializedProperty m_Area;
        SerializedProperty m_AutoUpdatePosition;
        SerializedProperty m_Bidirectional;
        SerializedProperty m_EndPoint;
        SerializedProperty m_StartPoint;
        SerializedProperty m_Width;

        static int s_SelectedID;
        static int s_SelectedPoint = -1;

        static Color s_HandleColor = new Color (255f, 167f, 39f, 210f) / 255;
        static Color s_HandleColorDisabled = new Color (255f*0.75f, 167f*0.75f, 39f*0.75f, 100f) / 255;

        void OnEnable ()
        {
            m_AgentTypeID = serializedObject.FindProperty ("m_AgentTypeID");
            m_Area = serializedObject.FindProperty ("m_Area");
            m_AutoUpdatePosition = serializedObject.FindProperty ("m_AutoUpdatePosition");
            m_Bidirectional = serializedObject.FindProperty ("m_Bidirectional");
            m_EndPoint = serializedObject.FindProperty ("m_EndPoint");
            m_StartPoint = serializedObject.FindProperty ("m_StartPoint");
            m_Width = serializedObject.FindProperty ("m_Width");

            s_SelectedID = 0;
            s_SelectedPoint = -1;

            NavMeshVisualizationSettings.showNavigation++;
        }

        void OnDisable ()
        {
            NavMeshVisualizationSettings.showNavigation--;
        }

        void AlignTransformToEndPoints (NavMeshLink navLink)
        {
            var transform = navLink.transform;
            Vector3 worldStartPt = transform.TransformPoint (navLink.startPoint);
            Vector3 worldEndPt = transform.TransformPoint (navLink.endPoint);

            Vector3 forward = worldEndPt - worldStartPt;
            Vector3 up = transform.up;

            // Flatten
            forward -= Vector3.Dot (up, forward) * up;

            var rotation = new Quaternion ();
            rotation.SetLookRotation (forward, up);

            transform.rotation = rotation;
            transform.position = (worldEndPt + worldStartPt) * 0.5f;

            navLink.startPoint = transform.InverseTransformPoint (worldStartPt);
            navLink.endPoint = transform.InverseTransformPoint (worldEndPt);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update ();

            NavMeshEditorHelpers.AgentTypePopup ("Agent Type", m_AgentTypeID);
            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (m_StartPoint);
            EditorGUILayout.PropertyField (m_EndPoint);
            EditorGUILayout.PropertyField (m_Width);
            m_Width.floatValue = Mathf.Max (0.0f, m_Width.floatValue);
            GUILayout.BeginHorizontal ();
                GUILayout.Space (EditorGUIUtility.labelWidth);
                if (GUILayout.Button ("Align Transform To Points"))
                {
                    foreach (NavMeshLink navLink in targets)
                    {
                        Undo.RecordObject (navLink.transform, "Align Transform to End Points");
                        Undo.RecordObject (navLink, "Align Transform to End Points");
                        AlignTransformToEndPoints (navLink);
                    }
                }
            GUILayout.EndHorizontal ();
            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (m_AutoUpdatePosition);
            EditorGUILayout.PropertyField (m_Bidirectional);

            NavMeshEditorHelpers.AreaPopup ("Area Type", m_Area);

            serializedObject.ApplyModifiedProperties ();

            EditorGUILayout.Space ();
        }

        static Vector3 CalcLinkRight (NavMeshLink navLink)
        {
            Vector3 dir = navLink.endPoint - navLink.startPoint;
            return (new Vector3 (-dir.z, 0.0f, dir.x)).normalized;
        }

        static void DrawLink (NavMeshLink navLink)
        {
            Vector3 right = CalcLinkRight (navLink);
            float rad = navLink.width * 0.5f;

            Gizmos.DrawLine (navLink.startPoint - right * rad, navLink.startPoint + right * rad);
            Gizmos.DrawLine (navLink.endPoint - right * rad, navLink.endPoint + right * rad);
            Gizmos.DrawLine (navLink.startPoint - right * rad, navLink.endPoint - right * rad);
            Gizmos.DrawLine (navLink.startPoint + right * rad, navLink.endPoint + right * rad);
        }

        [DrawGizmo (GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
        static void RenderBoxGizmo (NavMeshLink navLink, GizmoType gizmoType)
        {
            if (!EditorApplication.isPlaying)
                navLink.UpdateLink();

            var color = s_HandleColor;
            if (!navLink.enabled)
                color = s_HandleColorDisabled;

            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            Gizmos.matrix = navLink.transform.localToWorldMatrix;

            Gizmos.color = color;
            DrawLink (navLink);

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;

            Gizmos.DrawIcon (navLink.transform.position, "NavMeshLink Gizmo", true);
        }

        [DrawGizmo (GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderBoxGizmoNotSelected (NavMeshLink navLink, GizmoType gizmoType)
        {
            if (NavMeshVisualizationSettings.showNavigation > 0)
            {
                var color = s_HandleColor;
                if (!navLink.enabled)
                    color = s_HandleColorDisabled;

                var oldColor = Gizmos.color;
                var oldMatrix = Gizmos.matrix;

                Gizmos.matrix = navLink.transform.localToWorldMatrix;

                Gizmos.color = color;
                DrawLink (navLink);

                Gizmos.matrix = oldMatrix;
                Gizmos.color = oldColor;
            }

            Gizmos.DrawIcon (navLink.transform.position, "NavMeshLink Gizmo", true);
        }

        public void OnSceneGUI()
        {
            var navLink = (NavMeshLink)target;
            if (!navLink.enabled)
                return;

            Vector3 startPt = navLink.transform.TransformPoint (navLink.startPoint);
            Vector3 endPt = navLink.transform.TransformPoint (navLink.endPoint);
            Vector3 midPt = Vector3.Lerp (startPt, endPt, 0.35f);
            float startSize = HandleUtility.GetHandleSize (startPt);
            float endSize = HandleUtility.GetHandleSize (endPt);
            float midSize = HandleUtility.GetHandleSize (midPt);

            Quaternion zup = Quaternion.FromToRotation (Vector3.forward, Vector3.up);
            Vector3 right = navLink.transform.TransformVector (CalcLinkRight (navLink));

            Color oldColor = Handles.color;
            Handles.color = s_HandleColor;

            Vector3 pos;

            if (navLink.GetInstanceID() == s_SelectedID && s_SelectedPoint == 0)
            {
                EditorGUI.BeginChangeCheck ();
                Handles.CubeCap (0, startPt, zup, 0.1f * startSize);
                pos = Handles.PositionHandle (startPt, navLink.transform.rotation);
                if (EditorGUI.EndChangeCheck ())
                {
                    Undo.RecordObject (navLink, "Move link point");
                    navLink.startPoint = navLink.transform.InverseTransformPoint (pos);
                }
            }
            else
            {
                if (Handles.Button (startPt, zup, 0.1f * startSize, 0.1f * startSize, Handles.CubeCap))
                {
                    s_SelectedPoint = 0;
                    s_SelectedID = navLink.GetInstanceID();
                }
            }

            if (navLink.GetInstanceID() == s_SelectedID && s_SelectedPoint == 1)
            {
                EditorGUI.BeginChangeCheck ();
                Handles.CubeCap (0, endPt, zup, 0.1f * startSize);
                pos = Handles.PositionHandle (endPt, navLink.transform.rotation);
                if (EditorGUI.EndChangeCheck ())
                {
                    Undo.RecordObject (navLink, "Move link point");
                    navLink.endPoint = navLink.transform.InverseTransformPoint (pos);
                }
            }
            else
            {
                if (Handles.Button (endPt, zup, 0.1f * endSize, 0.1f * endSize, Handles.CubeCap))
                {
                    s_SelectedPoint = 1;
                    s_SelectedID = navLink.GetInstanceID();
                }
            }

            EditorGUI.BeginChangeCheck ();
            pos = Handles.Slider (midPt + right * navLink.width*0.5f, right, midSize * 0.03f, Handles.DotCap, 0);
            if (EditorGUI.EndChangeCheck ())
            {
                Undo.RecordObject (navLink, "Adjust link width");
                navLink.width = Mathf.Max (0.0f, Vector3.Dot (right, (pos - midPt)) * 2.0f);
            }

            EditorGUI.BeginChangeCheck ();
            pos = Handles.Slider (midPt - right * navLink.width*0.5f, -right, midSize * 0.03f, Handles.DotCap, 0);
            if (EditorGUI.EndChangeCheck ())
            {
                Undo.RecordObject (navLink, "Adjust link width");
                navLink.width = Mathf.Max (0.0f, Vector3.Dot (-right, (pos - midPt)) * 2.0f);
            }

            Handles.color = oldColor;
        }

        [MenuItem ("GameObject/AI/NavMesh Link", false, 2002)]
        static public void CreateNavMeshLink (MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            GameObject go = NavMeshEditorHelpers.CreateAndSelectGameObject ("NavMesh Link", parent);
            go.AddComponent<NavMeshLink> ();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView (go.transform);
        }
    }
}
