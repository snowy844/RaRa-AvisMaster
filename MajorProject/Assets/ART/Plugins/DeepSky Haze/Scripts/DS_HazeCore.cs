using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeepSky.Haze
{
    [ExecuteInEditMode, AddComponentMenu("DeepSky Haze/Controller", 51)]
    public class DS_HazeCore : MonoBehaviour
    {
        public static string kVersionStr = "DeepSky Haze v1.3.3";
        private static int kGUIHeight = 180;

        public enum HeightFalloffType { Exponential, None };
        public enum NoiseTextureSize { x8 = 8, x16 = 16, x32 = 32 };
        public enum DebugGUIPosition { TopLeft, TopCenter, TopRight, CenterLeft, Center, CenterRight, BottomLeft, BottomCenter, BottomRight };

        private static DS_HazeCore instance;
        public static DS_HazeCore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DS_HazeCore>();
                }

                return instance;
            }
        }

        #region FIELDS
        [SerializeField, Range(0, 1), Tooltip("The time at which Zones will evaluate their settings. Animate this or set in code to create time-of-day transitions.")]
        private float m_Time = 0.0f;
        [SerializeField, Tooltip("The height falloff method to use globally (default Exponential).")]
        private HeightFalloffType m_HeightFalloff = HeightFalloffType.Exponential;
        [SerializeField]
        private List<DS_HazeZone> m_Zones = new List<DS_HazeZone>();
        [SerializeField]
        private DebugGUIPosition m_DebugGUIPosition = DebugGUIPosition.TopLeft;

        // Volumetric lights set.
        private HashSet<DS_HazeLightVolume> m_LightVolumes = new HashSet<DS_HazeLightVolume>();
        [SerializeField] private Texture3D m_NoiseLUT;

        // Editor and GUI.
        [SerializeField]
        private bool m_ShowDebugGUI = false;
        private Vector2 m_GUIScrollPosition;
        private int m_GUISelectedView = -1;
        private bool m_GUISelectionPopup = false;
        private DS_HazeView m_GUIDisplayedView = null;

        public float Time
        {
            get { return m_Time; }
            set { m_Time = Mathf.Clamp01(value); }
        }

        public Texture3D NoiseLUT
        {
            get { return m_NoiseLUT; }
        }

        public HeightFalloffType HeightFalloff
        {
            get { return m_HeightFalloff; }
            set {
                m_HeightFalloff = value;
                SetGlobalHeightFalloff();
            }
        }
        #endregion

        private void SetGlobalHeightFalloff()
        {
            switch (m_HeightFalloff)
            {
                case HeightFalloffType.Exponential:
                    Shader.DisableKeyword("DS_HAZE_HEIGHT_FALLOFF_NONE");
                    break;
                case HeightFalloffType.None:
                    Shader.EnableKeyword("DS_HAZE_HEIGHT_FALLOFF_NONE");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Rebuild the list whenever the children change. This doesn't happen very often
        /// so just clear the old structure and remake.
        /// Inactive zones are included, they will be ignored later during blending.
        /// </summary>
        void OnTransformChildrenChanged()
        {
            m_Zones.Clear();
            DS_HazeZone[] zones = GetComponentsInChildren<DS_HazeZone>(true);
            m_Zones.AddRange(zones);
        }

        /// <summary>
        /// On Awake we need to check there is only one Haze Controller in the scene and
        /// set up the singleton reference to it.
        /// </summary>
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogError("DeepSky::DS_HazeCore:Awake - There is more than one Haze Controller in this scene! Disabling " + name);
                enabled = false;
            }
        }

        void OnEnable()
        {
            SetGlobalHeightFalloff();

            Shader.SetGlobalTexture("_SamplingOffsets", m_NoiseLUT);
        }

        /// <summary>
        /// On resetting the component it will lose references to all the child Zone objects, so re-scan the heirarchy.
        /// </summary>
        void Reset()
        {
            OnTransformChildrenChanged();
        }

        /// <summary>
        /// Make the noise LUT available to shaders.
        /// </summary>
        public void SetGlobalNoiseLUT()
        {
            Shader.SetGlobalTexture("_SamplingOffsets", m_NoiseLUT);
        }

        /// <summary>
        /// Add a volumetric light component to render this frame.
        /// </summary>
        /// <param name="lightVolume"></param>
        public void AddLightVolume(DS_HazeLightVolume lightVolume)
        {
            RemoveLightVolume(lightVolume);
            m_LightVolumes.Add(lightVolume);
        }

        /// <summary>
        /// Remove a volumetric light component from rendering this frame.
        /// </summary>
        /// <param name="lightVolume"></param>
        public void RemoveLightVolume(DS_HazeLightVolume lightVolume)
        {
            m_LightVolumes.Remove(lightVolume);
        }

        /// <summary>
        /// Populate the passed in lists with all the light volumes that will be rendered from
        /// the given camera position (split depending on whether cast shadows or not).
        /// </summary>
        /// <param name="cameraPosition"></param>
        /// <param name="lightVolumes"></param>
        public void GetRenderLightVolumes(Vector3 cameraPosition, List<DS_HazeLightVolume> lightVolumes, List<DS_HazeLightVolume> shadowVolumes)
        {
            foreach (DS_HazeLightVolume lv in m_LightVolumes)
            {
                if (lv.WillRender(cameraPosition))
                {
                    if (lv.CastShadows)
                    {
                        shadowVolumes.Add(lv);
                    }
                    else
                    {
                        lightVolumes.Add(lv);
                    }
                }
            }
        }

        /// <summary>
        /// Find the zones containing the given position and blend between them.
        /// The render context will be null if the position is not within any zones.
        /// If there is only one zone, that will provide the context as-is.
        /// </summary>
        public DS_HazeContextItem GetRenderContextAtPosition(Vector3 position)
        {
            List<DS_HazeZone> blendZones = new List<DS_HazeZone>();
            for (int zi = 0; zi < m_Zones.Count; zi++)
            {
                if (m_Zones[zi].Contains(position) && m_Zones[zi].enabled)
                {
                    blendZones.Add(m_Zones[zi]);
                }
            }

            if (blendZones.Count == 0) { return null; }

            if (blendZones.Count == 1)
            {
                return blendZones[0].Context.GetContextItemBlended(m_Time);
            }

            blendZones.Sort(delegate (DS_HazeZone z1, DS_HazeZone z2)
            {
                if (z1 < z2) return -1;
                else return 1;
            });

            // With the list in priority order (lowest to highest), iterate through and blend from first to last.
            DS_HazeContextItem renderContext = blendZones[0].Context.GetContextItemBlended(m_Time);
            float weight = 0;
            for (int ci = 1; ci < blendZones.Count; ci++)
            {
                weight = blendZones[ci].GetBlendWeight(position);
                renderContext.Lerp(blendZones[ci].Context.GetContextItemBlended(m_Time), weight);
            }

            return renderContext;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!m_ShowDebugGUI) return;

            Rect guiPosition = new Rect(5, 5, 256, kGUIHeight);

            switch (m_DebugGUIPosition)
            {
                case DebugGUIPosition.TopCenter:
                    guiPosition.x = Screen.width / 2 - 128;
                    break;
                case DebugGUIPosition.TopRight:
                    guiPosition.x = Screen.width - 261;
                    break;
                case DebugGUIPosition.CenterLeft:
                    guiPosition.y = Screen.height / 2 - 64;
                    break;
                case DebugGUIPosition.Center:
                    guiPosition.x = Screen.width / 2 - 128;
                    guiPosition.y = Screen.height / 2 - 64;
                    break;
                case DebugGUIPosition.CenterRight:
                    guiPosition.x = Screen.width - 261;
                    guiPosition.y = Screen.height / 2 - 64;
                    break;
                case DebugGUIPosition.BottomLeft:
                    guiPosition.y = Screen.height - (kGUIHeight + 5);
                    break;
                case DebugGUIPosition.BottomCenter:
                    guiPosition.x = Screen.width / 2 - 128;
                    guiPosition.y = Screen.height - (kGUIHeight + 5);
                    break;
                case DebugGUIPosition.BottomRight:
                    guiPosition.x = Screen.width - 261;
                    guiPosition.y = Screen.height - (kGUIHeight + 5);
                    break;
                default: // DebugGUIPosition.TopLeft
                    break;
            }

            Rect dropDownPosition = guiPosition;
            dropDownPosition.y += 50;
            dropDownPosition.height -= 50;

            GUIStyle headerStyle = new GUIStyle();
            headerStyle.normal.textColor = Color.white;
            headerStyle.alignment = TextAnchor.UpperCenter;
            headerStyle.padding = new RectOffset(5, 5, 5, 5);
            headerStyle.fontStyle = FontStyle.Bold;

            GUIStyle currentViewStyle = new GUIStyle(GUI.skin.GetStyle("box"));
            currentViewStyle.fontSize = 12;
            currentViewStyle.alignment = TextAnchor.MiddleLeft;
            currentViewStyle.margin.left = currentViewStyle.margin.right = 0;

            GUIStyle dropDownButtonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
            dropDownButtonStyle.fontSize = 14;
            dropDownButtonStyle.alignment = TextAnchor.MiddleCenter;
            dropDownButtonStyle.normal.textColor = Color.grey;
            dropDownButtonStyle.hover.textColor = Color.white;
            dropDownButtonStyle.margin.left = dropDownButtonStyle.margin.right = 0;

            GUIStyle infoTextStyle = new GUIStyle(GUI.skin.GetStyle("label"));
            infoTextStyle.fontSize = 12;
            infoTextStyle.margin = new RectOffset(0, 0, 0, 0);
            infoTextStyle.padding = new RectOffset(2, 2, 2, 2);

            GUIStyle popupWindowStyle = new GUIStyle(GUI.skin.GetStyle("window"));
            popupWindowStyle.fontSize = 12;
            popupWindowStyle.alignment = TextAnchor.UpperLeft;

            GUI.Box(guiPosition, GUIContent.none);
            GUILayout.BeginArea(guiPosition, kVersionStr, headerStyle);
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Box(m_GUIDisplayedView != null ? m_GUIDisplayedView.gameObject.name : "(None)", currentViewStyle, GUILayout.Width(220));
            if (GUILayout.Button(m_GUISelectionPopup ? '\u25B2'.ToString() : '\u25BC'.ToString(), dropDownButtonStyle))
            {
                m_GUISelectionPopup = !m_GUISelectionPopup;
            }
            GUILayout.EndHorizontal();
            
            if (m_GUISelectionPopup)
            {
                GUILayout.Window(0, dropDownPosition, ViewSelectionPopup, "Select A View", popupWindowStyle);
            }

            if (!m_GUISelectionPopup)
            {
                if (m_GUIDisplayedView == null) GUI.enabled = false;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Volume samples", infoTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_GUIDisplayedView != null ? m_GUIDisplayedView.SampleCount.ToString() : "-", infoTextStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Atmosphere volumetrics", infoTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_GUIDisplayedView != null ? m_GUIDisplayedView.RenderAtmosphereVolumetrics.ToString() : "-", infoTextStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Local volumetrics", infoTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_GUIDisplayedView != null ? m_GUIDisplayedView.RenderLocalVolumetrics.ToString() : "-", infoTextStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Scaling factor", infoTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_GUIDisplayedView != null ? "1/" + m_GUIDisplayedView.DownSampleFactor.ToString() : "-", infoTextStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Uses temporal reprojection", infoTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_GUIDisplayedView != null ? m_GUIDisplayedView.WillRenderWithTemporalReprojection.ToString() : "-", infoTextStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Render target size", infoTextStyle);
                GUILayout.FlexibleSpace();
                string rtSize = "-";
                if (m_GUIDisplayedView != null)
                {
                    Vector2 rts = m_GUIDisplayedView.RadianceTargetSize;
                    rtSize = rts.x.ToString() + "x" + rts.y.ToString();
                }
                GUILayout.Label(rtSize, infoTextStyle);
                GUILayout.EndHorizontal();

                GUI.enabled = true;
            }
            GUILayout.EndArea();
        }

        void ViewSelectionPopup(int id)
        {
            GUIStyle elementStyle = new GUIStyle(GUI.skin.GetStyle("button"));
            elementStyle.fontSize = 12;
            elementStyle.normal.textColor = Color.grey;
            elementStyle.hover.textColor = Color.white;
            elementStyle.alignment = TextAnchor.MiddleLeft;
            elementStyle.margin = new RectOffset(0, 0, 0, 0);

            DS_HazeView[] views = FindObjectsOfType<DS_HazeView>();

            m_GUIScrollPosition = GUILayout.BeginScrollView(m_GUIScrollPosition);
            string[] viewNames = views.Select(v => v.gameObject.name).ToArray();

            int select = GUILayout.SelectionGrid(m_GUISelectedView < viewNames.Length ? m_GUISelectedView : 0, viewNames, 1, elementStyle);
            if (select != m_GUISelectedView)
            {
                m_GUISelectedView = select;
                m_GUIDisplayedView = views[m_GUISelectedView];
                m_GUISelectionPopup = false;
            }
            GUILayout.EndScrollView();
        }
#endif
    }
}
