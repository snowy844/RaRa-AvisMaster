using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace DeepSky.Haze
{
    [ExecuteInEditMode, AddComponentMenu("DeepSky Haze/View", 1)]
    public class DS_HazeView : MonoBehaviour
    {
        private enum SizeFactor { Half = 2, Quarter = 4 };
        private enum VolumeSamples { x16, x24, x32 };

        private static string kClearRadianceCmdBufferName = "DS_Haze_ClearRadiance";
        private static string kShadowCascadesCmdBufferName = "DS_Haze_ShadowCascadesCopy";
        private static string kDirectionalLightCmdBufferName = "DS_Haze_DirectLight";
        private static string kRenderLightVolumeCmdBufferName = "DS_Haze_RenderLightVolume";
        private static string kPreviousDepthTargetName = "DS_Haze_PreviousDepthTarget";
        private static string kRadianceTarget01Name = "DS_Haze_RadianceTarget_01";
        private static string kRadianceTarget02Name = "DS_Haze_RadianceTarget_02";
        private static Shader kShader;

        [SerializeField]
        private bool m_OverrideTime = false;
        [SerializeField, Range(0, 1)]
        private float m_Time = 0.5f;
        [SerializeField]
        private bool m_OverrideContextAsset = false;
        [SerializeField]
        private DS_HazeContextAsset m_Context;
        [SerializeField]
        private bool m_OverrideContextVariant = false;
        [SerializeField]
        private int m_ContextItemIndex = 0;
        [SerializeField]
        private Light m_DirectLight;
        [SerializeField]
        private bool m_RenderAtmosphereVolumetrics = true;
        [SerializeField]
        private bool m_RenderLocalVolumetrics = true;
        [SerializeField]
        private bool m_TemporalReprojection = true;
        [SerializeField]
        private SizeFactor m_DownsampleFactor = SizeFactor.Half;
        [SerializeField]
        private VolumeSamples m_VolumeSamples = VolumeSamples.x16;

        // Shader params.
        [SerializeField, Range(100, 5000)]
        private int m_GaussianDepthFalloff = 500;
        [SerializeField, Range(0, 0.5f)]
        private float m_UpsampleDepthThreshold = 0.06f;
        [SerializeField, Range(0.001f, 1.0f)]
        private float m_TemporalRejectionScale = 0.1f;
        [SerializeField, Range(0.1f, 0.9f)]
        private float m_TemporalBlendFactor = 0.25f;

        // Shader keywords.
        private ShadowProjection m_ShadowProjectionType = ShadowProjection.StableFit;
        [SerializeField]
        private bool m_ApplyAirToSkybox = false;
        [SerializeField]
        private bool m_ApplyHazeToSkybox = true;
        [SerializeField]
        private bool m_ApplyFogExtinctionToSkybox = true;
        [SerializeField]
        private bool m_ApplyFogLightingToSkybox = true;
        [SerializeField]
        private bool m_ShowTemporalRejection = false;
        [SerializeField]
        private bool m_ShowUpsampleThreshold = false;

        // Non-serialized fields.
        private Camera m_Camera;
        private RenderTexture m_PerFrameRadianceTarget;
        private RenderTexture m_RadianceTarget_01;
        private RenderTexture m_RadianceTarget_02;
        private RenderTexture m_CurrentRadianceTarget;
        private RenderTexture m_PreviousRadianceTarget;
        private RenderTexture m_PreviousDepthTarget;
        private CommandBuffer m_ShadowCascadesCmdBuffer;
        private CommandBuffer m_DirectionalLightCmdBuffer;
        private CommandBuffer m_ClearRadianceCmdBuffer;
        private CommandBuffer m_RenderNonShadowVolumes;
        private Material m_Material;
        private Matrix4x4 m_PreviousViewProjMatrix = Matrix4x4.identity;
        private Matrix4x4 m_PreviousInvViewProjMatrix = Matrix4x4.identity;
        private float m_InterleavedOffsetIndex = 0f;
        private int m_X;
        private int m_Y;
        private RenderingPath m_PreviousRenderPath;
        private ColorSpace m_ColourSpace;

        private List<DS_HazeLightVolume> m_PerFrameLightVolumes = new List<DS_HazeLightVolume>();
        private List<DS_HazeLightVolume> m_PerFrameShadowLightVolumes = new List<DS_HazeLightVolume>();
        private Dictionary<Light, CommandBuffer> m_LightVolumeCmdBuffers = new Dictionary<Light, CommandBuffer>();

        // Public Accessors.
        public bool OverrideTime
        {
            get { return m_OverrideTime; }
            set
            {
                m_OverrideTime = value;
                if (value && m_OverrideContextVariant)
                {
                    // Time and Variant overrides can't both be true.
                    m_OverrideContextVariant = false;
                }
            }
        }

        public float Time
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        public bool OverrideContextAsset
        {
            get { return m_OverrideContextAsset; }
            set { m_OverrideContextAsset = value; }
        }

        public DS_HazeContextAsset ContextAsset
        {
            get { return m_Context; }
            set { m_Context = value; }
        }

        public bool OverrideContextVariant
        {
            get { return m_OverrideContextVariant; }
            set
            {
                m_OverrideContextVariant = value;
                if (value && m_OverrideTime)
                {
                    // Time and Variant overrides can't both be true.
                    m_OverrideTime = false;
                }
            }
        }

        public int ContextItemIndex
        {
            get { return m_ContextItemIndex; }
            set { m_ContextItemIndex = value > 0 ? value : 0; }
        }

        public Light DirectLight
        {
            get { return m_DirectLight; }
            set { m_DirectLight = value; }
        }

        public Vector2 RadianceTargetSize
        {
            get { return new Vector2(m_X, m_Y); }
        }

        public int SampleCount
        {
            get {
                switch(m_VolumeSamples)
                {
                    case VolumeSamples.x16:
                        return 16;
                    case VolumeSamples.x24:
                        return 24;
                    case VolumeSamples.x32:
                        return 32;
                    default:
                        return 16;
                }
            }
        }

        public int DownSampleFactor
        {
            get { return m_DownsampleFactor == SizeFactor.Half ? (int)SizeFactor.Half : (int)SizeFactor.Quarter; }
        }

        public bool RenderAtmosphereVolumetrics
        {
            get { return m_RenderAtmosphereVolumetrics; }
            set
            {
                m_RenderAtmosphereVolumetrics = value;
                SetTemporalKeywords();
            }
        }

        public bool RenderLocalVolumetrics
        {
            get { return m_RenderLocalVolumetrics; }
            set
            {
                m_RenderLocalVolumetrics = value;
                SetTemporalKeywords();
            }
        }

        public bool TemporalReprojection
        {
            get { return m_TemporalReprojection; }
            set
            {
                m_TemporalReprojection = value;
                SetTemporalKeywords();
            }
        }

        public bool WillRenderWithTemporalReprojection
        {
            get { return m_TemporalReprojection & (m_RenderAtmosphereVolumetrics | m_RenderLocalVolumetrics); }
        }

        public int AntiAliasingLevel()
        {
            int aa = 1;
#if UNITY_5_6_OR_NEWER // MSAA and float targets allowed, Camera.allowMSAA exists.
            if (m_Camera.actualRenderingPath == RenderingPath.Forward && m_Camera.allowMSAA && QualitySettings.antiAliasing > 0)
#elif UNITY_5_5 // MSAA and float targets allowed.
            if (m_Camera.actualRenderingPath == RenderingPath.Forward && QualitySettings.antiAliasing > 0)
#else // MSAA and float targets NOT allowed.
            if (m_Camera.actualRenderingPath != RenderingPath.Forward && QualitySettings.antiAliasing > 0)
#endif
            {
                aa = QualitySettings.antiAliasing;
            }
            return aa;
        }

        /// <summary>
        /// Does this system support the shader model and render texture formats required?
        /// </summary>
        /// <returns></returns>
        private bool CheckHasSystemSupport()
        {
#if !UNITY_5_5_OR_NEWER
            if (QualitySettings.antiAliasing != 0 && m_Camera.actualRenderingPath == RenderingPath.Forward)
            {
                Debug.LogError("DeepSky::DS_HazeView: Unity versions before 5.5 do not support float render targets when using Forward rendering with MSAA. Please disable MSAA or use Deferred.");
                enabled = false;
                return false;
            }
#endif

            if (!SystemInfo.supportsImageEffects)
            {
                Debug.LogError("DeepSky::DS_HazeView: Image effects are not supported on this platform.");
                enabled = false;
                return false;
            }

#if !UNITY_5_5_OR_NEWER
            if (!SystemInfo.supportsRenderTextures)
            {
                Debug.LogError("DeepSky::DS_HazeView: Render Textures are not supported on this platform.");
                enabled = false;
                return false;
            }
#endif

            if (SystemInfo.graphicsShaderLevel < 30)
            {
                Debug.LogError("DeepSky::DS_HazeView: Minimum required shader model (3.0) is not supported on this platform.");
                enabled = false;
                return false;
            }

#if UNITY_5_6_OR_NEWER
            if (m_Camera.allowHDR)
#else
            if (m_Camera.hdr)
#endif
            {
                if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                {
                    Debug.LogError("DeepSky::DS_HazeView: ARGBHalf render texture format is not supported on this platform.");
                    enabled = false;
                    return false;
                }
            }

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat))
            {
                Debug.LogError("DeepSky::DS_HazeView: RFloat render texture format is not supported on this platform.");
                enabled = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set all material parameters from a context item, ready for rendering.
        /// The Rayleigh and Mie scattering coefficients are derived from 'Precomputed 
        /// Atmospheric Scattering (Bruneton, Neyret 2008)' - https://hal.inria.fr/inria-00288758/file/article.pdf
        /// The original Rayleigh values have been scaled up to make it easier to work with
        /// the typically shorter view distances in a game environment.
        /// </summary>
        /// <param name="ctx"> The context item to get settings from. </param>
        private void SetMaterialFromContext(DS_HazeContextItem ctx)
        {
            if (WillRenderWithTemporalReprojection)
            {
                m_InterleavedOffsetIndex += 0.0625f;
                if (Mathf.Approximately(m_InterleavedOffsetIndex, 1))
                {
                	m_InterleavedOffsetIndex = 0;
            	}
            }

            float atmosViewpointDensity = 1.0f;
            float hazeViewpointDensity = 1.0f;
            float fogViewpointDensity = 1.0f;

            switch (DS_HazeCore.Instance.HeightFalloff)
            {
                case DS_HazeCore.HeightFalloffType.None:
                    atmosViewpointDensity = 1.0f;
                    hazeViewpointDensity = 1.0f;
                    fogViewpointDensity = 1.0f;
                    break;
                case DS_HazeCore.HeightFalloffType.Exponential:
                    float absY = Mathf.Abs(transform.position.y);
                    atmosViewpointDensity = Mathf.Exp(-ctx.m_AirDensityHeightFalloff * absY);
                    hazeViewpointDensity = Mathf.Exp(-ctx.m_HazeDensityHeightFalloff * absY);
                    fogViewpointDensity = Mathf.Exp(-ctx.m_FogDensityHeightFalloff * absY);
                    break;
                default:
                    break;
            }

            Vector3 kRBetaS = ctx.m_AirScatteringScale * new Vector3(0.00116f, 0.0027f, 0.00662f); //<--- (.0000058, .0000135, .0000331) * 200
            float kMBetaS = ctx.m_HazeScatteringScale * 0.0021f;
            float kFBetaS = ctx.m_FogScatteringScale;
            float kFBetaE = ctx.m_FogExtinctionScale * 0.01f;

            Vector4 airHazeParams = new Vector4(ctx.m_AirDensityHeightFalloff, ctx.m_HazeDensityHeightFalloff, 0, ctx.m_HazeScatteringDirection);
            Vector4 betaParams = new Vector4(kMBetaS, m_RenderAtmosphereVolumetrics ? ctx.m_HazeSecondaryScatteringRatio : 0, kFBetaS, kFBetaE);
            Vector4 initialDensityParams = new Vector4(atmosViewpointDensity, hazeViewpointDensity, fogViewpointDensity, 0);
            Vector4 fogParams = new Vector4(ctx.m_FogStartDistance, ctx.m_FogDensityHeightFalloff, ctx.m_FogOpacity, ctx.m_FogScatteringDirection);
            Vector4 samplingParams = new Vector4(m_GaussianDepthFalloff, m_UpsampleDepthThreshold * 0.01f, m_TemporalRejectionScale, m_TemporalBlendFactor);

            m_Material.SetVector("_SamplingParams", samplingParams);
            //m_Material.SetVector("_InterleavedOffset", new Vector4(DS_HazeCore.kOffsetSequence[m_InterleavedOffsetIndex].x, DS_HazeCore.kOffsetSequence[m_InterleavedOffsetIndex].y, 0, 0));
            m_Material.SetVector("_InterleavedOffset", new Vector4(m_InterleavedOffsetIndex, 0, 0, 0));
            m_Material.SetMatrix("_PreviousViewProjMatrix", m_PreviousViewProjMatrix);
            m_Material.SetMatrix("_PreviousInvViewProjMatrix", m_PreviousInvViewProjMatrix);

            Shader.SetGlobalVector("_DS_BetaParams", betaParams);
            Shader.SetGlobalVector("_DS_RBetaS", kRBetaS);
            Shader.SetGlobalVector("_DS_AirHazeParams", airHazeParams);
            Shader.SetGlobalVector("_DS_FogParams", fogParams);
            Shader.SetGlobalVector("_DS_InitialDensityParams", initialDensityParams);

            // Update material direct light values (if no directional light supplied, light is white
            // and directly from above).
            Vector3 direction;
            Color lightColour;
            if (m_DirectLight)
            {
                direction = -m_DirectLight.transform.forward;
                lightColour = m_DirectLight.color.linear * m_DirectLight.intensity;

                Shader.SetGlobalColor("_DS_FogAmbientLight", ctx.m_FogAmbientColour.linear * m_DirectLight.intensity);
                Shader.SetGlobalColor("_DS_FogDirectLight", ctx.m_FogLightColour.linear * m_DirectLight.intensity);
            }
            else
            {
                direction = Vector3.up;
                lightColour = Color.white;

                Shader.SetGlobalColor("_DS_FogAmbientLight", ctx.m_FogAmbientColour.linear);
                Shader.SetGlobalColor("_DS_FogDirectLight", ctx.m_FogLightColour.linear);
            }
            Shader.SetGlobalVector("_DS_LightDirection", direction);
            Shader.SetGlobalVector("_DS_LightColour", lightColour);
        }

        /// <summary>
        /// Zero the scattering parameters to effectively disable scattering.
        /// If there are multiple cameras in the scene but this camera is outside any zones,
        /// transparent objects will still pick up the global shader properties set by any
        /// camera in a zone - which will almost certainly be wrong.
        /// </summary>
        private void SetGlobalParamsToNull()
        {
            Shader.SetGlobalVector("_DS_BetaParams", Vector4.zero);
            Shader.SetGlobalVector("_DS_RBetaS", Vector4.zero);
        }

        /// <summary>
        /// Enable the keywords for debugging the upsample and reprojection thresholds.
        /// </summary>
        public void SetDebugKeywords()
        {
            if (m_ShowTemporalRejection)
            {
                m_Material.EnableKeyword("SHOW_TEMPORAL_REJECTION");
            }
            else
            {
                m_Material.DisableKeyword("SHOW_TEMPORAL_REJECTION");
            }

            if (m_ShowUpsampleThreshold)
            {
                m_Material.EnableKeyword("SHOW_UPSAMPLE_THRESHOLD");
            }
            else
            {
                m_Material.DisableKeyword("SHOW_UPSAMPLE_THRESHOLD");
            }
        }

        /// <summary>
        /// Configure how the shader should apply atmospheric effects to the skybox. Skybox is considered to be
        /// anything with a depth greater than 0.9999.
        /// </summary>
        public void SetSkyboxKeywords()
        {
            if (m_ApplyAirToSkybox)
            {
                m_Material.EnableKeyword("DS_HAZE_APPLY_RAYLEIGH");
            }
            else
            {
                m_Material.DisableKeyword("DS_HAZE_APPLY_RAYLEIGH");
            }

            if (m_ApplyHazeToSkybox)
            {
                m_Material.EnableKeyword("DS_HAZE_APPLY_MIE");
            }
            else
            {
                m_Material.DisableKeyword("DS_HAZE_APPLY_MIE");
            }

            if (m_ApplyFogExtinctionToSkybox)
            {
                m_Material.EnableKeyword("DS_HAZE_APPLY_FOG_EXTINCTION");
            }
            else
            {
                m_Material.DisableKeyword("DS_HAZE_APPLY_FOG_EXTINCTION");
            }

            if (m_ApplyFogLightingToSkybox)
            {
                m_Material.EnableKeyword("DS_HAZE_APPLY_FOG_RADIANCE");
            }
            else
            {
                m_Material.DisableKeyword("DS_HAZE_APPLY_FOG_RADIANCE");
            }
        }

        public void SetTemporalKeywords()
        {
            // Always disable temporal reprojection if no volumetrics are actually being rendered.
            if (WillRenderWithTemporalReprojection)
            {
                // UpdateResources will take care of creating the render targets, just need to enable the shader keyword.
                m_Material.EnableKeyword("DS_HAZE_TEMPORAL");
            }
            else
            {
                // If disabling temporal reprojection, need to clean up the unused render targets as well.
                m_Material.DisableKeyword("DS_HAZE_TEMPORAL");

                if (m_ShowTemporalRejection)
                {
                    m_ShowTemporalRejection = false;
                    m_Material.DisableKeyword("SHOW_TEMPORAL_REJECTION");
                }

                if (m_RadianceTarget_01)
                {
                    m_RadianceTarget_01.Release();
                    DestroyImmediate(m_RadianceTarget_01);
                    m_RadianceTarget_01 = null;
                }
                if (m_RadianceTarget_02)
                {
                    m_RadianceTarget_02.Release();
                    DestroyImmediate(m_RadianceTarget_02);
                    m_RadianceTarget_02 = null;
                }
                if (m_PreviousDepthTarget)
                {
                    m_PreviousDepthTarget.Release();
                    DestroyImmediate(m_PreviousDepthTarget);
                    m_PreviousDepthTarget = null;
                }
            }
        }

        /// <summary>
        /// Set the various keywords on the material to control shader variant being used.
        /// </summary>
        private void SetShaderKeyWords()
        {
            if (m_ShadowProjectionType == ShadowProjection.CloseFit)
            {
                m_Material.EnableKeyword("SHADOW_PROJ_CLOSE");
            }
            else if (m_ShadowProjectionType == ShadowProjection.StableFit)
            {
                m_Material.DisableKeyword("SHADOW_PROJ_CLOSE");
            }

            if (DS_HazeCore.Instance != null)
            {
                switch (DS_HazeCore.Instance.HeightFalloff)
                {
                    case DS_HazeCore.HeightFalloffType.None:
                        m_Material.EnableKeyword("DS_HAZE_HEIGHT_FALLOFF_NONE");
                        break;
                    case DS_HazeCore.HeightFalloffType.Exponential:
                        m_Material.DisableKeyword("DS_HAZE_HEIGHT_FALLOFF_NONE");
                        break;
                    default:
                        m_Material.EnableKeyword("DS_HAZE_HEIGHT_FALLOFF_NONE");
                        break;
                }
            }
        }
        
        /// <summary>
        /// Check this GO actually has a camera component (and renders depth) and system support for the effect.
        /// If there are already command buffers available (this effect has already been added but then disabled)
        /// then hook them up again to the directional light and camera.
        /// </summary>
        void OnEnable()
        {
            SetGlobalParamsToNull();

            m_Camera = GetComponent<Camera>();

            if (!m_Camera)
            {
                Debug.LogError("DeepSky::DS_HazeView: GameObject '" + gameObject.name + "' does not have a camera component!");
                enabled = false;
                return;
            }

            if (!CheckHasSystemSupport())
            {
                enabled = false;
                return;
            }

            if (kShader == null)
            {
                kShader = Resources.Load<Shader>("DS_Haze");
            }

            if (m_Material == null)
            {
                m_Material = new Material(kShader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_Camera.actualRenderingPath == RenderingPath.Forward && (m_Camera.depthTextureMode & DepthTextureMode.Depth) != DepthTextureMode.Depth)
            {
                m_Camera.depthTextureMode = m_Camera.depthTextureMode | DepthTextureMode.Depth;
            }

            if (m_RenderNonShadowVolumes == null)
            {
                CommandBuffer[] cmds = m_Camera.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
                bool found = false;
                foreach (CommandBuffer cmd in cmds)
                {
                    if (cmd.name == kRenderLightVolumeCmdBufferName)
                    {
                        m_RenderNonShadowVolumes = cmd;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    m_RenderNonShadowVolumes = new CommandBuffer();
                    m_RenderNonShadowVolumes.name = kRenderLightVolumeCmdBufferName;
                    m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_RenderNonShadowVolumes);
                }
            }

            m_CurrentRadianceTarget = m_RadianceTarget_01;
            m_PreviousRadianceTarget = m_RadianceTarget_02;

            SetSkyboxKeywords();
            SetDebugKeywords();

            m_ColourSpace = QualitySettings.activeColorSpace;
            m_PreviousRenderPath = m_Camera.actualRenderingPath;
        }

        /// <summary>
        /// Create a render target to hold the radiance accumulation. Takes care of format differences when rendering in HDR.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="radianceTarget"></param>
        private void CreateRadianceTarget(string name, out RenderTexture radianceTarget)
        {
#if UNITY_5_6_OR_NEWER
            if (m_Camera.allowHDR)
#else
            if (m_Camera.hdr)
#endif
            {
                radianceTarget = new RenderTexture(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf);
            }
            else
            {
                radianceTarget = new RenderTexture(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGB32);
            }

            radianceTarget.name = name;
#if UNITY_5_5_OR_NEWER
            radianceTarget.antiAliasing = AntiAliasingLevel();
#else
            radianceTarget.antiAliasing = 1;
#endif
            radianceTarget.useMipMap = false;
            radianceTarget.hideFlags = HideFlags.HideAndDontSave;
            radianceTarget.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// Create a render target to use for storing depth.
        /// </summary>
        /// <param name="name"> The render target's name.</param>
        /// <param name="depthTarget"> The render target object to fill.</param>
        /// <param name="downsample"> Should it be a smaller size (used when performing the depth down-sample)?</param>
        private void CreateDepthTarget(string name, out RenderTexture depthTarget, bool downsample = false)
        {
            depthTarget = new RenderTexture(downsample ? m_X : m_Camera.pixelWidth, downsample ? m_Y : m_Camera.pixelHeight, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            depthTarget.name = name;
            depthTarget.antiAliasing = 1;
            depthTarget.useMipMap = false;
            depthTarget.hideFlags = HideFlags.HideAndDontSave;
            depthTarget.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// Does the camera have the command buffer used to clear the per-frame radiance target?
        /// </summary>
        /// <param name="foundCmd"> The existing command buffer to use </param>
        /// <returns></returns>
        private bool CameraHasClearRadianceCmdBuffer(out CommandBuffer foundCmd)
        {
            CommandBuffer[] cmdBuffers;

            if (m_Camera.actualRenderingPath == RenderingPath.DeferredShading)
            {
                cmdBuffers = m_Camera.GetCommandBuffers(CameraEvent.BeforeGBuffer);
            }
            else
            {
                CameraEvent camEv = (m_Camera.depthTextureMode & DepthTextureMode.DepthNormals) == DepthTextureMode.DepthNormals ? CameraEvent.BeforeDepthNormalsTexture : CameraEvent.BeforeDepthTexture;
                cmdBuffers = m_Camera.GetCommandBuffers(camEv);
            }

            foreach (CommandBuffer cmd in cmdBuffers)
            {
                if (cmd.name == kClearRadianceCmdBufferName)
                {
                    foundCmd = cmd;
                    return true;
                }
            }

            foundCmd = null;
            return false;
        }

        /// <summary>
        /// Does the directional light have the command buffer to get a handle to shadow cascades?
        /// </summary>
        /// <returns></returns>
        private CommandBuffer LightHasCascadesCopyCmdBuffer()
        {
            CommandBuffer[] cmds = m_DirectLight.GetCommandBuffers(LightEvent.AfterShadowMap);
            foreach (CommandBuffer cmd in cmds)
            {
                if (cmd.name == kShadowCascadesCmdBufferName)
                {
                    return cmd;
                }
            }
            return null;
        }

        /// <summary>
        /// Does the directional light have the command buffer to render volumetrics?
        /// </summary>
        /// <returns></returns>
        private CommandBuffer LightHasRenderCmdBuffer()
        {
            CommandBuffer[] cmds = m_DirectLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask);
            foreach (CommandBuffer cmd in cmds)
            {
                if (cmd.name == kDirectionalLightCmdBufferName)
                {
                    return cmd;
                }
            }
            return null;
        }

        /// <summary>
        /// Remove the shadow grab command buffer from a light. Used when changing the light
        /// reference in the editor to ensure command buffers are properly removed.
        /// </summary>
        /// <param name="light"> The light source to remove the command buffer from. </param>
        public void RemoveCommandBufferFromLight(Light light)
        {
            CommandBuffer[] cmds = light.GetCommandBuffers(LightEvent.AfterShadowMap);
            for (int cmd = 0; cmd < cmds.Length; cmd++)
            {
                if (cmds[cmd].name == kShadowCascadesCmdBufferName)
                {
                    light.RemoveCommandBuffer(LightEvent.AfterShadowMap, cmds[cmd]);
                    break;
                }
            }

            cmds = light.GetCommandBuffers(LightEvent.AfterScreenspaceMask);
            for (int cmd = 0; cmd < cmds.Length; cmd++)
            {
                if (cmds[cmd].name == kDirectionalLightCmdBufferName)
                {
                    light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmds[cmd]);
                    break;
                }
            }
        }

        /// <summary>
        /// When switching between forward and deferred, we need to make sure the camera is rendering depth and the command buffer to
        /// clear the radiance render target is in the correct place.
        /// </summary>
        private void RenderPathChanged()
        {
            if (m_Camera.actualRenderingPath == RenderingPath.Forward && (m_Camera.depthTextureMode & DepthTextureMode.Depth) != DepthTextureMode.Depth)
            {
                m_Camera.depthTextureMode = m_Camera.depthTextureMode | DepthTextureMode.Depth;
            }

            if (m_ClearRadianceCmdBuffer != null)
            {
                CameraEvent camEv;
                if (m_PreviousRenderPath == RenderingPath.DeferredShading)
                {
                    camEv = CameraEvent.BeforeGBuffer;
                }
                else
                {
                    camEv = (m_Camera.depthTextureMode & DepthTextureMode.DepthNormals) == DepthTextureMode.DepthNormals ? CameraEvent.BeforeDepthNormalsTexture : CameraEvent.BeforeDepthTexture;
                }
                CommandBuffer[] cmdBuffers = m_Camera.GetCommandBuffers(camEv);

                foreach (CommandBuffer cmd in cmdBuffers)
                {
                    if (cmd.name == kClearRadianceCmdBufferName)
                    {
                        m_Camera.RemoveCommandBuffer(camEv, cmd);
                        break;
                    }
                }
            }
            m_PreviousRenderPath = m_Camera.actualRenderingPath;
        }

        /// <summary>
        /// Check everything needed to render the effects is available, render targets are the right size and format and
        /// command buffers exist and are bound (although most won't be filled until OnPreRender).
        /// This takes care of resizing the frame, changing render path and colourspace.
        /// </summary>
        void UpdateResources()
        {
            m_X = m_Camera.pixelWidth / (int)m_DownsampleFactor;
            m_Y = m_Camera.pixelHeight / (int)m_DownsampleFactor;

            // If the rendering path has changed, need to clean up the existing command buffers first.
            if (m_Camera.actualRenderingPath != m_PreviousRenderPath)
            {
                RenderPathChanged();
            }

            // Expected radiance target format and colorspace.
#if UNITY_5_6_OR_NEWER
            RenderTextureFormat rtFormat = m_Camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#else
            RenderTextureFormat rtFormat = m_Camera.hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif
            bool colorSpaceChanged = m_ColourSpace != QualitySettings.activeColorSpace;
            m_ColourSpace = QualitySettings.activeColorSpace;

            if (WillRenderWithTemporalReprojection)
            {
                if (m_RadianceTarget_01 == null)
                {
                    CreateRadianceTarget(kRadianceTarget01Name, out m_RadianceTarget_01);
                    m_CurrentRadianceTarget = m_RadianceTarget_01;
                }
                else
                {
                    if (colorSpaceChanged || m_RadianceTarget_01.width != m_Camera.pixelWidth || m_RadianceTarget_01.height != m_Camera.pixelHeight || m_RadianceTarget_01.format != rtFormat)
                    {
                        DestroyImmediate(m_RadianceTarget_01);
                        CreateRadianceTarget(kRadianceTarget01Name, out m_RadianceTarget_01);
                        m_CurrentRadianceTarget = m_RadianceTarget_01;
                    }
                }

                if (m_RadianceTarget_02 == null)
                {
                    CreateRadianceTarget(kRadianceTarget02Name, out m_RadianceTarget_02);
                    m_PreviousRadianceTarget = m_RadianceTarget_02;
                }
                else
                {
                    if (colorSpaceChanged || m_RadianceTarget_02.width != m_Camera.pixelWidth || m_RadianceTarget_02.height != m_Camera.pixelHeight || m_RadianceTarget_02.format != rtFormat)
                    {
                        DestroyImmediate(m_RadianceTarget_02);
                        CreateRadianceTarget(kRadianceTarget02Name, out m_RadianceTarget_02);
                        m_PreviousRadianceTarget = m_RadianceTarget_02;
                    }
                }

                if (m_PreviousDepthTarget == null)
                {
                    CreateDepthTarget(kPreviousDepthTargetName, out m_PreviousDepthTarget);
                }
                else
                {
                    if (m_PreviousDepthTarget.width != m_Camera.pixelWidth || m_PreviousDepthTarget.height != m_Camera.pixelHeight)
                    {
                        DestroyImmediate(m_PreviousDepthTarget);
                        CreateDepthTarget(kPreviousDepthTargetName, out m_PreviousDepthTarget);
                    }
                }
            }

            if (m_ClearRadianceCmdBuffer == null)
            {
                m_ClearRadianceCmdBuffer = new CommandBuffer();
                m_ClearRadianceCmdBuffer.name = kClearRadianceCmdBufferName;
            }

            CameraEvent cv;
            if (m_Camera.actualRenderingPath == RenderingPath.DeferredShading)
            {
                cv = CameraEvent.BeforeGBuffer;
            }
            else
            {
                cv = (m_Camera.depthTextureMode & DepthTextureMode.DepthNormals) == DepthTextureMode.DepthNormals ? CameraEvent.BeforeDepthNormalsTexture : CameraEvent.BeforeDepthTexture;
            }

            CommandBuffer existing;
            if (!CameraHasClearRadianceCmdBuffer(out existing))
            {
                m_Camera.AddCommandBuffer(cv, m_ClearRadianceCmdBuffer);
            }
            else
            {
                if (existing != m_ClearRadianceCmdBuffer)
                {
                    m_Camera.RemoveCommandBuffer(cv, existing);
                    existing.Dispose();
                    m_Camera.AddCommandBuffer(cv, m_ClearRadianceCmdBuffer);
                }
            }

            if (m_DirectLight)
            {
                m_ShadowCascadesCmdBuffer = LightHasCascadesCopyCmdBuffer();
                if (m_ShadowCascadesCmdBuffer == null)
                {
                    m_ShadowCascadesCmdBuffer = new CommandBuffer();
                    m_ShadowCascadesCmdBuffer.name = kShadowCascadesCmdBufferName;
                    m_ShadowCascadesCmdBuffer.SetGlobalTexture("_ShadowCascades", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
                    m_DirectLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_ShadowCascadesCmdBuffer);
                }


                m_DirectionalLightCmdBuffer = LightHasRenderCmdBuffer();
                if (m_DirectionalLightCmdBuffer == null)
                {
                        m_DirectionalLightCmdBuffer = new CommandBuffer();
                        m_DirectionalLightCmdBuffer.name = kDirectionalLightCmdBufferName;
                        m_DirectLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, m_DirectionalLightCmdBuffer);
                }

                if (m_ShadowProjectionType != QualitySettings.shadowProjection)
                {
                    m_ShadowProjectionType = QualitySettings.shadowProjection;
                }
            }
        }

        /// <summary>
        /// Remove command buffers from lights and this camera to prevent effect rendering.
        /// </summary>
        void OnDisable()
        {
            SetGlobalParamsToNull();

            CommandBuffer[] cmdBuffers = m_Camera.GetCommandBuffers(CameraEvent.AfterSkybox);

            CameraEvent cv;
            if (m_Camera.actualRenderingPath == RenderingPath.DeferredShading)
            {
                cv = CameraEvent.BeforeGBuffer;
            }
            else
            {
                cv = (m_Camera.depthTextureMode & DepthTextureMode.DepthNormals) == DepthTextureMode.DepthNormals ? CameraEvent.BeforeDepthNormalsTexture : CameraEvent.BeforeDepthTexture;
            }
            cmdBuffers = m_Camera.GetCommandBuffers(cv);

            foreach (CommandBuffer cmd in cmdBuffers)
            {
                if (cmd.name == kClearRadianceCmdBufferName)
                {
                    m_Camera.RemoveCommandBuffer(cv, cmd);
                    break;
                }
            }

            if (m_DirectLight)
            {
                cmdBuffers = m_DirectLight.GetCommandBuffers(LightEvent.AfterShadowMap);
                foreach(CommandBuffer cmd in cmdBuffers)
                {
                    if (cmd.name == kShadowCascadesCmdBufferName)
                    {
                        m_DirectLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, cmd);
                        break;
                    }
                }

                cmdBuffers = m_DirectLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask);
                foreach (CommandBuffer cmd in cmdBuffers)
                {
                    if (cmd.name == kDirectionalLightCmdBufferName)
                    {
                        m_DirectLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
                        break;
                    }
                }
            }

            if (m_LightVolumeCmdBuffers.Count > 0)
            {
                foreach (KeyValuePair<Light, CommandBuffer> entry in m_LightVolumeCmdBuffers)
                {
                    entry.Key.RemoveCommandBuffer(LightEvent.AfterShadowMap, entry.Value);
                    entry.Value.Dispose();
                }

                m_LightVolumeCmdBuffers.Clear();
            }

            if (m_RenderNonShadowVolumes != null)
            {
                m_RenderNonShadowVolumes.Clear();
            }
        }

        /// <summary>
        /// Free up render targets, command buffers and any per-frame data.
        /// </summary>
        void OnDestroy()
        {
            if (m_RadianceTarget_01)
            {
                m_RadianceTarget_01.Release();
                DestroyImmediate(m_RadianceTarget_01);
                m_RadianceTarget_01 = null;
            }
            if (m_RadianceTarget_02)
            {
                m_RadianceTarget_02.Release();
                DestroyImmediate(m_RadianceTarget_02);
                m_RadianceTarget_02 = null;
            }
            if (m_PreviousDepthTarget)
            {
                m_PreviousDepthTarget.Release();
                DestroyImmediate(m_PreviousDepthTarget);
                m_PreviousDepthTarget = null;
            }

            if (m_ClearRadianceCmdBuffer != null)
            {
                if (m_Camera.actualRenderingPath == RenderingPath.DeferredShading)
                {
                    m_Camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, m_ClearRadianceCmdBuffer);
                }
                else
                {
                    CameraEvent cv = (m_Camera.depthTextureMode & DepthTextureMode.DepthNormals) == DepthTextureMode.DepthNormals ? CameraEvent.BeforeDepthNormalsTexture : CameraEvent.BeforeDepthTexture;
                    m_Camera.RemoveCommandBuffer(cv, m_ClearRadianceCmdBuffer);
                }
                m_ClearRadianceCmdBuffer.Dispose();
                m_ClearRadianceCmdBuffer = null;
            }

            if (m_ShadowCascadesCmdBuffer != null)
            {
                if (m_DirectLight != null)
                {
                    m_DirectLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_ShadowCascadesCmdBuffer);
                }
                m_ShadowCascadesCmdBuffer.Dispose();
                m_ShadowCascadesCmdBuffer = null;
            }

            if (m_DirectionalLightCmdBuffer != null)
            {
                if (m_DirectLight != null)
                {
                    m_DirectLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, m_DirectionalLightCmdBuffer);
                }
                m_DirectionalLightCmdBuffer.Dispose();
                m_DirectionalLightCmdBuffer = null;
            }

            if (m_LightVolumeCmdBuffers.Count > 0)
            {
                foreach (KeyValuePair<Light, CommandBuffer> entry in m_LightVolumeCmdBuffers)
                {
                    entry.Key.RemoveCommandBuffer(LightEvent.AfterShadowMap, entry.Value);
                    entry.Value.Dispose();
                }

                m_LightVolumeCmdBuffers.Clear();
            }

            if (m_RenderNonShadowVolumes != null)
            {
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_RenderNonShadowVolumes);
                m_RenderNonShadowVolumes.Dispose();
                m_RenderNonShadowVolumes = null;
            }
        }

        /// <summary>
        /// Set everything up for rendering atmospherics and light volumes this frame. The material values are gathered from the render context
        /// and all the command buffers are filled for setting up the directional light scattering. Light volumes that are in range are found
        /// and their command buffers set up.
        /// </summary>
        void OnPreRender()
        {
            if (!CheckHasSystemSupport())
            {
                enabled = false;
            }

            UpdateResources();
            SetShaderKeyWords();

            // Get the temporary radiance target for use this frame. This is released in OnRenderImage after the final upscale and compose.
#if UNITY_5_6_OR_NEWER
            RenderTextureFormat rtFormat = m_Camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#else
            RenderTextureFormat rtFormat = m_Camera.hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif
#if UNITY_5_5_OR_NEWER
            m_PerFrameRadianceTarget = RenderTexture.GetTemporary(m_X, m_Y, 0, rtFormat, RenderTextureReadWrite.Linear, AntiAliasingLevel());
#else
            m_PerFrameRadianceTarget = RenderTexture.GetTemporary(m_X, m_Y, 0, rtFormat, RenderTextureReadWrite.Linear, 1);
#endif
            m_PerFrameRadianceTarget.name = "_DS_Haze_PerFrameRadiance";
            m_PerFrameRadianceTarget.filterMode = FilterMode.Point;

            // Use a command buffer to clear the per-frame radiance target at the start of the frame, as it most likely has the contents of the
            // previous frame.
            m_ClearRadianceCmdBuffer.Clear();
            m_ClearRadianceCmdBuffer.SetRenderTarget(m_PerFrameRadianceTarget);
            m_ClearRadianceCmdBuffer.ClearRenderTarget(false, true, Color.clear);

            // Get the render context to setup the material for atmospherics and directional light scattering.
            DS_HazeCore core = DS_HazeCore.Instance;
            DS_HazeContextItem ctxToRender = null;
            if (m_OverrideContextAsset && m_Context != null)
            {
                if (m_OverrideContextVariant)
                {
                    ctxToRender = m_Context.Context.GetItemAtIndex(m_ContextItemIndex);
                }
                else
                {
                    ctxToRender = m_Context.Context.GetContextItemBlended(m_Time);
                }
            }
            else
            {
                if (core == null)
                {
                    SetGlobalParamsToNull();
                    return;
                }
                ctxToRender = core.GetRenderContextAtPosition(transform.position);
            }

            // Just in case neither DS_HazeCore nor the override context return a valid variant
            // (camera could be outside a zone).
            if (ctxToRender == null)
            {
                // Need to reset the global shader properties so any transparencies rendered with this
                // camera get the correct fog values.
                SetGlobalParamsToNull();
            }
            else
            {
                // Update the render material with the values from the blended context.
                SetMaterialFromContext(ctxToRender);

                // Setup the command buffer for the directional light rendering here.
                float farClip = m_Camera.farClipPlane;
                float fovWHalf = m_Camera.fieldOfView * 0.5f;
                float dY = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);
                float dX = dY * m_Camera.aspect;
                Vector3 vpC = transform.forward * farClip;
                Vector3 vpR = transform.right * dX * farClip;
                Vector3 vpU = transform.up * dY * farClip;
                m_Material.SetVector("_ViewportCorner", vpC - vpR - vpU);
                m_Material.SetVector("_ViewportRight", vpR * 2.0f);
                m_Material.SetVector("_ViewportUp", vpU * 2.0f);

                if (m_DirectLight && m_RenderAtmosphereVolumetrics)
                {
                    // Ray-march volumetric pass.
                    //RenderTargetIdentifier noRT = new RenderTargetIdentifier(BuiltinRenderTextureType.None); //<-- resolve 'ambiguous call to blit' when passing null.
                    m_DirectionalLightCmdBuffer.Blit(BuiltinRenderTextureType.None, m_PerFrameRadianceTarget, m_Material, (int)m_VolumeSamples + (m_DownsampleFactor == SizeFactor.Half ? 0 : 3));
                }
            }

            if (m_RenderLocalVolumetrics == false)
            {
                // Not rendering local light volumes, can end now.
                return;
            }

            // Setup rendering for per-frame light volumes. These are ray-marched after normal lighting or (for shadow casters) just after the shadow map has
            // been rendered.
            Matrix4x4 gpuProjMtx = GL.GetGPUProjectionMatrix(m_Camera.projectionMatrix, true);
            Matrix4x4 viewProjMtx = gpuProjMtx * m_Camera.worldToCameraMatrix;

            core.GetRenderLightVolumes(transform.position, m_PerFrameLightVolumes, m_PerFrameShadowLightVolumes);

            // If this camera will render light volumes, set the render target.
            if (m_PerFrameLightVolumes.Count > 0)
            {
                m_RenderNonShadowVolumes.SetRenderTarget(m_PerFrameRadianceTarget);
            }

            foreach (DS_HazeLightVolume lv in m_PerFrameLightVolumes)
            {
                lv.SetupMaterialPerFrame(viewProjMtx, m_Camera.worldToCameraMatrix, transform, WillRenderWithTemporalReprojection ? m_InterleavedOffsetIndex : 0);

                // Add this light to this camera's command buffer.
                lv.AddLightRenderCommand(transform, m_RenderNonShadowVolumes, (int)m_DownsampleFactor);
            }
            foreach (DS_HazeLightVolume lv in m_PerFrameShadowLightVolumes)
            {
                lv.SetupMaterialPerFrame(viewProjMtx, m_Camera.worldToCameraMatrix, transform, WillRenderWithTemporalReprojection ? m_InterleavedOffsetIndex : 0);

                // This light will render using it's own command buffer.
                lv.FillLightCommandBuffer(m_PerFrameRadianceTarget, transform, (int)m_DownsampleFactor);
                m_LightVolumeCmdBuffers.Add(lv.LightSource, lv.RenderCommandBuffer);
            }
        }

        /// <summary>
        /// Custom Blit with support for multiple render targets. Some parts of the effect combine
        /// several aspects of rendering using MRTs for performance. It is assumed the destination
        /// array is in the same order as the shader expects!
        /// </summary>
        /// <param name="source"> The render target to bind to _MainTex. </param>
        /// <param name="destination"> The render targets to bind to SV_Target outputs. </param>
        /// <param name="mat"> Material to render with. </param>
        /// <param name="pass"> Shader pass to render with. </param>
        private void BlitToMRT(RenderTexture source, RenderTexture[] destination, Material mat, int pass)
        {
            RenderBuffer[] colourBuffers = new RenderBuffer[destination.Length];
            for (int rb = 0; rb < destination.Length; rb++)
            {
                   colourBuffers[rb] = destination[rb].colorBuffer;
            }
            Graphics.SetRenderTarget(colourBuffers, destination[0].depthBuffer);

            mat.SetTexture("_MainTex", source);
            mat.SetPass(pass);

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.MultiTexCoord2(0, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 0.1f);
            GL.MultiTexCoord2(0, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 0.0f, 0.1f);
            GL.MultiTexCoord2(0, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 0.1f);
            GL.MultiTexCoord2(0, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.1f);
            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Upscale and re-project the accumulated radiance buffer. All volumetric lighting has been calculated by this point,
        /// now need to calculate the analytic atmosphere components (fog, secondary scattering, Rayleigh) and do the final
        /// compose with the scene.
        /// </summary>
        /// <param name="src"> Render target to bind to _MainTex. </param>
        /// <param name="dest"> Final output render target. </param>
        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            RenderTexture tmpRad = null;
            RenderTexture tmpHalfDepth = null;

            if (m_RenderAtmosphereVolumetrics || m_RenderLocalVolumetrics)
            {
#if UNITY_5_6_OR_NEWER
                tmpRad = RenderTexture.GetTemporary(m_X, m_Y, 0, m_Camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32);
#else
                tmpRad = RenderTexture.GetTemporary(m_X, m_Y, 0, m_Camera.hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32);
#endif
                tmpHalfDepth = RenderTexture.GetTemporary(m_X, m_Y, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);

                // Down-sample the depth buffer.
                Graphics.Blit(null, tmpHalfDepth, m_Material, m_DownsampleFactor == SizeFactor.Half ? 10 : 11);
                m_Material.SetTexture("_HalfResDepth", tmpHalfDepth);

                // Bi-lateral Gaussian gather pass.
                Graphics.Blit(m_PerFrameRadianceTarget, tmpRad, m_Material, 6);
                Graphics.Blit(tmpRad, m_PerFrameRadianceTarget, m_Material, 7);

                // Set the previous radiance, depth and current radiance buffers to use during temporal reprojection.
                if (m_TemporalReprojection)
                {
                    m_Material.SetTexture("_PrevAccumBuffer", m_PreviousRadianceTarget);
                    m_Material.SetTexture("_PrevDepthBuffer", m_PreviousDepthTarget);
                }
            }

            m_PerFrameRadianceTarget.filterMode = FilterMode.Bilinear;
            m_Material.SetTexture("_RadianceBuffer", m_PerFrameRadianceTarget);

            // If this DS_HazeView is the last in the blit chain, dest will be null. This should never be the case in an
            // actual game (tonemapping/colour grading/bloom etc. will come after), but to prevent errors while editing,
            // compose into a temporary target and then perform a final blit to the backbuffer.
            RenderTexture tmpDest;
            if (dest == null)
            {
                tmpDest = RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format);
                if (WillRenderWithTemporalReprojection)
                {
                    RenderTexture[] mrts1 = { tmpDest, m_CurrentRadianceTarget };
                    BlitToMRT(src, mrts1, m_Material, 8);
                }
                else
                {
                    Graphics.Blit(src, tmpDest, m_Material, 8);
                }
                Graphics.Blit(tmpDest, (RenderTexture)null);
                RenderTexture.ReleaseTemporary(tmpDest);
            }
            else
            {
                if (WillRenderWithTemporalReprojection)
                {
                    RenderTexture[] mrts1 = { dest, m_CurrentRadianceTarget };
                    BlitToMRT(src, mrts1, m_Material, 8);
                }
                else
                {
                    Graphics.Blit(src, dest, m_Material, 8);

                }
            }

            // Grab the depth buffer for reprojection next frame. We need to manually set the render target back to 'dest' afterwards
            // so further rendering (transparencies, image effects) goes into the correct buffer.
            if (WillRenderWithTemporalReprojection)
            {
                Graphics.Blit(src, m_PreviousDepthTarget, m_Material, 9);
                Graphics.SetRenderTarget(dest);

                // Make the radiance buffer available to everyone so skybox transparent shaders can sample volumetrics.
                Shader.SetGlobalTexture("_DS_RadianceBuffer", m_CurrentRadianceTarget);
                RenderTexture.ReleaseTemporary(m_PerFrameRadianceTarget);
            }
            else
            {
                Shader.SetGlobalTexture("_DS_RadianceBuffer", m_PerFrameRadianceTarget);
            }

            if (tmpRad != null)
            {
                RenderTexture.ReleaseTemporary(tmpRad);
            }
            if (tmpHalfDepth != null)
            {
                RenderTexture.ReleaseTemporary(tmpHalfDepth);
            }   
        }

        /// <summary>
        /// Update the values used for temporal reprojection and swap the radiance buffer being rendered to next frame.
        /// Also cleanup the per-frame lists and command buffers used to render light volumes.
        /// </summary>
        void OnPostRender()
        {
            if (WillRenderWithTemporalReprojection)
            {
                // Swap the radiance buffers ready for the next frame.
                RenderTexture tmp = m_CurrentRadianceTarget;
                m_CurrentRadianceTarget = m_PreviousRadianceTarget;
                m_PreviousRadianceTarget = tmp;

                // Update the view/projection matrix used to transform from world-space to previous frame clip-space.
                Matrix4x4 thisViewMatrix = m_Camera.worldToCameraMatrix;
                Matrix4x4 thisProjMatrix = GL.GetGPUProjectionMatrix(m_Camera.projectionMatrix, true);
                m_PreviousViewProjMatrix = thisProjMatrix * thisViewMatrix;
                m_PreviousInvViewProjMatrix = m_PreviousViewProjMatrix.inverse;
            }
            else
            {
                RenderTexture.ReleaseTemporary(m_PerFrameRadianceTarget);
            }

            if (m_LightVolumeCmdBuffers.Count > 0)
            {
                foreach (KeyValuePair<Light, CommandBuffer> entry in m_LightVolumeCmdBuffers)
                {
                    entry.Value.Clear();
                }
                m_LightVolumeCmdBuffers.Clear();
            }

            if (m_DirectLight)
            {
                m_DirectionalLightCmdBuffer.Clear();
            }
            m_RenderNonShadowVolumes.Clear();
            m_PerFrameLightVolumes.Clear();
            m_PerFrameShadowLightVolumes.Clear();
        }
    }
}
