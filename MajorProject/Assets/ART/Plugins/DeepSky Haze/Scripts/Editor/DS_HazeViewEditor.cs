using UnityEngine;
using UnityEditor;

namespace DeepSky.Haze
{
    [CustomEditor(typeof(DS_HazeView))]
    public class DS_HazeViewEditor : Editor
    {
        #region HELP_STRINGS
        private static string m_HelpTxtOverrides =
            "Add a reference to a saved <b>DeepSky Haze Scriptable Context</b> to render using its settings (rather " +
            "than the settings from the Zone system).\nOverride the time at which the settings are evaluated " +
            "<b>or</b> choose a specific time-of-day variant from the dropdown.";
        private static string m_HelpTxtShader =
            "These options control shader features and enable/disable <b>haze</b> and <b>fog</b> rendering.\n" +
            "Adjust the <b>Gaussian Depth Falloff</b> to control how differences in depth affect the blur pass. Large " +
            "values allow more blurring to reduce artifacts but increase the chance of the blur \"leaking\" over surrounding " +
            "geometry.\nSimilarly, <b>Upsample Depth Threshold</b> controls how differences in depth affect the final " +
            "compose. Smaller values help preserve small details and prevent the atmospherics \"leaking\" over surrounding " +
            "geometry.\nTemporal reprojection uses the <b>Temporal Rejection Scale</b> value to determine how similar pixels " +
            "are between frames. Lower values allow more reuse for smoother results but increase the chance of artifacts or " +
            "\"ghosting.\" The rejected pixels can be displayed by enabling <b>Show Temporal Rejection</b>.";
        #endregion

        private bool m_HelpTxtOverridesExpanded = false;
        private bool m_HelpTxtShaderFeaturesExpanded = false;

        /// <summary>
        /// Draw the custom Inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Styling and icons.
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

            // Get the serialized properties upfront.
            serializedObject.Update();
            DS_HazeView view = target as DS_HazeView;
            SerializedProperty renderAtmos = serializedObject.FindProperty("m_RenderAtmosphereVolumetrics");
            SerializedProperty renderLocal = serializedObject.FindProperty("m_RenderLocalVolumetrics");
            SerializedProperty useTemporal = serializedObject.FindProperty("m_TemporalReprojection");
            SerializedProperty sizeFactor = serializedObject.FindProperty("m_DownsampleFactor");
            SerializedProperty samples = serializedObject.FindProperty("m_VolumeSamples");
            SerializedProperty lightProp = serializedObject.FindProperty("m_DirectLight");
            SerializedProperty ctxOverProp = serializedObject.FindProperty("m_OverrideContextAsset");
            SerializedProperty ctxProp = serializedObject.FindProperty("m_Context");
            SerializedProperty ctxTimeProp = serializedObject.FindProperty("m_OverrideTime");
            SerializedProperty ctxVariantOverProp = serializedObject.FindProperty("m_OverrideContextVariant");
            SerializedProperty ctxVariantProp = serializedObject.FindProperty("m_ContextItemIndex");
            SerializedProperty ctxGaussDepthProp = serializedObject.FindProperty("m_GaussianDepthFalloff");
            SerializedProperty ctxUpDepthProp = serializedObject.FindProperty("m_UpsampleDepthThreshold");
            SerializedProperty ctxTemporalProp = serializedObject.FindProperty("m_TemporalRejectionScale");
            SerializedProperty ctxTemporalBlendProp = serializedObject.FindProperty("m_TemporalBlendFactor");
            SerializedProperty applyAirProp = serializedObject.FindProperty("m_ApplyAirToSkybox");
            SerializedProperty applyHazeProp = serializedObject.FindProperty("m_ApplyHazeToSkybox");
            SerializedProperty applyFogEProp = serializedObject.FindProperty("m_ApplyFogExtinctionToSkybox");
            SerializedProperty applyFogRProp = serializedObject.FindProperty("m_ApplyFogLightingToSkybox");
            SerializedProperty ctxShowTemporalProp = serializedObject.FindProperty("m_ShowTemporalRejection");
            SerializedProperty ctxShowUpsampleThresholdProp = serializedObject.FindProperty("m_ShowUpsampleThreshold");

            // Populate the array of context variant names for the popup from the override.
            string[] variants = { string.Empty };
            variants = GetVariantsArray();

            bool updateTemporalKeywords = false;
            bool updateSkyboxKeywords = false;
            bool updateDebugKeywords = false;

            // Main GUI layout and drawing.
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("General:", EditorStyles.boldLabel);

                GUI.enabled = renderAtmos.boolValue | renderLocal.boolValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(useTemporal);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(renderAtmos);
                EditorGUILayout.PropertyField(renderLocal);
                if (EditorGUI.EndChangeCheck())
                {
                    updateTemporalKeywords = true;
                }

                EditorGUILayout.PropertyField(sizeFactor);
                EditorGUILayout.PropertyField(samples);

                EditorGUI.BeginChangeCheck();
                Object tmp = lightProp.objectReferenceValue;
                EditorGUILayout.PropertyField(lightProp);
                if (EditorGUI.EndChangeCheck())
                {
                    // Remove the command buffer if no longer referencing the directional light.
                    if (tmp != null && tmp != lightProp.objectReferenceValue)
                    {
                        view.RemoveCommandBufferFromLight((Light)tmp);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Overrides:", EditorStyles.boldLabel);
                m_HelpTxtOverridesExpanded = EditorGUILayout.Toggle(m_HelpTxtOverridesExpanded, helpIconStyle, GUILayout.Width(helpIconImage.width));
                EditorGUILayout.EndHorizontal();
                if (m_HelpTxtOverridesExpanded) EditorGUILayout.TextArea(m_HelpTxtOverrides, helpBoxStyle);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    ctxOverProp.boolValue = EditorGUILayout.Toggle(ctxOverProp.boolValue, GUILayout.Width(16));
                    if(EditorGUI.EndChangeCheck())
                    {
                        variants = GetVariantsArray();
                    }
                    if (!ctxOverProp.boolValue) GUI.enabled = false;
                    EditorGUILayout.LabelField("Context:", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(ctxProp, GUIContent.none);
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    if (!ctxOverProp.boolValue) GUI.enabled = false;
                    EditorGUI.BeginChangeCheck();
                    ctxTimeProp.boolValue = EditorGUILayout.Toggle(ctxTimeProp.boolValue, EditorStyles.radioButton, GUILayout.Width(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (ctxTimeProp.boolValue)
                        {
                            ctxVariantOverProp.boolValue = false;
                        }
                    }
                    if (!ctxTimeProp.boolValue) GUI.enabled = false;
                    EditorGUILayout.LabelField("Time:", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Time"), GUIContent.none);
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    if (!ctxOverProp.boolValue) GUI.enabled = false;
                    EditorGUI.BeginChangeCheck();
                    ctxVariantOverProp.boolValue = EditorGUILayout.Toggle(ctxVariantOverProp.boolValue, EditorStyles.radioButton, GUILayout.Width(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (ctxVariantOverProp.boolValue)
                        {
                            ctxTimeProp.boolValue = false;
                        }
                    }
                    if (!ctxVariantOverProp.boolValue) GUI.enabled = false;
                    EditorGUILayout.LabelField("Variant:", GUILayout.Width(100));
                    ctxVariantProp.intValue = EditorGUILayout.Popup(ctxVariantProp.intValue, variants);
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Shader Parameters:", EditorStyles.boldLabel);
                m_HelpTxtShaderFeaturesExpanded = EditorGUILayout.Toggle(m_HelpTxtShaderFeaturesExpanded, helpIconStyle, GUILayout.Width(helpIconImage.width));
                EditorGUILayout.EndHorizontal();
                if (m_HelpTxtShaderFeaturesExpanded) EditorGUILayout.TextArea(m_HelpTxtShader, helpBoxStyle);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Apply To Skybox:");
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(applyAirProp, new GUIContent("Air"));
                EditorGUILayout.PropertyField(applyHazeProp, new GUIContent("Haze"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(applyFogEProp, new GUIContent("Fog"));
                EditorGUILayout.PropertyField(applyFogRProp, new GUIContent("Fog Lighting"));
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                if (EditorGUI.EndChangeCheck())
                {
                    updateSkyboxKeywords = true;
                }
                EditorGUILayout.PropertyField(ctxGaussDepthProp);
                EditorGUILayout.PropertyField(ctxUpDepthProp);
                if (useTemporal.boolValue == false)
                {
                    GUI.enabled = false;
                }
                EditorGUILayout.PropertyField(ctxTemporalProp);
                EditorGUILayout.PropertyField(ctxTemporalBlendProp);
                GUI.enabled = true;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Options:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    if (useTemporal.boolValue == false) GUI.enabled = false;
                    ctxShowTemporalProp.boolValue = EditorGUILayout.Toggle(ctxShowTemporalProp.boolValue, EditorStyles.radioButton, GUILayout.Width(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (ctxShowTemporalProp.boolValue)
                        {
                            ctxShowUpsampleThresholdProp.boolValue = false;
                        }
                        updateDebugKeywords = true;
                    }
                    if (!ctxShowTemporalProp.boolValue) GUI.enabled = false;
                    EditorGUILayout.LabelField("Show Temporal Rejection");
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    ctxShowUpsampleThresholdProp.boolValue = EditorGUILayout.Toggle(ctxShowUpsampleThresholdProp.boolValue, EditorStyles.radioButton, GUILayout.Width(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (ctxShowUpsampleThresholdProp.boolValue)
                        {
                            ctxShowTemporalProp.boolValue = false;
                        }
                        updateDebugKeywords = true;
                    }
                    if (!ctxShowUpsampleThresholdProp.boolValue) GUI.enabled = false;
                    EditorGUILayout.LabelField("Show Upsample Threshold");
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (updateTemporalKeywords) view.SetTemporalKeywords();
            if (updateSkyboxKeywords) view.SetSkyboxKeywords();
            if (updateDebugKeywords) view.SetDebugKeywords();
        }

        /// <summary>
        /// Get an array of names from the context to display in a popup. These are in the same
        /// order as the list.
        /// </summary>
        /// <param name="useOverride"> Should the variant come from the override context? </param>
        /// <returns> string array of variant names. </returns>
        private string[] GetVariantsArray()
        {
            DS_HazeView view = target as DS_HazeView;

            return view.ContextAsset != null ? view.ContextAsset.Context.GetItemNames() : new string[] { string.Empty };
        }
    }
}
