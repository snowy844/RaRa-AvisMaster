using UnityEngine;
using UnityEditor;

using System.IO;

namespace DeepSky.Haze
{
    [CustomEditor(typeof(DS_HazeZone))]
    public class DS_HazeZoneEditor : Editor
    {
        #region HELP_STRINGS
        private const string kHelpTxt =
            "Save this context with <b>Create Preset</b> and load an existing one with <b>Load Preset</b> (this will " +
            "overwrite the current settings!).\n" +
            "The <b>Current Time</b> is the time at which the time-of-day variants are rendered by any " +
            "<b>DeepSky:Haze View</b> components using these settings. Set this value from a time-of-day system for " +
            "smooth transitions during a day/night cycle.\n" +
            "The <b>Time-of-day Variants</b> stack displays all the variants within this context. They are blended " +
            "from <b>top-to-bottom</b> according to their individual weight curves. There is always at least one variant and " +
            "the top variant in the stack always has <b>a weight of one</b> (regardless of its weight curve).\n" +
            "Each time-of-day variant can be set to <b>solo</b> to display only that variant (editor only), has a " +
            "<b>weight curve</b> to control when they are active plus controls to <b>duplicate</b>, <b>move up</b> the stack, " +
            "<b>move down</b> the stack and <b>delete</b>.";
        #endregion

        private static Color kSelectedColour = new Color(0, 0.78f, 0.73f);
        private static Color kUnselectedColour = new Color(0, 0.45f, 0.41f);

        private SerializedProperty m_ContextProp;
        private DS_HazePresetNamePopup m_PresetNamePopup;
        private Rect m_SavePresetRect;
        private DS_HazeContextAsset m_WaitingToLoad = null;
        private bool m_HelpTxtExpanded = false;

        /// <summary>
        /// Get references and register the Create New Preset callback.
        /// </summary>
        public void OnEnable()
        {
            m_ContextProp = serializedObject.FindProperty("m_Context");
            m_PresetNamePopup = new DS_HazePresetNamePopup();
            m_PresetNamePopup.OnCreate += CreateNewHazeContextPreset;
        }

        /// <summary>
        /// Custom Inspector drawing for DS_HazeCore components.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Make the foldout text bold.
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            GUIStyle headerLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            headerLabelStyle.alignment = TextAnchor.MiddleLeft;

            // Get the styles for the mini buttons and 'solo' toggle.
            GUIStyle variantStyle = new GUIStyle(EditorStyles.helpBox);
            variantStyle.padding = new RectOffset(0, 2, 3, 3);
            GUIStyle buttonLeft = GUI.skin.FindStyle("ButtonLeft");
            GUIStyle buttonRight = GUI.skin.FindStyle("ButtonRight");
            GUIStyle miniButtonStyleL = new GUIStyle(EditorStyles.miniButtonLeft);
            GUIStyle miniButtonStyleM = new GUIStyle(EditorStyles.miniButtonMid);
            GUIStyle miniButtonStyleR = new GUIStyle(EditorStyles.miniButtonRight);

            // Style for expandable help text.
            GUIStyle helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
            helpBoxStyle.richText = true;
            Texture2D helpIconImage = EditorGUIUtility.FindTexture("console.infoicon.sml");
            GUIStyle helpIconStyle = new GUIStyle();
            helpIconStyle.normal.background = helpIconImage;
            helpIconStyle.onNormal.background = helpIconImage;
            helpIconStyle.active.background = helpIconImage;
            helpIconStyle.onActive.background = helpIconImage;
            helpIconStyle.focused.background = helpIconImage;
            helpIconStyle.onFocused.background = helpIconImage;

            serializedObject.Update();
            DS_HazeZone hazeZone = target as DS_HazeZone;
            SerializedProperty soloIndexProp = m_ContextProp.FindPropertyRelative("m_SoloItem");

            // Handle the object picker for selecting a preset. Note that the actual loading happens at the end of
            // this function and only after a Repaint event.
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Zone Parameters:", headerLabelStyle);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Priority"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BlendRange"));

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Create Preset", buttonLeft))
                    {
                        PopupWindow.Show(m_SavePresetRect, m_PresetNamePopup);
                    }
                    if (Event.current.type == EventType.Repaint) m_SavePresetRect = GUILayoutUtility.GetLastRect();

                    if (GUILayout.Button("Load Preset", buttonRight))
                    {
                        int ctrlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<DS_HazeContextAsset>(null, false, "", ctrlID);
                    }
                    m_HelpTxtExpanded = EditorGUILayout.Toggle(m_HelpTxtExpanded, helpIconStyle, GUILayout.Width(helpIconImage.width));
                }
                EditorGUILayout.EndHorizontal();
                if (m_HelpTxtExpanded) EditorGUILayout.TextArea(kHelpTxt, helpBoxStyle);


                // Check for messages returned by the object picker.
                if (Event.current.commandName == "ObjectSelectorClosed")
                {
                    m_WaitingToLoad = EditorGUIUtility.GetObjectPickerObject() as DS_HazeContextAsset;
                }
                EditorGUILayout.Space();

                // Manually calculate a scaling factor based on the current width of the Inspector and a fixed minimum width.
                float maxLabelWidth = EditorGUIUtility.labelWidth;
                if (EditorGUIUtility.currentViewWidth < 350)
                {
                    maxLabelWidth = Mathf.Lerp(50, EditorGUIUtility.labelWidth, (1.0f / (350.0f - 275.0f)) * (EditorGUIUtility.currentViewWidth - 275.0f));
                }

                EditorGUILayout.LabelField("Time-Of-Day Variants:", EditorStyles.boldLabel);
                SerializedProperty ctxVariants = m_ContextProp.FindPropertyRelative("m_ContextItems");
                for (int cv = 0; cv < ctxVariants.arraySize; cv++)
                {
                    SerializedProperty cvElem = ctxVariants.GetArrayElementAtIndex(cv);
                    SerializedProperty cvName = cvElem.FindPropertyRelative("m_Name");
                    SerializedProperty cvWeightProp = cvElem.FindPropertyRelative("m_Weight");

                    Rect cvRect = EditorGUILayout.BeginHorizontal(variantStyle);
                    {
                        cvRect.y += 5;
                        cvRect.width = 50;

                        // Draw the foldout and get the expanded state of this variant.
                        cvElem.isExpanded = EditorGUI.Foldout(cvRect, cvElem.isExpanded, GUIContent.none, true, foldoutStyle);
                        EditorGUILayout.LabelField(cvName.stringValue, headerLabelStyle, GUILayout.Width(maxLabelWidth));

                        // Draw the 'solo' button.
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.Toggle(cv == soloIndexProp.intValue, GUILayout.Width(12));
                        if (EditorGUI.EndChangeCheck())
                        {
                            soloIndexProp.intValue = soloIndexProp.intValue != cv ? cv : -1;
                        }

                        // Draw the weight curve as part of the header, so we can edit the weighting without having to expand the whole context.
                        cvWeightProp.animationCurveValue = EditorGUILayout.CurveField(cvWeightProp.animationCurveValue, Color.green, new Rect(0, 0, 1, 1));

                        // Add, move up/down and delete buttons.
                        if (GUILayout.Button("+", miniButtonStyleL))
                        {
                            hazeZone.Context.DuplicateContextItem(cv);
                        }
                        if (GUILayout.Button('\u25B2'.ToString(), miniButtonStyleM))
                        {
                            hazeZone.Context.MoveContextItemUp(cv);
                        }
                        if (GUILayout.Button('\u25BC'.ToString(), miniButtonStyleM))
                        {
                            hazeZone.Context.MoveContextItemDown(cv);
                        }
                        if (GUILayout.Button("-", miniButtonStyleR))
                        {
                            hazeZone.Context.RemoveContextItem(cv);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // If the context variant is expanded, draw its actual property below the header.
                    if (cvElem.isExpanded)
                    {
                        EditorGUILayout.PropertyField(cvElem);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();

            // Check if there's a context waiting to be loaded. We need to wait until
            // after a repaint event as it will probably modify the size of the context variants
            // list. This can cause a mis-match between what was setup during the Layout event and
            // what we're now drawing during a Repaint.
            if (Event.current.type == EventType.Repaint && m_WaitingToLoad != null)
            {
                LoadFromContextPreset(m_WaitingToLoad);
                m_WaitingToLoad = null;
            }
        }

        /// <summary>
        /// Save a new context preset from the context currently displayed
        /// in the editor.
        /// </summary>
        public void CreateNewHazeContextPreset(string name)
        {
            DS_HazeZone editingObject = (DS_HazeZone)target;

            DS_HazeContextAsset asset = editingObject.Context.GetContextAsset();

            string[] paths = Directory.GetDirectories(Application.dataPath, "DeepSky Haze", SearchOption.AllDirectories);
            if (paths.Length == 0 || paths.Length > 1)
            {
                Debug.LogError("DS_HazeZoneEditor::CreateNewHazeContextPreset: Unable to find the DeepSky Haze folder! Has it been renamed?");
                return;
            }
            int assetind = paths[0].IndexOf("Assets", 0);
            string rootpath = paths[0].Substring(assetind);
            string contextpath = rootpath + Path.DirectorySeparatorChar + "Contexts";

            if (!AssetDatabase.IsValidFolder(contextpath))
            {
                AssetDatabase.CreateFolder(rootpath, "Contexts");
            }

            AssetDatabase.CreateAsset(asset, contextpath + Path.DirectorySeparatorChar + name + ".asset");
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// Set the context from the preset context asset.
        /// </summary>
        /// <param name="cxt"></param>
        private void LoadFromContextPreset(DS_HazeContextAsset ctx)
        {
            DS_HazeZone editingObject = (DS_HazeZone)target;
            editingObject.Context.CopyFrom(ctx.Context);
        }

        /// <summary>
        /// Used to set a specified zone from a preset context asset.
        /// </summary>
        /// <param name="zone"> The zone to set.</param>
        /// <param name="ctx"> The context to load the preset from.</param>
        public static void SetZoneFromContextPreset(DS_HazeZone zone, DS_HazeContextAsset ctx)
        {
            if (!zone)
            {
                Debug.LogError("DeepSky::DS_HazeZoneEditor:SetZoneFromContextPreset - null zone passed!");
                return;
            }
            if (!ctx)
            {
                Debug.LogError("DeepSky::DS_HazeZoneEditor:SetZoneFromContextPreset - null ctx passed!");
                return;
            }

            zone.Context.CopyFrom(ctx.Context);
        }


        /// <summary>
        /// Draw the gizmo showing the zone size and inner edge of the blend range.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="gizmoType"></param>
        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy | GizmoType.Active | GizmoType.Pickable)]
        public static void DS_HazeZoneDrawGizmo(DS_HazeZone src, GizmoType gizmoType)
        {
            Transform xForm = src.transform;
            float dT;
            Vector3 sizes;

            Gizmos.matrix = xForm.localToWorldMatrix;
            if ((gizmoType & GizmoType.Active) != GizmoType.Active)
            { 
                dT = Mathf.Min(xForm.localScale.x, xForm.localScale.y, xForm.localScale.z) * src.BlendRange;
                sizes = xForm.localScale;
                sizes.x = (sizes.x - dT) / sizes.x;
                sizes.y = (sizes.y - dT) / sizes.y;
                sizes.z = (sizes.z - dT) / sizes.z;
                sizes *= 0.5f;

                Vector3[] vtxs = {
                    new Vector3(sizes.x, sizes.y, sizes.z),
                    new Vector3(-sizes.x, sizes.y, sizes.z),
                    new Vector3(-sizes.x, sizes.y, sizes.z),
                    new Vector3(-sizes.x, sizes.y, -sizes.z),
                    new Vector3(-sizes.x, sizes.y, -sizes.z),
                    new Vector3(sizes.x, sizes.y, -sizes.z),
                    new Vector3(sizes.x, sizes.y, -sizes.z),
                    new Vector3(sizes.x, sizes.y, sizes.z),
                    new Vector3(sizes.x, -sizes.y, sizes.z),
                    new Vector3(-sizes.x, -sizes.y, sizes.z),
                    new Vector3(-sizes.x, -sizes.y, sizes.z),
                    new Vector3(-sizes.x, -sizes.y, -sizes.z),
                    new Vector3(-sizes.x, -sizes.y, -sizes.z),
                    new Vector3(sizes.x, -sizes.y, -sizes.z),
                    new Vector3(sizes.x, -sizes.y, -sizes.z),
                    new Vector3(sizes.x, -sizes.y, sizes.z),
                    new Vector3(sizes.x, sizes.y, sizes.z),
                    new Vector3(sizes.x, -sizes.y, sizes.z),
                    new Vector3(-sizes.x, sizes.y, sizes.z),
                    new Vector3(-sizes.x, -sizes.y, sizes.z),
                    new Vector3(-sizes.x, sizes.y, -sizes.z),
                    new Vector3(-sizes.x, -sizes.y, -sizes.z),
                    new Vector3(sizes.x, sizes.y, -sizes.z),
                    new Vector3(sizes.x, -sizes.y, -sizes.z),
                };

                Matrix4x4 handleM = Handles.matrix;
                Handles.matrix = xForm.localToWorldMatrix;

#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2
                Handles.color = Color.yellow;
                for (int vi=0; vi < vtxs.Length; vi+=2)
                {
                    Handles.DrawLine(vtxs[vi], vtxs[vi+1]);
                }
#else
                Handles.color = Color.yellow;
                Handles.DrawDottedLines(vtxs, 5);
#endif
                Handles.matrix = handleM;

                Gizmos.color = kUnselectedColour;
            }
            else
            {
                Gizmos.color = Color.green;
                dT = Mathf.Min(xForm.localScale.x, xForm.localScale.y, xForm.localScale.z) * src.BlendRange;
                sizes = xForm.localScale;
                sizes.x = (sizes.x - dT) / sizes.x;
                sizes.y = (sizes.y - dT) / sizes.y;
                sizes.z = (sizes.z - dT) / sizes.z;
                Gizmos.DrawWireCube(Vector3.zero, sizes);

                Gizmos.color = kSelectedColour;
            }
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}