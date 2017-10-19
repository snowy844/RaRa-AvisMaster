using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeepSky.Haze.Demo
{
    public class BasicMouseLookControl : MonoBehaviour
    {
        [SerializeField]
        private float m_XSensitivity = 2.5f;
        [SerializeField]
        private float m_YSensitivity = 2.5f;

        Quaternion m_StartRotation;
        float m_X = 0;
        float m_Y = 0;

        void Start()
        {
            m_StartRotation = transform.localRotation;
        }

        void Update()
        {
            m_X += Input.GetAxis("Mouse X") * m_XSensitivity;
            m_Y += Input.GetAxis("Mouse Y") * m_YSensitivity;

            if (m_X > 360) m_X = 0.0f;
            else if (m_X < 0) m_X = 360.0f;
            if (m_Y > 60) m_Y = 60.0f;
            else if (m_Y < -60) m_Y = -60.0f;

            Quaternion xaxis = Quaternion.AngleAxis(m_X, Vector3.up);
            Quaternion yaxis = Quaternion.AngleAxis(m_Y, Vector3.left);

            transform.localRotation = m_StartRotation * xaxis * yaxis;

            if (Input.GetKeyUp(KeyCode.Escape))
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
