using UnityEngine;
using UnityEditor;

namespace DeepSky.Haze
{
    [CustomEditor(typeof(DS_HazeCore))]
    public class DS_HazeCoreEditor : Editor
    {
        private const string kHelpTxt = "This is the time at which zones are evaluated during rendering. " +
            "Animate it, or set via a time-of-day system, to create transitions throughout a day/night cycle.";

        private string[] m_DebugGUIPositionStr = {
            '\u2196'.ToString(),
            '\u2191'.ToString(),
            '\u2197'.ToString(),
            '\u2190'.ToString(),
            "-",
            '\u2192'.ToString(),
            '\u2199'.ToString(),
            '\u2193'.ToString(),
            '\u2198'.ToString() };

        /// <summary>
        /// Custom Inspector drawing for DS_HazeCore components.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty guiProp = serializedObject.FindProperty("m_ShowDebugGUI");
            SerializedProperty guiPositionProp = serializedObject.FindProperty("m_DebugGUIPosition");

            DS_HazeCore core = target as DS_HazeCore;
            bool texChange = false;

            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(kHelpTxt, MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HeightFalloff"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Time"));

                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NoiseLUT"));
                if(EditorGUI.EndChangeCheck())
                {
                    texChange = true;
                }
                EditorGUILayout.PropertyField(guiProp);
                GUI.enabled = guiProp.boolValue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Debug GUI Position");
                guiPositionProp.enumValueIndex = GUILayout.SelectionGrid(guiPositionProp.enumValueIndex, m_DebugGUIPositionStr, 3, EditorStyles.miniButton);
                GUILayout.FlexibleSpace();
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (texChange) core.SetGlobalNoiseLUT();
        }
    }
}