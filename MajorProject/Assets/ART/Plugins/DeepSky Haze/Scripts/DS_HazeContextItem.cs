using UnityEngine;
using System;
using System.Reflection;

namespace DeepSky.Haze
{
    [Serializable, AddComponentMenu("")]
    public class DS_HazeContextItem
    {
        public enum Multiplier { OneTenth, OneFifth, OneHalf, One, Two, Five, Ten };

        public static float MultiplierAsFloat(Multiplier mult)
        {
            switch (mult)
            {
                case Multiplier.OneTenth:
                    return 0.1f;
                case Multiplier.OneFifth:
                    return 0.2f;
                case Multiplier.OneHalf:
                    return 0.5f;
                case Multiplier.One:
                    return 1.0f;
                case Multiplier.Two:
                    return 2.0f;
                case Multiplier.Five:
                    return 5.0f;
                case Multiplier.Ten:
                    return 10.0f;
                default:
                    return 1.0f;
            }
        }

        public static float ParamWithMultiplier(float param, Multiplier mult)
        {
            switch (mult)
            {
                case Multiplier.OneTenth:
                    return param * 0.1f;
                case Multiplier.OneFifth:
                    return param * 0.2f;
                case Multiplier.OneHalf:
                    return param * 0.5f;
                case Multiplier.One:
                    return param * 1.0f;
                case Multiplier.Two:
                    return param * 2.0f;
                case Multiplier.Five:
                    return param * 5.0f;
                case Multiplier.Ten:
                    return param * 10.0f;
                default:
                    return param * 1.0f;
            }
        }

        #region FIELDS
        [SerializeField]
        public string m_Name;
        [SerializeField]
        public AnimationCurve m_Weight;
        [SerializeField, Range(0, 8.0f)]
        public float m_AirScatteringScale = 1.0f;
        [SerializeField]
        public Multiplier m_AirScatteringMultiplier = Multiplier.One;
        [SerializeField, Range(0.0001f, 0.1f)]
        public float m_AirDensityHeightFalloff = 0.001f;

        [SerializeField, Range(0, 8.0f)]
        public float m_HazeScatteringScale = 1.0f;
        [SerializeField]
        public Multiplier m_HazeScatteringMultiplier = Multiplier.One;
        [SerializeField, Range(0.0001f, 0.1f)]
        public float m_HazeDensityHeightFalloff = 0.003f;
        [SerializeField, Range(-0.99f, 0.99f)]
        public float m_HazeScatteringDirection = 0.8f;
        [SerializeField, Range(0, 1)]
        public float m_HazeSecondaryScatteringRatio = 0.8f;

        [SerializeField, Range(0, 1)]
        public float m_FogOpacity = 1.0f;
        [SerializeField, Range(0, 8.0f)]
        public float m_FogScatteringScale = 1.0f;
        [SerializeField, Range(0, 8.0f)]
        public float m_FogExtinctionScale = 1.0f;
        [SerializeField]
        public Multiplier m_FogExtinctionMultiplier = Multiplier.One;
        [SerializeField, Range(0.0001f, 0.5f)]
        public float m_FogDensityHeightFalloff = 0.01f;
        [SerializeField, Range(0, 1)]
        public float m_FogStartDistance = 0.0f;
        [SerializeField, Range(-0.99f, 0.99f)]
        public float m_FogScatteringDirection = 0.7f;
        [SerializeField]
        public Color m_FogAmbientColour = Color.white;
        [SerializeField]
        public Color m_FogLightColour = Color.white;
        #endregion

        /// <summary>
        /// Default constructor with blend in/out around 0.5.
        /// </summary>
        public DS_HazeContextItem()
        {
            m_Name = "New";
            m_Weight = new AnimationCurve(new Keyframe(0.25f, 0), new Keyframe(0.5f, 1), new Keyframe(0.75f, 0));
        }

        /// <summary>
        /// Linearly interpolate between this variant and another.
        /// </summary>
        /// <param name="other"> DS_HazeContextVariant to interpolate with. </param>
        /// <param name="dt"> How far through the blend. </param>
        public void Lerp(DS_HazeContextItem other, float dt)
        {
            if (other == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("DeepSky::DS_HazeContextItem:Lerp - null context passed!");
#endif
                return;
            }

            dt = Mathf.Clamp01(dt);

            float dTinv = 1.0f - dt;
            
            m_AirScatteringScale = m_AirScatteringScale * dTinv + other.m_AirScatteringScale * dt;
            m_AirDensityHeightFalloff = m_AirDensityHeightFalloff * dTinv + other.m_AirDensityHeightFalloff * dt;
            m_HazeScatteringScale = m_HazeScatteringScale * dTinv + other.m_HazeScatteringScale * dt;
            m_HazeDensityHeightFalloff = m_HazeDensityHeightFalloff * dTinv + other.m_HazeDensityHeightFalloff * dt;
            m_HazeScatteringDirection = m_HazeScatteringDirection * dTinv + other.m_HazeScatteringDirection * dt;
            m_HazeSecondaryScatteringRatio = m_HazeSecondaryScatteringRatio * dTinv + other.m_HazeSecondaryScatteringRatio * dt;
            m_FogOpacity = m_FogOpacity * dTinv + other.m_FogOpacity * dt;
            m_FogScatteringScale = m_FogScatteringScale * dTinv + other.m_FogScatteringScale * dt;
            m_FogExtinctionScale = m_FogExtinctionScale * dTinv + other.m_FogExtinctionScale * dt;
            m_FogDensityHeightFalloff = m_FogDensityHeightFalloff * dTinv + other.m_FogDensityHeightFalloff * dt;
            m_FogStartDistance = m_FogStartDistance * dTinv + other.m_FogStartDistance * dt;
            m_FogScatteringDirection = m_FogScatteringDirection * dTinv + other.m_FogScatteringDirection * dt;
            m_FogAmbientColour = m_FogAmbientColour * dTinv + other.m_FogAmbientColour * dt;
            m_FogLightColour = m_FogLightColour * dTinv + other.m_FogLightColour * dt;
        }

        /// <summary>
        /// Copy all the values from 'other'.
        /// </summary>
        /// <param name="other"> The DS_HazeContextVariant to get values from. </param>
        public void CopyFrom(DS_HazeContextItem other)
        {
            if (other == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("DeepSky::DS_HazeContextItem:CopyFrom - null context passed!");
#endif
                return;
            }

            Type thisType = GetType();
            Type otherType = other.GetType();

            foreach (FieldInfo field in thisType.GetFields())
            {
                FieldInfo otherField = otherType.GetField(field.Name);
                field.SetValue(this, otherField.GetValue(other));
            }

            // We need an actual copy of the weight curve, not just the reference.
            m_Weight = new AnimationCurve(m_Weight.keys);
        }
    }
}
