using UnityEngine;
using UnityEditor;

namespace DeepSky.Haze
{
    [CustomPropertyDrawer(typeof(DS_HazeContextItem))]
    public class DS_HazeContextItemDrawer : PropertyDrawer
    {
        private static string[] MultiplierStr = { "0.1", "0.2", "0.5", "1", "2", "5", "10" };

        #region HELP_STRINGS
        private const string strToolTipAirScatterScale =
            "The amount of scattering due to air molecules. Higher values result in more light from the sky being " +
            "scattered into the view direction.";
        private const string strToolTipAirScatterMult =
            "Multiplier for air scattering scale. Applying a multiplier other than 1 can make it easier to control " +
            "the air scattering in scenes with very large (try < 1) or very short (try > 1) view distances.";
        private const string strToolTipAirHeightFalloff =
            "How quickly the air density falls off with height. Higher values increase the rate at which density " +
            "approaches zero.";
        private const string strToolTipHazeScatterScale =
            "The amount of scattering due to aerosols (dust, pollution). Higher values result in more " +
            "light being scattered into the view direction.";
        private const string strToolTipHazeScatterMult =
            "Multiplier for haze scattering scale. Applying a multiplier other than 1 can make it easier to control " +
            "the haze scattering in scenes with very large (try < 1) or very short (try > 1) view distances.";
        private const string strToolTipHazeHeightFalloff =
            "How quickly the aerosol density falls off with height. Higher values increase the rate at which density " +
            "approaches zero.";
        private const string strToolTipHazeScatterDirection =
            "The dominant direction in which the light scatters. Positive values result in more forward scattering; " +
            "the light is more likely to continue in the same direction. Negative values result in more back " +
            "scattering; the light is more likely to scatter back towards the light source. A value of zero scatters " +
            "in all directions equally.";
        private const string strToolTipHazeIndirectRatio =
            "The ratio between indirect and direct aerosol scattering. Higher values result in more indirect scattering and " +
            "will increase the haze brightness in darker/shadowed areas.";
        private const string strToolTipFogOpacity =
            "Global fog opacity - the maximum amount the fog can obscure the scene.";
        private const string strToolTipFogScatterScale =
            "The amount of scattering due to water vapour (fog/mist). Higher values result in more light from the sky being " +
            "scattered into the view direction.";
        private const string strToolTipFogExtMult =
            "Multiplier for fog extinction scale. Applying a multiplier other than 1 can make it easier to control " +
            "the fog in scenes with very large (try < 1) or very short (try > 1) view distances.";
        private const string strToolTipFogExtScale =
            "The amount of scattering out of the view ray due to water vapour (fog/mist). Higher values result in less light " +
            "from the background reaching the camera.";
        private const string strToolTipFogHeightFalloff =
            "How quickly the fog/mist density falls off with height. Higher values increase the rate at which density " +
            "approaches zero.";
        private const string strToolTipFogStartDistance =
            "The distance from the camera at which the fog starts (1 = far clip plane).";
        private const string strToolTipFogScatterDirection =
            "The dominant direction in which the light scatters. Positive values result in more forward scattering; " +
            "the light is more likely to continue in the same direction. Negative values result in more back " +
            "scattering; the light is more likely to scatter back towards the light source. A value of zero scatters " +
            "in all directions equally.";
        private const string strToolTipFogAmbCol =
            "The ambient (or skylighting) colour of the fog. This is the colour the fog will have when not directly " +
            "illuminated by the directional light source.";
        private const string strToolTipFogLightCol =
            "The direct lighting colour of the fog. This is the colour the fog will have when illuminated by the " +
            "directional light source.";
        #endregion

        private const float kControlLineCount = 21f;

        static public float expandedHeight {
            get { return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * kControlLineCount; }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel++;

            float curY = position.y + EditorGUIUtility.singleLineHeight / 2;
            float ctrlCount = 0;
            float ctrlSizeY = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Get all the rects upfront.
            Rect name = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect aLabel = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect aBetaSMult = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect aBetaS = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect aFalloff = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hLabel = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hBetaSMult = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hBetaS = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hFalloff = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hDir = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect hRatio = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fLabel = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fOpacity = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fBetaS = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fExtMult = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fExtS = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fFalloff = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fDist = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fDir = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fAmb = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);
            Rect fLight = new Rect(position.x, curY + ctrlSizeY * ctrlCount++, position.width, EditorGUIUtility.singleLineHeight);

            // Draw all the properties and labels.
            SerializedProperty airMultProp = property.FindPropertyRelative("m_AirScatteringMultiplier");
            SerializedProperty hazeMultProp = property.FindPropertyRelative("m_HazeScatteringMultiplier");
            SerializedProperty fogEMultProp = property.FindPropertyRelative("m_FogExtinctionMultiplier");
            SerializedProperty airScatterProp = property.FindPropertyRelative("m_AirScatteringScale");
            SerializedProperty hazeScatterProp = property.FindPropertyRelative("m_HazeScatteringScale");
            SerializedProperty fogEProp = property.FindPropertyRelative("m_FogExtinctionScale");

            GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
            popupStyle.alignment = TextAnchor.MiddleLeft;
            popupStyle.fixedWidth = 40;

            DS_HazeCore.HeightFalloffType falloff = DS_HazeCore.HeightFalloffType.Exponential;

            if (DS_HazeCore.Instance != null)
            {
                falloff = DS_HazeCore.Instance.HeightFalloff;
            }
            
            EditorGUI.PropertyField(name, property.FindPropertyRelative("m_Name"));
            EditorGUI.LabelField(aLabel, "Air:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            airMultProp.enumValueIndex = EditorGUI.Popup(aBetaSMult, "Scattering Multiplier", airMultProp.enumValueIndex, MultiplierStr, popupStyle);

            float airParamMax = 8 * DS_HazeContextItem.MultiplierAsFloat((DS_HazeContextItem.Multiplier)airMultProp.enumValueIndex);
            EditorGUI.Slider(aBetaS, airScatterProp, 0, airParamMax, "Scattering");

            if (EditorGUI.EndChangeCheck())
            {
                
                if (airScatterProp.floatValue > airParamMax)
                {
                    airScatterProp.floatValue = airParamMax;
                }
            }

            bool wasEnabled = GUI.enabled;
            if (falloff != DS_HazeCore.HeightFalloffType.Exponential) GUI.enabled = false;
            EditorGUI.PropertyField(aFalloff, property.FindPropertyRelative("m_AirDensityHeightFalloff"), new GUIContent("Height Falloff", strToolTipAirHeightFalloff));
            GUI.enabled = wasEnabled;

            EditorGUI.LabelField(hLabel, "Haze:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            hazeMultProp.enumValueIndex = EditorGUI.Popup(hBetaSMult, "Scattering Multiplier", hazeMultProp.enumValueIndex, MultiplierStr, popupStyle);

            float hazeParamMax = 8 * DS_HazeContextItem.MultiplierAsFloat((DS_HazeContextItem.Multiplier)hazeMultProp.enumValueIndex);
            EditorGUI.Slider(hBetaS, hazeScatterProp, 0, hazeParamMax, "Scattering");

            if (EditorGUI.EndChangeCheck())
            {

                if (hazeScatterProp.floatValue > hazeParamMax)
                {
                    hazeScatterProp.floatValue = hazeParamMax;
                }
            }

            wasEnabled = GUI.enabled;
            if (falloff != DS_HazeCore.HeightFalloffType.Exponential) GUI.enabled = false;
            EditorGUI.PropertyField(hFalloff, property.FindPropertyRelative("m_HazeDensityHeightFalloff"), new GUIContent("Height Falloff", strToolTipHazeHeightFalloff));
            GUI.enabled = wasEnabled;

            EditorGUI.PropertyField(hDir, property.FindPropertyRelative("m_HazeScatteringDirection"), new GUIContent("Scatter Direction", strToolTipHazeScatterDirection));
            EditorGUI.PropertyField(hRatio, property.FindPropertyRelative("m_HazeSecondaryScatteringRatio"), new GUIContent("Direct/Indirect Ratio", strToolTipHazeIndirectRatio));
            EditorGUI.LabelField(fLabel, "Fog/Mist:", EditorStyles.boldLabel);
            EditorGUI.PropertyField(fOpacity, property.FindPropertyRelative("m_FogOpacity"), new GUIContent("Opacity", strToolTipFogOpacity));

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(fBetaS, property.FindPropertyRelative("m_FogScatteringScale"), new GUIContent("Scattering", strToolTipFogScatterScale));
            fogEMultProp.enumValueIndex = EditorGUI.Popup(fExtMult, "Extinction Multiplier", fogEMultProp.enumValueIndex, MultiplierStr, popupStyle);

            float fogEParamMax = 8 * DS_HazeContextItem.MultiplierAsFloat((DS_HazeContextItem.Multiplier)fogEMultProp.enumValueIndex);
            EditorGUI.Slider(fExtS, fogEProp, 0, fogEParamMax, "Extinction");

            if (EditorGUI.EndChangeCheck())
            {

                if (fogEProp.floatValue > fogEParamMax)
                {
                    fogEProp.floatValue = fogEParamMax;
                }
            }

            wasEnabled = GUI.enabled;
            if (falloff != DS_HazeCore.HeightFalloffType.Exponential) GUI.enabled = false;
            EditorGUI.PropertyField(fFalloff, property.FindPropertyRelative("m_FogDensityHeightFalloff"), new GUIContent("Height Falloff", strToolTipFogHeightFalloff));
            GUI.enabled = wasEnabled;

            EditorGUI.PropertyField(fDist, property.FindPropertyRelative("m_FogStartDistance"), new GUIContent("Start Distance", strToolTipFogStartDistance));
            EditorGUI.PropertyField(fDir, property.FindPropertyRelative("m_FogScatteringDirection"), new GUIContent("Scatter Direction", strToolTipFogScatterDirection));
            EditorGUI.PropertyField(fAmb, property.FindPropertyRelative("m_FogAmbientColour"), new GUIContent("Ambient Colour", strToolTipFogAmbCol));
            EditorGUI.PropertyField(fLight, property.FindPropertyRelative("m_FogLightColour"), new GUIContent("Light Colour", strToolTipFogLightCol));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + expandedHeight;
        }
    }
}
