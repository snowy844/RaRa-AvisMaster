using UnityEngine;

namespace DeepSky.Haze
{
    [ExecuteInEditMode, AddComponentMenu("DeepSky Haze/Zone", 52)]
    public class DS_HazeZone : MonoBehaviour
    {
        #region FIELDS
        [SerializeField]
        private DS_HazeContext m_Context = new DS_HazeContext();
        [SerializeField, Range(0, 250)]
        private int m_Priority = 0;
        [SerializeField, Range(0.001f, 1)]
        private float m_BlendRange = 0.1f;

        public DS_HazeContext Context
        {
            get { return m_Context; }
        }

        public int Priority
        {
            get { return m_Priority; }
            set { m_Priority = value > 0 ? value : 0; }
        }

        public float BlendRange
        {
            get { return m_BlendRange; }
            set { m_BlendRange = Mathf.Clamp01(value); }
        }
        #endregion

        private Bounds m_AABB;
        private float m_BlendRangeInverse;

        /// <summary>
        /// Perform initialisation - get the transform component and set the bounding volume size. This MUST be
        /// called any time the zone's transform changes!
        /// </summary>
        private void Setup()
        {
            m_AABB = new Bounds(Vector3.zero, transform.localScale);
            m_BlendRangeInverse = 1.0f / Mathf.Max((Mathf.Min(m_AABB.extents.x, m_AABB.extents.y, m_AABB.extents.z) * m_BlendRange), Mathf.Epsilon);
        }

        void Start()
        {
            Setup();
        }

        void OnValidate()
        {
            Setup();
        }

        /// <summary>
        /// Test if a world-space position is inside this zone. This requires transforming the point to local-space
        /// to get the rotation but we still need to apply the scale.
        /// </summary>
        /// <param name="position"> The world-space position to test. </param>
        /// <returns> True if the position is inside, otherwise false. </returns>
        public bool Contains(Vector3 position)
        {
            if (transform.hasChanged)
            {
                Setup();
            }

            Vector3 localPosition = transform.InverseTransformPoint(position);
            localPosition.Scale(transform.localScale);
            return m_AABB.Contains(localPosition);
        }

        /// <summary>
        /// Get the blending weight to apply to this zone calculated from the shortest distance to the bounding volume.
        /// The weight will be 0 at the edge of the zone, 1 at the distance inside the zone controlled by the
        /// BlendRange zone property.
        /// </summary>
        /// <param name="position"> World-space position at which to calculate blending. </param>
        /// <returns> Blend weight in range [0-1]. </returns>
        public float GetBlendWeight(Vector3 position)
        {
            Vector3 localPosition = transform.InverseTransformPoint(position);
            localPosition.Scale(transform.localScale);

            float dX = Mathf.Abs(m_AABB.extents.x - Mathf.Abs(localPosition.x));
            float dY = Mathf.Abs(m_AABB.extents.y - Mathf.Abs(localPosition.y));
            float dZ = Mathf.Abs(m_AABB.extents.z - Mathf.Abs(localPosition.z));

            float dT = Mathf.Min(dX, dY, dZ);
            return Mathf.Clamp01(dT * m_BlendRangeInverse);
        }

        /// <summary>
        /// Greater-than operator. Zones are compared first on priorities and then (in the case of equal priorities)
        /// the size. Smaller zones are considered higher priority as they represent more localised settings.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator >(DS_HazeZone c1, DS_HazeZone c2)
        {
            if (c1.m_Priority == c2.m_Priority)
            {
                return Vector3.Dot(c1.m_AABB.extents, c1.m_AABB.extents) > Vector3.Dot(c2.m_AABB.extents, c2.m_AABB.extents) ? true : false;
            }

            return c1.m_Priority > c2.m_Priority ? true : false;
        }

        /// <summary>
        /// Less-than operator. Zones are compared first on priorities and then (in the case of equal priorities)
        /// the size. Smaller zones are considered higher priority as they represent more localised settings.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator <(DS_HazeZone c1, DS_HazeZone c2)
        {
            if (c1.m_Priority == c2.m_Priority)
            {
                return Vector3.Dot(c1.m_AABB.extents, c1.m_AABB.extents) < Vector3.Dot(c2.m_AABB.extents, c2.m_AABB.extents) ? true : false;
            }

            return c1.m_Priority < c2.m_Priority ? true : false;
        }
    }
}