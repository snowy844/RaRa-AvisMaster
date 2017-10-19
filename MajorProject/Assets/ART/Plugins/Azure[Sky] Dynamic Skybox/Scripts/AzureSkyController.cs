using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Azure[Sky]/Sky Controller")]
[ExecuteInEditMode]
public class AzureSkyController : MonoBehaviour
{
	#region Editor Variables
	//Editor variables.
	//-------------------------------------------------------------------------------------------------------
	public bool  Azure_ShowTimeOfDayTab           = true;
	public bool  Azure_ShowObjectsAndMaterialsTab = false;
	public bool  Azure_ShowScatteringTab          = false;
	public bool  Azure_ShowNightSkyTab            = false;
	public bool  Azure_ShowCloudTab               = false;
	public bool  Azure_ShowFogTab                 = false;
	public bool  Azure_ShowLightingTab            = false;
	public bool  Azure_ShowOptionsTab             = false;
	public bool  Azure_ShowOutputsTab             = false;
	public Color Azure_CurveColorField            = Color.yellow;
	#endregion

	#region Time of Day Tab Variables
	//Time of Day Tab.
	//-------------------------------------------------------------------------------------------------------
	public int   Azure_TimeMode  = 0;
	public int   Azure_CurveMode = 0;
	public int   Azure_DayOfWeek = 0;
	public int   Azure_NumberOfDays = 7;
	public float Azure_Timeline  = 6.0f;
	public int   Azure_Latitude  = 0;
	public int   Azure_Longitude = 0;
	public float Azure_DayCycle  = 10.0f;//In minutes.
	public bool  Azure_SetTimeByCurve   = false;
	public float Azure_TimeOfDayByCurve =  6.0f;
	public AnimationCurve Azure_DayNightLengthCurve  = AnimationCurve.Linear(0,0,24,24);
	private float Azure_GetCurveTime;
	private float Azure_GetGradientTime;
	private float Azure_GetCurveSunElevation;
	private float Azure_GetGradientSunElevation;
	private float Azure_GetGradientMoonElevation;
	private float Azure_GetCurveMoonElevation;
	//Realistic Time Mode.
	public int    Azure_Day    = 15;
	public int    Azure_Month  = 9;
	public int    Azure_Year   = 1903;
	public int    Azure_MaxDayMonth = 30;
	public int    Azure_UTC         = 0;
	public bool   Azure_StartAtCurrentTime = false;
	public bool   Azure_StartAtCurrentDate = false;
	private bool  Azure_LeapYear = false;
	private float lst, sinLatitude, cosLatitude, radians;
	private Matrix4x4 Azure_StarfieldMatrix;
	#endregion

	#region Objects and Materials Tab Variables
	//Objects and Materials Tab.
	//-------------------------------------------------------------------------------------------------------
	public Transform Azure_SunDirectionalLight;
	public Transform Azure_MoonDirectionalLight;
	public Transform Azure_Skydome;
	public Material  Azure_SkyMaterial;
	public Material  Azure_FogMaterial;
	//public Material  Azure_MoonMaterial;
	#endregion

	#region Scattering Tab Variables
	//Scattering Tab.
	//-------------------------------------------------------------------------------------------------------
	private Vector3 Azure_Br            = new Vector3 (0.006955632f, 0.01176226f, 0.02439022f);//Pre computed Rayleigh.
	private Vector3 Azure_Bm            = new Vector3 (0.005721017f, 0.004451339f, 0.003146905f);//Pre computed Mie.
	private Vector3 Azure_MieG          = new Vector3 (0.4375f, 1.5625f, 1.5f);//Pre computed directionality factor.
	public  float   Azure_Rayleigh      = 1.0f;
	public  float   Azure_Mie           = 1.0f;
	private float   Azure_Pi316         = 0.0596831f;
	private float   Azure_Pi14          = 0.07957747f;
	public  float   Azure_Kr            = 8.4f;
	public  float   Azure_Km            = 1.25f;
	public  float   Azure_SunIntensity  = 15.0f;
	public float Azure_SkyDarkness      = 0.0f;
	public float Azure_NightIntensity   = 0.5f;
	public float Azure_Exposure         = 2.0f;
	public float Azure_SunDiskSize        = 500.0f;
	public float Azure_SunDiskPropagation = 5.0f;
	public float Azure_SunMoonLightSpeed  = 50.0f;
	private float r;
	private float Azure_SunRise;
	private float Azure_MoonRise;

	//public float[] Azure_g     =  0.75f;//Directionality factor.
	public int Azure_ComputeMode = 0;
	public Color Azure_SkyColor  =  new Color(0.65f, 0.57f, 0.475f, 1.0f);
	public Vector3 Azure_Lambda  =  new Vector3(650.0f, 570.0f, 475.0f);
	private Vector3 Azure_K      =  new Vector3(686.0f, 678.0f, 666.0f);
	private const float Azure_n  =  1.0003f;  //Refractive index of air.
	private const float Azure_N  =  2.545E25f;//Molecular density.
	private const float Azure_pn =  0.035f;   //Depolatization factor for standard air.
	private const float pi       =  Mathf.PI; //3.141592

	//Curves by Timeline.
	public AnimationCurve[] Azure_RayleighCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f),
		AnimationCurve.Linear (0, 1.0f, 24, 1.0f)
	};
	public AnimationCurve[] Azure_MieCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f)
	};
	public AnimationCurve[] Azure_KrCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f),
		AnimationCurve.Linear(0,8.4f,24,8.4f)
	};
	public AnimationCurve[] Azure_KmCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f),
		AnimationCurve.Linear(0,1.25f,24,1.25f)
	};
	public AnimationCurve[] Azure_SunIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f),
		AnimationCurve.Linear(0,15.0f,24,15.0f)
	};
	public AnimationCurve[] Azure_SkyDarknessCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f),
		AnimationCurve.Linear (0, 0.0f, 24, 0.0f)
	};
	public AnimationCurve[] Azure_NightIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f)
	};
	public AnimationCurve[] Azure_ExposureCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f),
		AnimationCurve.Linear(0,2.0f,24,2.0f)
	};
	public AnimationCurve[] Azure_SunDiskSizeCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f),
		AnimationCurve.Linear (0, 500.0f, 24, 500.0f)
	};
	public AnimationCurve[] Azure_SunDiskPropagationCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f),
		AnimationCurve.Linear (0, 5.0f, 24, 5.0f)
	};
	public Gradient[] Azure_RayleighGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MieGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_SkyColorGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};

	//Curves by elevation.
	public AnimationCurve[] Azure_RayleighCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f),
		AnimationCurve.Linear (-1, 1.0f, 1, 1.0f)
	};
	public AnimationCurve[] Azure_MieCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f)
	};
	public AnimationCurve[] Azure_KrCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f),
		AnimationCurve.Linear(-1,8.4f,1,8.4f)
	};
	public AnimationCurve[] Azure_KmCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f),
		AnimationCurve.Linear(-1,1.25f,1,1.25f)
	};
	public AnimationCurve[] Azure_SunIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f),
		AnimationCurve.Linear(-1,15.0f,1,15.0f)
	};
	public AnimationCurve[] Azure_SkyDarknessCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f),
		AnimationCurve.Linear (-1, 0.0f, 1, 0.0f)
	};
	public AnimationCurve[] Azure_NightIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f)
	};
	public AnimationCurve[] Azure_ExposureCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f),
		AnimationCurve.Linear(-1,2.0f,1,2.0f)
	};
	public AnimationCurve[] Azure_SunDiskSizeCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f),
		AnimationCurve.Linear (-1, 500.0f, 1, 500.0f)
	};
	public AnimationCurve[] Azure_SunDiskPropagationCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f),
		AnimationCurve.Linear (-1, 5.0f, 1, 5.0f)
	};
	public Gradient[] Azure_RayleighGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MieGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_SkyColorGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	#endregion

	#region Night Tab Variables
	//Night Sky Tab.
	//-------------------------------------------------------------------------------------------------------
	public Cubemap Azure_StarfieldTexture;
	public Cubemap Azure_StarNoiseTexture;
	public float   Azure_StarfieldIntensity    = 0.0f;
	public float   Azure_MilkyWayIntensity     = 0.0f;
	public Vector3 Azure_StarfieldColorBalance = new Vector3 (1.0f, 1.0f, 1.0f);
	public Vector3 Azure_StarfieldPosition;
	public Texture Azure_MoonTexture;
	public float Azure_MoonDiskSize        = 0.5f;
	public float Azure_MoonDiskBright      = 0.15f;
	public float Azure_MoonDiskBrightRange = 200.0f;
	public float Azure_MoonSkyBright       = 0.15f;
	public float Azure_MoonSkyBrightRange  = 50.0f;
	private float Azure_FixMoonSize        = 0.0f;
	public float  Azure_StarsScintillation  = 5.5f;
	private float Azure_NoiseMatrixRotation = 0.0f;
	private  Matrix4x4 Azure_NoiseMatrix;
	public float Azure_MoonPenumbra   = 0.5f;
	public float Azure_MoonShadow     = 0.0f;
	public float Azure_MoonSaturation = 1.0f;

	//Curves based on Timeline.
	public AnimationCurve[] Azure_StarfieldIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f)
	};
	public AnimationCurve[] Azure_MilkyWayIntensityCurve  = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f)
	};
	public AnimationCurve[] Azure_MoonDiskSizeCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f),
		AnimationCurve.Linear(0,0.5f,24,0.5f)
	};
	public AnimationCurve[] Azure_MoonDiskBrightCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f)
	};
	public AnimationCurve[] Azure_MoonDiskBrightRangeCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f),
		AnimationCurve.Linear (0, 200.0f, 24, 200.0f)
	};
	public AnimationCurve[] Azure_MoonSkyBrightCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f),
		AnimationCurve.Linear (0, 0.15f, 24, 0.15f)
	};
	public AnimationCurve[] Azure_MoonSkyBrightRangeCurve = new AnimationCurve[] {
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f),
		AnimationCurve.Linear (0, 50.0f, 24, 50.0f)
	};
	public Gradient[] Azure_MoonDiskGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MoonSkyBrightGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};

	//Curves based on sun/moon elevation.
	public AnimationCurve[] Azure_StarfieldIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f)
	};
	public AnimationCurve[] Azure_MilkyWayIntensityCurveE  = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f)
	};
	public AnimationCurve[] Azure_MoonDiskSizeCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f),
		AnimationCurve.Linear(-1,0.5f,1,0.5f)
	};
	public AnimationCurve[] Azure_MoonDiskBrightCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f)
	};
	public AnimationCurve[] Azure_MoonDiskBrightRangeCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f),
		AnimationCurve.Linear (-1, 200.0f, 1, 200.0f)
	};
	public AnimationCurve[] Azure_MoonSkyBrightCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f),
		AnimationCurve.Linear (-1, 0.15f, 1, 0.15f)
	};
	public AnimationCurve[] Azure_MoonSkyBrightRangeCurveE = new AnimationCurve[] {
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f),
		AnimationCurve.Linear (-1, 50.0f, 1, 50.0f)
	};
	public Gradient[] Azure_MoonDiskGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MoonSkyBrightGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	#endregion

	#region Cloud Tab Variables
	//Cloud Tab.
	//-------------------------------------------------------------------------------------------------------
	public int Azure_CloudMode = 0;
	public Texture2D Azure_StaticCloudTexture;
	public float Azure_StaticCloudMultiplier = 1.0f;
	//Dynamic Clouds.
	public Texture2D Azure_DynamicCloudTexture;
	public float Azure_DynamicCloudDensity   = 0.75f;
	public float Azure_DynamicCloudDirection = 1.0f;
	public float Azure_DynamicCloudSpeed     = 0.1f;

	//Curves based on timeline.
	public AnimationCurve[] Azure_StaticCloudMultiplierCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f)
	};
	public Gradient[] Azure_StaticCloudEdgeGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_StaticCloudDensityGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	//2D Dynamic Clouds.
	public AnimationCurve[] Azure_DynamicCloudDensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f)
	};
	public Gradient[] Azure_DynamicCloudEdgeGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_DynamicCloudDensityGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};

	//Curves based on sun/moon elevation.
	public AnimationCurve[] Azure_StaticCloudMultiplierCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f)
	};
	public Gradient[] Azure_StaticCloudEdgeGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_StaticCloudDensityGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	//2D Dynamic Clouds.
	public AnimationCurve[] Azure_DynamicCloudDensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f)
	};
	public Gradient[] Azure_DynamicCloudEdgeGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_DynamicCloudDensityGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	#endregion

	#region Fog Tab Variables
	//Fog Tab.
	//-------------------------------------------------------------------------------------------------------
	public int   Azure_FogMode       = 0;
	public float Azure_FogDistance   = 75.0f;
	public float Azure_FogScale      = 0.75f;
	public float Azure_FogExtinction = 0.15f;
	public float Azure_FogMieDepth   = 0.0f;

	//Curves based on timeline.
	public AnimationCurve[] Azure_FogDistanceCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f),
		AnimationCurve.Linear(0,75.0f,24,75.0f)
	};
	public AnimationCurve[] Azure_FogScaleCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f),
		AnimationCurve.Linear(0,0.75f,24,0.75f)
	};
	public AnimationCurve[] Azure_FogExtinctionCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f),
		AnimationCurve.Linear(0,0.15f,24,0.15f)
	};

	//Curves based on sun/moon elevation.
	public AnimationCurve[] Azure_FogDistanceCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f),
		AnimationCurve.Linear(-1,75.0f,1,75.0f)
	};
	public AnimationCurve[] Azure_FogScaleCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f),
		AnimationCurve.Linear(-1,0.75f,1,0.75f)
	};
	public AnimationCurve[] Azure_FogExtinctionCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f),
		AnimationCurve.Linear(-1,0.15f,1,0.15f)
	};
	#endregion

	#region Lightin Tab Variables
	//Lighting Tab.
	//-------------------------------------------------------------------------------------------------------
	private Light Azure_SunLightComponent;
	private Light Azure_MoonLightComponent;
	public float Azure_SunLightIntensity  = 0.0f;
	public float Azure_MoonLightIntensity = 0.0f;
	public int Azure_UnityAmbientSource = 0;
	public float Azure_AmbientIntensity = 1.0f;
	//Curves based on timeline.
	public AnimationCurve[] Azure_SunLightIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f)
	};
	public AnimationCurve[] Azure_MoonLightIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f),
		AnimationCurve.Linear(0,0.0f,24,0.0f)
	};
	public AnimationCurve[] Azure_AmbientIntensityCurve = new AnimationCurve[] {
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f),
		AnimationCurve.Linear(0,1.0f,24,1.0f)
	};
	public Gradient[] Azure_SunLightGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MoonLightGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityAmbientGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityEquatorGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityGroundGradientColor = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};

	//Curves based on sun/moon elevation.
	public AnimationCurve[] Azure_SunLightIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f)
	};
	public AnimationCurve[] Azure_MoonLightIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f),
		AnimationCurve.Linear(-1,0.0f,1,0.0f)
	};
	public AnimationCurve[] Azure_AmbientIntensityCurveE = new AnimationCurve[] {
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f),
		AnimationCurve.Linear(-1,1.0f,1,1.0f)
	};
	public Gradient[] Azure_SunLightGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_MoonLightGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityAmbientGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityEquatorGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	public Gradient[] Azure_UnityGroundGradientColorE = new Gradient[] {
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient(),
		new Gradient()
	};
	#endregion

	#region Options Tab Variables
	//Options Tab.
	//-------------------------------------------------------------------------------------------------------
	public bool Azure_FollowActiveMainCamera = true;
	public bool Azure_AutomaticSunIntensity = true;
	public bool Azure_AutomaticMoonIntensity = true;
	public float Azure_GammaCorrection = 1.0f;
	public int Azure_CullMode   = 1;
	public int Azure_ShaderMode = 0;
	public int Azure_SkyMode    = 0;
	public bool Azure_AutomaticSkydomeSize = true;
	public float Azure_SkydomeSizePercentage = 90.0f;
	public float Azure_SkydomeSize = 1000.0f;
	#endregion

	#region Output Tab
	//Outputs Tab.
	//-------------------------------------------------------------------------------------------------------
	public List<AnimationCurve> Azure_OutputCurveList    = new List<AnimationCurve>();
	public List<Gradient>       Azure_OutputGradientList = new List<Gradient>();
	#endregion

	#region Start()
	//Use this for initialization.
	void Start ()
	{
		//Create the moon render texture.
		//AzureCreateMoonRenderTexture(512);
		//Graphics.Blit (null, Azure_MoonTexture, Azure_MoonMaterial, 0);
		Azure_SunLightComponent  = Azure_SunDirectionalLight.GetComponent<Light> ();
		Azure_MoonLightComponent = Azure_MoonDirectionalLight.GetComponent<Light> ();

		//Follow MainCamera.
		if (Azure_FollowActiveMainCamera && Camera.main)
		{
			transform.position = Camera.main.transform.position;
		}
		//Sets manually Skydome size.
		if (Azure_SkyMode == 0 && !Azure_AutomaticSkydomeSize)
		{
			Azure_Skydome.localScale = new Vector3 (Azure_SkydomeSize, Azure_SkydomeSize, Azure_SkydomeSize);
		}

		if (Azure_StartAtCurrentTime)
			AzureGetCurrentTime ();
		if (Azure_StartAtCurrentDate)
			AzureGetCurrentDate ();

		AzureSetTime (Azure_Timeline, Azure_DayCycle);
		AzureLighting();
		AzureSetCloudMode ();
		AzureSetFogMode ();
		AzureSetSkyMode();
		AzureShaderInitializeUniforms ();
		AzureShaderUpdateUniforms ();
	}
	#endregion

	#region Update()
	private Quaternion Azure_SunPosition; //For the Sun Rotation.
	private float Azure_PassTimeValue = 0.0f;
	private float cameraFarPlane  = 1000.0f;
	//Update is called once per frame.
	void Update ()
	{
		//Update moon render texture.
		//Graphics.Blit (null, Azure_MoonTexture, Azure_MoonMaterial, 0);
		//-------------------------------------------------------------------------------------------------------
		//Follow MainCamera.
		if (Azure_FollowActiveMainCamera && Camera.main)
		{
			transform.position = Camera.main.transform.position;
		}

		//Sets automatic Skydome size.
		if (Azure_SkyMode == 0 && Azure_AutomaticSkydomeSize == true && Camera.main)
		{
			cameraFarPlane = Camera.main.farClipPlane * (Azure_SkydomeSizePercentage / 100.0f) * 2.0f;
			Azure_Skydome.localScale = new Vector3 (cameraFarPlane, cameraFarPlane, cameraFarPlane);
		}

		//Pre Compute Mie and Rayleigh.
		if (Azure_ComputeMode == 1)
		{
			AzureSetWavelength ();
			Azure_Bm = AzureComputeBetaMie ();
			Azure_Br = AzureComputeBetaRay ();
		} else
			{
				Azure_Br = new Vector3 (0.006955632f, 0.01176226f, 0.02439022f);//Pre computed Rayleigh.
				Azure_Bm = new Vector3 (0.005721017f, 0.004451339f, 0.003146905f);//Pre computed Mie.
			}

		//Need constant update.
		//-------------------------------------------------------------------------------------------------------
		Azure_TimeOfDayByCurve = Azure_DayNightLengthCurve.Evaluate(Azure_Timeline);
		Shader.SetGlobalVector ("_Azure_SunDirection" , -Azure_SunDirectionalLight.transform.forward);
		Shader.SetGlobalVector ("_Azure_MoonDirection" , -Azure_MoonDirectionalLight.transform.forward);
		Shader.SetGlobalMatrix ("_Azure_MoonMatrix",  Azure_MoonDirectionalLight.transform.worldToLocalMatrix);
		AzureLighting();
		AzureShaderUpdateUniforms ();
		if (Azure_StarsScintillation > 0.0f)
		{
			Azure_NoiseMatrixRotation += Azure_StarsScintillation * Time.deltaTime;
			Quaternion rot = Quaternion.Euler (Azure_NoiseMatrixRotation, Azure_NoiseMatrixRotation, Azure_NoiseMatrixRotation);
			Azure_NoiseMatrix = Matrix4x4.TRS (Vector3.zero, rot, new Vector3 (1, 1, 1));
			Shader.SetGlobalMatrix ("_Azure_NoiseMatrix", Azure_NoiseMatrix);
		}

		//Timeline.
		//-------------------------------------------------------------------------------------------------------
		// Only in gameplay.
		if (Application.isPlaying)
		{
			//Pass the time of day.
			Azure_Timeline += Azure_PassTimeValue * Time.deltaTime;

			//Restart timeline and update the date.
			if (Azure_Timeline > 24.0f)
			{
				Azure_DayOfWeek++;
				if (Azure_DayOfWeek > (Azure_NumberOfDays-1)) {
					Azure_DayOfWeek = 0;
				}
				if (Azure_TimeMode == 1)
				{
					Azure_Day++;
					if (Azure_Day > Azure_MaxDayMonth) {
						Azure_Day = 1;
						Azure_Month++;
						if (Azure_Month > 12) {
							Azure_Month = 1;
							Azure_Year++;
							if (Azure_Year > 9999) {
								Azure_Year = 1;
							}
						}
					}
				}
				Azure_Timeline = 0.0f;
			}
		}

		//Get Curves and Gradients time based on Curve Mode.
		//-------------------------------------------------------------------------------------------------------
		//Get curves and gradients time based on Timeline.
		Azure_GetGradientTime = Azure_Timeline / 24.0f;
		Azure_GetCurveTime = Azure_Timeline;
		if (Azure_SetTimeByCurve)
		{
			Azure_GetCurveTime = Azure_TimeOfDayByCurve;
			Azure_GetGradientTime = Azure_TimeOfDayByCurve / 24.0f;
		}
		//Get curves and gradients time based on sun/moon elevation.
		Azure_GetCurveSunElevation    = Vector3.Dot (-Azure_SunDirectionalLight.transform.forward, new Vector3(0,1,0));
		Azure_GetGradientSunElevation = Mathf.InverseLerp (-1, 1, Azure_GetCurveSunElevation);
		Azure_GetCurveMoonElevation    = Vector3.Dot (-Azure_MoonDirectionalLight.transform.forward, new Vector3(0,1,0));
		Azure_GetGradientMoonElevation = Mathf.InverseLerp (-1, 1, Azure_GetCurveMoonElevation);
		r              = Vector3.Magnitude(new Vector3(0f, Azure_SunMoonLightSpeed, 0f));
		Azure_MoonRise = Mathf.Clamp(Vector3.Dot(new Vector3(0f, 500f,0f), -Azure_MoonDirectionalLight.transform.forward) / r, 0f, 1f);
		Azure_SunRise  = Mathf.Clamp(Vector3.Dot(new Vector3(0f, 500f,0f), -Azure_SunDirectionalLight.transform.forward) / r, 0f, 1f);

		//Set Sun/Moon positions based on Time Mode.
		//-------------------------------------------------------------------------------------------------------
		switch(Azure_TimeMode)
		{
		//Simple Sun and Moon Position.
		case 0:

			//Setting sun and moon position.
			Azure_SunPosition = Quaternion.Euler (0.0f, Azure_Longitude, Azure_Latitude);
			Azure_SunPosition *= Quaternion.Euler (AzureSetSimpleSunPosition (), 0.0f, 0.0f);
			Azure_SunDirectionalLight.transform.rotation = Azure_SunPosition;
			Azure_MoonDirectionalLight.transform.rotation = AzureSetSimpleMoonPosition ();
			Azure_StarfieldMatrix = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler (Azure_StarfieldPosition), Vector3.one);
			Shader.SetGlobalMatrix ("_Azure_SunMatrix",  Azure_SunDirectionalLight.transform.worldToLocalMatrix);
			Shader.SetGlobalMatrix ("_Azure_StarfieldMatrix", Azure_StarfieldMatrix);
			break;

		//Realistic Sun and Moon Position.
		case 1:
			AzureSetMaxDayPerMonth();
			//Planetary Positions. By Paul Schlyter, Stockholm, Sweden
			//http://www.stjarnhimlen.se/comp/ppcomp.html
			//---------------------------------------------------------------------------------------------------
			radians = (Mathf.PI * 2.0f) / 360.0f;//Used to convert degress to radians.
			//Need convert to radians.
			float radLatitude = radians * Azure_Latitude;
			sinLatitude = Mathf.Sin(radLatitude);
			cosLatitude = Mathf.Cos(radLatitude);

			//Setting lights positions.
			//---------------------------------------------------------------------------------------------------
			Vector3 sunPosition = AzureSetRealisticSunPosition();
			Azure_SunDirectionalLight.transform.forward = sunPosition;
			Vector3 moonPosition = AzureSetRealisticMoonPosition();
			Azure_MoonDirectionalLight.transform.forward = moonPosition;


			//Setting Starfield Position.
			//---------------------------------------------------------------------------------------------------
			Quaternion starfieldRotation    = Quaternion.Euler(90.0f - Azure_Latitude, 0.0f, 0.0f) * Quaternion.Euler(0.0f, Azure_Longitude, 0.0f) * Quaternion.Euler(0.0f, lst * Mathf.Rad2Deg, 0.0f);
			Azure_StarfieldMatrix = Matrix4x4.TRS (Vector3.zero, starfieldRotation * Quaternion.Euler(Azure_StarfieldPosition), Vector3.one);
			Shader.SetGlobalMatrix ("_Azure_StarfieldMatrix", Azure_StarfieldMatrix.inverse);
			Shader.SetGlobalMatrix ("_Azure_SunMatrix",  Matrix4x4.identity);
			break;
		}

		//Update every frame only in Editor. In Game need only initialization on Start.
		//-------------------------------------------------------------------------------------------------------
		#if UNITY_EDITOR
		AzureShaderInitializeUniforms ();
		AzureSetCloudMode ();
		AzureSetFogMode ();
		AzureSetSkyMode();

		//Sets manually Skydome size.
		if (Azure_SkyMode == 0 && Azure_AutomaticSkydomeSize == false)
		{
			Azure_Skydome.localScale = new Vector3 (Azure_SkydomeSize, Azure_SkydomeSize, Azure_SkydomeSize) * 2.0f;
		}
		#endif

	}
	#endregion

	#region Time and Lighting methods
	//Simple Sun Position.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Set the simple sun position based on "Time of Day".
	/// </summary>
	public float AzureSetSimpleSunPosition()
	{
		float ret;
		if (Azure_SetTimeByCurve)
		{
			ret = ((Azure_TimeOfDayByCurve) * 360.0f / 24.0f) - 90.0f;
		} else
			 {
			ret = ((Azure_Timeline) * 360.0f / 24.0f) - 90.0f;
		     }
		return ret;
	}
	//Simple Moon Position.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Set the simple moon position based on "Time of Day".
	/// </summary>
	public Quaternion AzureSetSimpleMoonPosition()
	{
		return Azure_SunDirectionalLight.transform.rotation * Quaternion.Euler(1, -180, 1);
	}
	// Set "Time of Day" and "Day Duration"
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Set the "Time of Day" and duration of Day/Night cycle.
	/// </summary>
	public void  AzureSetTime(float hour, float dayDuration) {
		Azure_Timeline = hour;
		Azure_DayCycle = dayDuration;
		if (dayDuration > 0.0f)
			Azure_PassTimeValue = (24.0f / 60.0f) / Azure_DayCycle;
		else
			Azure_PassTimeValue = 0.0f;
	}

	//Time and Date.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Get the current system time.
	/// </summary>
	public void AzureGetCurrentTime()
	{
		Azure_Timeline = System.DateTime.Now.Hour + ((1.0f/60.0f) * System.DateTime.Now.Minute);
		//Azure_Minute = System.DateTime.Now.Minute;
	}
	/// <summary>
	/// Get the current system date.
	/// </summary>
	public void AzureGetCurrentDate()
	{
		Azure_Day = System.DateTime.Now.Day;
		Azure_Month = System.DateTime.Now.Month;
		Azure_Year = System.DateTime.Now.Year;
	}
	/// <summary>
	/// Convert timeline to hour and minutes. Will return a Vector2(hour, minutes).
	/// </summary>
	public Vector2 AzureGetHourAndMinutes()
	{
		Vector2 ret;
		if (Azure_SetTimeByCurve)
		{
			ret.x = Mathf.Floor (Azure_TimeOfDayByCurve);
			ret.y = 60.0f * (Azure_TimeOfDayByCurve - Mathf.Floor (Azure_TimeOfDayByCurve));
			ret.y = Mathf.Floor (ret.y);
		} else
		{
			ret.x = Mathf.Floor (Azure_Timeline);
			ret.y = 60.0f * (Azure_Timeline - Mathf.Floor (Azure_Timeline));
			ret.y = Mathf.Floor (ret.y);
		}
		return ret;
	}

	//Realistic Sun Position.
	//-------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Set the realistic sun position based on Time, Date and Location.
	/// </summary>
	private Vector3 AzureSetRealisticSunPosition()
	{
		float hour = Azure_Timeline - Azure_UTC;

		//Time Scale.
		//---------------------------------------------------------------------------------------------------
		//d = 367*y - 7 * ( y + (m+9)/12 ) / 4 + 275*m/9 + D - 730530
		//d = d + UT/24.0
		float d = 367 * Azure_Year - 7 * (Azure_Year + (Azure_Month + 9) / 12) / 4 + 275 * Azure_Month / 9 + Azure_Day - 730530;
		      d = d + hour / 24.0f;

		//Tilt of earth's axis.
		//---------------------------------------------------------------------------------------------------
		//obliquity of the ecliptic.
		float ecliptic = 23.4393f - 3.563E-7f * d;
		//Need convert to radians before apply sine and cosine.
		float radEcliptic = radians * ecliptic;
		float sinEcliptic = Mathf.Sin(radEcliptic);
		float cosEcliptic = Mathf.Cos(radEcliptic);

		//Orbital elements of the Sun.
		//---------------------------------------------------------------------------------------------------
		//float N = 0.0;
		//float i = 0.0;
		float w = 282.9404f + 4.70935E-5f * d;
		//float a = 1.000000f;
		float e = 0.016709f - 1.151E-9f * d;
		float M = 356.0470f + 0.9856002585f * d;

		//Eccentric anomaly.
		//---------------------------------------------------------------------------------------------------
		//E = M + e*(180/pi) * sin(M) * ( 1.0 + e * cos(M) ) in degress.
		//E = M + e * sin(M) * ( 1.0 + e * cos(M) ) in radians.
		//Need convert to radians before apply sine and cosine.
		float radM = radians * M;
		float sinM = Mathf.Sin(radM);
		float cosM = Mathf.Cos(radM);

		//Need convert to radians before apply sine and cosine.
		float radE = radM + e * sinM * (1.0f + e * cosM);
		float sinE = Mathf.Sin(radE);
		float cosE = Mathf.Cos(radE);

		//Sun's distance (r) and its true anomaly (v).
		//---------------------------------------------------------------------------------------------------
		//Xv = r * cos (v) = cos (E) - e
		//Yv = r * sen (v) = sqrt (1,0 - e * e) * sen (E)
		float xv = cosE - e;
		float yv = Mathf.Sqrt(1.0f - e*e) * sinE;

		//V = atan2 (yv, xv)
		//R = sqrt (xv * xv + yv * yv)
		float v = Mathf.Rad2Deg * Mathf.Atan2(yv, xv);
		float r = Mathf.Sqrt(xv*xv + yv*yv);

		//Sun's true longitude.
		//---------------------------------------------------------------------------------------------------
		float radLongitude = radians * (v + w);
		float sinLongitude = Mathf.Sin(radLongitude);
		float cosLongitude = Mathf.Cos(radLongitude);

		float xs = r * cosLongitude;
		float ys = r * sinLongitude;

		//Equatorial coordinates.
		//---------------------------------------------------------------------------------------------------
		float xe = xs;
		float ye = ys * cosEcliptic;
		float ze = ys * sinEcliptic;

		//Sun's Right Ascension(RA) and Declination(Dec).
		//---------------------------------------------------------------------------------------------------
		float RA     = Mathf.Atan2(ye, xe);
		float Dec    = Mathf.Atan2(ze, Mathf.Sqrt(xe*xe + ye*ye));
		float sinDec = Mathf.Sin(Dec);
		float cosDec = Mathf.Cos(Dec);

		//The Sidereal Time.
		//---------------------------------------------------------------------------------------------------
		float Ls = v + w;

		float GMST0 = Ls + 180.0f;
		float UT    = 15.0f * hour;//Universal Time.
		float GMST  = GMST0 + UT;
		float LST   = radians * (GMST + Azure_Longitude);
		//Store local sideral time.
		lst   = LST;

		//Azimuthal coordinates.
		//---------------------------------------------------------------------------------------------------
		float HA    = LST - RA;
		float sinHA = Mathf.Sin(HA);
		float cosHA = Mathf.Cos(HA);

		float x = cosHA * cosDec;
		float y = sinHA * cosDec;
		float z = sinDec;

		float xhor = x * sinLatitude - z * cosLatitude;
		float yhor = y;
		float zhor = x * cosLatitude + z * sinLatitude;

		//az  = atan2( yhor, xhor ) + 180_degrees
		//alt = asin( zhor ) = atan2( zhor, sqrt(xhor*xhor+yhor*yhor) )
		float azimuth  = Mathf.Atan2(yhor, xhor) + radians * 180.0f;
		float altitude = Mathf.Asin (zhor);

		//Zenith angle.
		//Zenith=90°−α  Where α is the elevation angle.
		float zenith   = 90.0f * radians - altitude;

		//Converts from Spherical(radius r, zenith-inclination θ, azimuth φ) to Cartesian(x,y,z) coordinates.
		//https://en.wikipedia.org/wiki/Spherical_coordinate_system
		//---------------------------------------------------------------------------------------------------
		//x​​​​ = r sin(θ)cos(φ)​​
		//​y​​​​ = r sin(θ)sin(φ)
		//z = r cos(θ)
		Vector3 ret;

		//radius = 1
		ret.z = Mathf.Sin(zenith) * Mathf.Cos(azimuth);
		ret.x = Mathf.Sin(zenith) * Mathf.Sin(azimuth);
		ret.y = Mathf.Cos(zenith);

		return ret * -1.0f;
	}

	//Ralistic Moon Position.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Set the realistic moon position based on Time, Date and Location.
	/// </summary>
	private Vector3 AzureSetRealisticMoonPosition()
	{
		float hour  = Azure_Timeline - Azure_UTC;

		//Time Scale.
		//---------------------------------------------------------------------------------------------------
		//d = 367*y - 7 * ( y + (m+9)/12 ) / 4 + 275*m/9 + D - 730530
		//d = d + UT/24.0
		float d = 367 * Azure_Year - 7 * (Azure_Year + (Azure_Month + 9) / 12) / 4 + 275 * Azure_Month / 9 + Azure_Day - 730530;
		d = d + hour / 24.0f;

		//Tilt of earth's axis.
		//---------------------------------------------------------------------------------------------------
		//obliquity of the ecliptic.
		float ecliptic = 23.4393f - 3.563E-7f * d;
		//Need convert to radians before apply sine and cosine.
		float radEcliptic = radians * ecliptic;
		float sinEcliptic = Mathf.Sin(radEcliptic);
		float cosEcliptic = Mathf.Cos(radEcliptic);

		//Orbital elements of the Moon.
		//---------------------------------------------------------------------------------------------------
		float N = 125.1228f - 0.0529538083f	* d;
		float i = 5.1454f;
		float w = 318.0634f + 0.1643573223f * d;
		float a = 60.2666f;
		float e = 0.054900f;
		float M = 115.3654f + 13.0649929509f * d;

		//Eccentric anomaly.
		//---------------------------------------------------------------------------------------------------
		//E = M + e*(180/pi) * sin(M) * ( 1.0 + e * cos(M) )
		float radM = radians * M;
		float E    = radM + e * Mathf.Sin(radM) * (1f + e * Mathf.Cos(radM));

		//Planet's distance and true anomaly.
		//---------------------------------------------------------------------------------------------------
		//xv = r * cos(v) = a * ( cos(E) - e )
		//yv = r * sin(v) = a * ( sqrt(1.0 - e*e) * sin(E) )
		float xv = a * (Mathf.Cos(E) - e);
		float yv = a * (Mathf.Sqrt (1f - e * e) * Mathf.Sin(E));
		//V = atan2 (yv, xv)
		//R = sqrt (xv * xv + yv * yv)
		float v = Mathf.Rad2Deg * Mathf.Atan2(yv, xv);
		float r = Mathf.Sqrt(xv*xv + yv*yv);

		//Moon position in 3D space.
		//---------------------------------------------------------------------------------------------------
		float radLongitude = radians * (v + w);
		float sinLongitude = Mathf.Sin(radLongitude);
		float cosLongitude = Mathf.Cos(radLongitude);

		//Geocentric (Earth-centered) coordinates.
		//---------------------------------------------------------------------------------------------------
		//xh = r * ( cos(N) * cos(v+w) - sin(N) * sin(v+w) * cos(i) )
		//yh = r * ( sin(N) * cos(v+w) + cos(N) * sin(v+w) * cos(i) )
		//zh = r * ( sin(v+w) * sin(i) )
		float radN = radians * N;
		float radI = radians * i;

		float xh = r * (Mathf.Cos(radN) * cosLongitude - Mathf.Sin(radN) * sinLongitude * Mathf.Cos(radI));
		float yh = r * (Mathf.Sin(radN) * cosLongitude + Mathf.Cos(radN) * sinLongitude * Mathf.Cos(radI));
		float zh = r * (sinLongitude * Mathf.Sin(radI));

		//float xg = xh; //No needed to the moon.
		//float yg = yh;
		//float zg = zh;

		//Equatorial coordinates.
		//---------------------------------------------------------------------------------------------------
		float xe = xh;
		float ye = yh * cosEcliptic - zh * sinEcliptic;
		float ze = yh * sinEcliptic + zh * cosEcliptic;

		//Planet's Right Ascension (RA) and Declination (Dec).
		//---------------------------------------------------------------------------------------------------
		float RA  = Mathf.Atan2(ye, xe);
		float Dec = Mathf.Atan2(ze, Mathf.Sqrt(xe*xe + ye*ye));

		//The Sidereal Time.
		//---------------------------------------------------------------------------------------------------
		//It is already calculated for the sun and stored in the lst, it is not necessary to calculate again for the moon.
		//float Ls = ls;

		//float GMST0 = Ls + 180.0f;
		//float UT    = 15.0f * hour;
		//float GMST  = GMST0 + UT;
		//float LST   = radians * (GMST + Azure_Longitude);

		//Azimuthal coordinates.
		//---------------------------------------------------------------------------------------------------
		float HA = lst - RA;

		float x = Mathf.Cos(HA) * Mathf.Cos(Dec);
		float y = Mathf.Sin(HA) * Mathf.Cos(Dec);
		float z = Mathf.Sin(Dec);

		float xhor = x * sinLatitude - z * cosLatitude;
		float yhor = y;
		float zhor = x * cosLatitude + z * sinLatitude;

		//az  = atan2( yhor, xhor ) + 180_degrees
		//alt = asin( zhor ) = atan2( zhor, sqrt(xhor*xhor+yhor*yhor) )
		float azimuth  = Mathf.Atan2 (yhor, xhor) + radians * 180.0f;
		float altitude = Mathf.Asin (zhor);

		//Zenith angle.
		//Zenith = 90°−α  where α is the elevation angle.
		float zenith   = 90.0f * radians - altitude;

		//Converts from Spherical(radius r, zenith-inclination θ, azimuth φ) to Cartesian(x,y,z) coordinates.
		//https://en.wikipedia.org/wiki/Spherical_coordinate_system
		//---------------------------------------------------------------------------------------------------
		//x​​​​ = r sin(θ)cos(φ)​​
		//​y​​​​ = r sin(θ)sin(φ)
		//z = r cos(θ)
		Vector3 ret;

		//radius = 1
		ret.z = Mathf.Sin(zenith) * Mathf.Cos(azimuth);
		ret.x = Mathf.Sin(zenith) * Mathf.Sin(azimuth);
		ret.y = Mathf.Cos(zenith);

		return ret * -1.0f;
	}

	//Lighting.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Sets the scene lighting.
	/// </summary>
	private void AzureLighting()
	{
		if (Azure_SunLightComponent != null)
		{
			switch (Azure_CurveMode)
			{
			case 0:
				Azure_SunLightIntensity = Azure_SunLightIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
				if (Azure_AutomaticSunIntensity) {
					Azure_SunLightIntensity *= Azure_SunRise;
				}
				Azure_SunLightComponent.intensity = Azure_SunLightIntensity;
				Azure_SunLightComponent.color = Azure_SunLightGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
				break;
			case 1:
				Azure_SunLightIntensity = Azure_SunLightIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
				if (Azure_AutomaticSunIntensity) {
					Azure_SunLightIntensity *= Azure_SunRise;
				}
				Azure_SunLightComponent.intensity = Azure_SunLightIntensity;
				Azure_SunLightComponent.color = Azure_SunLightGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
				break;
			}

			if (Azure_SunLightComponent.intensity <= 0)
			{
				Azure_SunLightComponent.enabled = false;
			} else
				 {
					 Azure_SunLightComponent.enabled = true;
				 }
		}
		if (Azure_MoonLightComponent != null)
		{
			switch (Azure_CurveMode)
			{
			case 0:
				Azure_MoonLightIntensity = Azure_MoonLightIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
				if (Azure_AutomaticMoonIntensity) {
					Azure_MoonLightIntensity *= Azure_MoonRise;
				}
				Azure_MoonLightComponent.intensity = Azure_MoonLightIntensity;
				Azure_MoonLightComponent.color = Azure_MoonLightGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
				break;
			case 1:
				Azure_MoonLightIntensity = Azure_MoonLightIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
				if (Azure_AutomaticMoonIntensity) {
					Azure_MoonLightIntensity *= Azure_MoonRise * (1.0f - Azure_SunRise);
				}
				Azure_MoonLightComponent.intensity = Azure_MoonLightIntensity;
				Azure_MoonLightComponent.color = Azure_MoonLightGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
				break;
			}

			if (Azure_MoonLightComponent.intensity <= 0)
			{
				Azure_MoonLightComponent.enabled = false;
			} else
			{
				Azure_MoonLightComponent.enabled = true;
			}
		}
		switch (Azure_UnityAmbientSource)
		{
		case 0:
				switch (Azure_CurveMode)
				{
				case 0:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					//RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
					break;
				case 1:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					//RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
					break;
				}
			break;

		case 1:
				switch (Azure_CurveMode)
				{
				case 0:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
					RenderSettings.ambientEquatorColor = Azure_UnityEquatorGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
					RenderSettings.ambientGroundColor = Azure_UnityGroundGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
					break;
				case 1:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
					RenderSettings.ambientEquatorColor = Azure_UnityEquatorGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
					RenderSettings.ambientGroundColor = Azure_UnityGroundGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
					break;
				}
			break;

		case 2:
				switch (Azure_CurveMode)
				{
				case 0:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
					break;
				case 1:
					Azure_AmbientIntensity = Azure_AmbientIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
					RenderSettings.ambientIntensity = Azure_AmbientIntensity;
					RenderSettings.ambientSkyColor = Azure_UnityAmbientGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
					break;
				}
			break;
		}
	}

	//Outputs.
	//-------------------------------------------------------------------------------------------------------
	/// curveMode = 0 = timeline;
	/// curveMode = 1 = sun elevation;
	/// curveMode = 2 = moon elevation;
	/// <summary>
	/// Get Azure[Sky] curve output based on "Curve Mode".
	/// </summary>
	public float AzureGetCurveOutput(int index, int curveMode)
	{
		float ret = 0.0f;
		switch (curveMode)
		{
		case 0:
			ret = Azure_OutputCurveList [index].Evaluate (Azure_GetCurveTime);
			break;
		case 1:
			ret = Azure_OutputCurveList [index].Evaluate (Azure_GetCurveSunElevation);
			break;
		case 2:
			ret = Azure_OutputCurveList [index].Evaluate (Azure_GetCurveMoonElevation);
			break;
		}
		return ret;
	}

	/// curveMode = 0 = timeline;
	/// curveMode = 1 = sun elevation;
	/// curveMode = 2 = moon elevation;
	/// <summary>
	/// Get Azure[Sky] gradient output based on "Curve Mode".
	/// </summary>
	public Color AzureGetGradientOutput(int index, int curveMode)
	{
		Color ret = Color.white;
		switch (curveMode)
		{
		case 0:
			ret = Azure_OutputGradientList [index].Evaluate (Azure_GetGradientTime);
			break;
		case 1:
			ret = Azure_OutputGradientList [index].Evaluate (Azure_GetGradientSunElevation);
			break;
		case 2:
			ret = Azure_OutputGradientList [index].Evaluate (Azure_GetGradientMoonElevation);
			break;
		}
		return ret;
	}

	/// <summary>
	/// Sets correct number of days in each month.
	/// </summary>
	void AzureSetMaxDayPerMonth()
	{
		if (Azure_Month == 1 || Azure_Month == 3 || Azure_Month == 5 || Azure_Month == 7 || Azure_Month == 8 || Azure_Month == 10 || Azure_Month == 12) {
			Azure_MaxDayMonth = 31;
		}
		if (Azure_Month == 4 || Azure_Month == 6 || Azure_Month == 9 || Azure_Month == 11 ) {
			Azure_MaxDayMonth = 30;
		}
		if(Azure_Month == 2)
		{
			//https://pt.wikipedia.org/wiki/Ano_bissexto
			Azure_LeapYear = false;
			if ((Azure_Year % 4 == 0 && Azure_Year % 100 != 0) || Azure_Year % 400 == 0) {
				Azure_LeapYear = true;
			}
			
			if (Azure_LeapYear)
			{
				Azure_MaxDayMonth = 29;
			} else
				{
					Azure_MaxDayMonth = 28;
				}
		}
	}
	#endregion

	#region Hoffman and Preetham Equations
	//Total Rayleigh.
	//-------------------------------------------------------------------------------------------------------
	private Vector3 AzureComputeBetaRay()
	{
		//Converting the wavelength values given in Inpector for real scale used in formula.
		Vector3 converted_lambda = Azure_Lambda * 1.0e-9f;

		Vector3 Br;
		////////////////
		// Without pn //
		//The (6.0f - 7.0f * pn) and (6.0f + 3.0f * pn), they are not included in this equation because there is no significant visual changes in the sky.
		Br.x = ((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(Azure_n, 2.0f) - 1.0f, 2.0f)) ) / (3.0f * Azure_N * Mathf.Pow(converted_lambda.x, 4.0f)))*1000.0f;
		Br.y = ((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(Azure_n, 2.0f) - 1.0f, 2.0f)) ) / (3.0f * Azure_N * Mathf.Pow(converted_lambda.y, 4.0f)))*1000.0f;
		Br.z = ((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(Azure_n, 2.0f) - 1.0f, 2.0f)) ) / (3.0f * Azure_N * Mathf.Pow(converted_lambda.z, 4.0f)))*1000.0f;

		///////////////////////
		// Original equation //
		//Br.x = (((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(n, 2.0f) - 1.0f, 2.0f)))*(6.0f+3.0f*pn) ) / ((3.0f * N * Mathf.Pow(converted_lambda.x, 4.0f))*(6.0f-7.0f*pn) ))*1000.0f;
		//Br.y = (((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(n, 2.0f) - 1.0f, 2.0f)))*(6.0f+3.0f*pn) ) / ((3.0f * N * Mathf.Pow(converted_lambda.y, 4.0f))*(6.0f-7.0f*pn) ))*1000.0f;
		//Br.z = (((8.0f * Mathf.Pow(pi, 3.0f) * (Mathf.Pow(Mathf.Pow(n, 2.0f) - 1.0f, 2.0f)))*(6.0f+3.0f*pn) ) / ((3.0f * N * Mathf.Pow(converted_lambda.z, 4.0f))*(6.0f-7.0f*pn) ))*1000.0f;

		return Br;
	}

	//Total Mie.
	//-------------------------------------------------------------------------------------------------------
	private Vector3 AzureComputeBetaMie()
	{
		//float c = (6544f * Turbidity - 6510f) * 10.0f;
		//float c = (0.2f * Azure_Turbidity ) * 10.0f;
		float c = (0.2f * 2.0f ) * 10.0f;
		Vector3 Bm;
		Bm.x = (434.0f * c * pi * Mathf.Pow((2.0f * pi) / Azure_Lambda.x, 2.0f) * Azure_K.x);
		Bm.y = (434.0f * c * pi * Mathf.Pow((2.0f * pi) / Azure_Lambda.y, 2.0f) * Azure_K.y);
		Bm.z = (434.0f * c * pi * Mathf.Pow((2.0f * pi) / Azure_Lambda.z, 2.0f) * Azure_K.z);

		Bm.x=Mathf.Pow(Bm.x,-1.0f);
		Bm.y=Mathf.Pow(Bm.y,-1.0f);
		Bm.z=Mathf.Pow(Bm.z,-1.0f);

		return Bm;
	}

	//Get Wavelength.
	private void AzureSetWavelength()
	{
		switch (Azure_CurveMode)
		{
		case 0:
			Azure_SkyColor = Azure_SkyColorGradientColor[Azure_DayOfWeek].Evaluate (Azure_GetGradientTime);
			break;

		case 1:
			Azure_SkyColor = Azure_SkyColorGradientColorE[Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation);
			break;
		}
		Azure_Lambda.x = Mathf.Lerp(950, 350, Azure_SkyColor.r);
		Azure_Lambda.y = Mathf.Lerp(870, 270, Azure_SkyColor.g);
		Azure_Lambda.z = Mathf.Lerp(775, 175, Azure_SkyColor.b);
	}

	//Get mie g constants.
	//-------------------------------------------------------------------------------------------------------
	//private Vector3 AzureCumputeMieG()
	//{
	//	return new Vector3(1.0f - Azure_g * Azure_g, 1.0f + Azure_g * Azure_g, 2.0f * Azure_g);
	//}
	#endregion

	#region Materials and Shaders methods
	//Need only initialization on Start.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Updates the uniforms shader that needs only be updated at "Scene Start".
	/// </summary>
	void AzureShaderInitializeUniforms()
	{
		Shader.SetGlobalTexture ("_Azure_StarfieldTexture", Azure_StarfieldTexture);
		Shader.SetGlobalTexture ("_AzureStarNoiseTexture", Azure_StarNoiseTexture);
		Shader.SetGlobalVector ("_Azure_StarfieldColorBalance", Azure_StarfieldColorBalance);
		Shader.SetGlobalTexture ("_Azure_MoonTexture", Azure_MoonTexture);
		Shader.SetGlobalInt ("_Azure_CullMode", Azure_CullMode);
		Shader.SetGlobalTexture ("_Azure_StaticCloudTexture", Azure_StaticCloudTexture);
		Shader.SetGlobalTexture ("_Azure_DynamicCloudTexture", Azure_DynamicCloudTexture);
	}

	//Need constant update.
	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Update shader uniforms every frame.
	/// </summary>
	void AzureShaderUpdateUniforms()
	{
		Shader.SetGlobalTexture ("_Azure_MoonTexture", Azure_MoonTexture);
		switch (Azure_CurveMode)
		{
		//Curve based on Timeline.
		case 0:
			//Get Curves Value.
			//-------------------------------------------------------------------------------------------------------
			//Scattering.
			Azure_Rayleigh = Azure_RayleighCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_Mie = Azure_MieCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_Kr = Azure_KrCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_Km = Azure_KmCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_SunIntensity = Azure_SunIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_SunDiskSize = Azure_SunDiskSizeCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_SunDiskPropagation = Azure_SunDiskPropagationCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			//Night.
			Azure_Exposure = Azure_ExposureCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_SkyDarkness = Azure_SkyDarknessCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_NightIntensity = Azure_NightIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_StarfieldIntensity = Azure_StarfieldIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MilkyWayIntensity = Azure_MilkyWayIntensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MoonDiskSize = Azure_MoonDiskSizeCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MoonDiskBright = Azure_MoonDiskBrightCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MoonDiskBrightRange = Azure_MoonDiskBrightRangeCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MoonSkyBright = Azure_MoonSkyBrightCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_MoonSkyBrightRange = Azure_MoonSkyBrightRangeCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			//Cloud.
			Azure_StaticCloudMultiplier = Azure_StaticCloudMultiplierCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_DynamicCloudDensity   = Azure_DynamicCloudDensityCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			//Fog.
			Azure_FogDistance = Azure_FogDistanceCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_FogScale = Azure_FogScaleCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);
			Azure_FogExtinction = Azure_FogExtinctionCurve [Azure_DayOfWeek].Evaluate (Azure_GetCurveTime);

			//Scattering.
			Shader.SetGlobalFloat ("_Azure_Pi316", Azure_Pi316);
			Shader.SetGlobalVector ("_Azure_Br", Azure_Br * Azure_Rayleigh);
			Shader.SetGlobalVector ("_Azure_Bm", Azure_Bm * Azure_Mie);
			Shader.SetGlobalVector ("_Azure_MieG", Azure_MieG);
			Shader.SetGlobalFloat ("_Azure_Pi316", Azure_Pi316);
			Shader.SetGlobalFloat ("_Azure_Pi14", Azure_Pi14);
			Shader.SetGlobalFloat ("_Azure_Kr", Azure_Kr);
			Shader.SetGlobalFloat ("_Azure_Km", Azure_Km);
			Shader.SetGlobalFloat ("_Azure_Pi", Mathf.PI);
			Shader.SetGlobalFloat ("_Azure_SunIntensity", Azure_SunIntensity);
			Shader.SetGlobalColor ("_Azure_RayleighColor", Azure_RayleighGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalColor ("_Azure_MieColor", Azure_MieGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalFloat ("_Azure_LightSpeed", Azure_SunMoonLightSpeed);
			Shader.SetGlobalFloat ("_Azure_SunDiskSize", Azure_SunDiskSize);
			Shader.SetGlobalFloat ("_Azure_SunDiskPropagation", Azure_SunDiskPropagation);
			//Night.
			Shader.SetGlobalFloat ("_Azure_Exposure", Azure_Exposure);
			Shader.SetGlobalFloat ("_Azure_SkyDarkness", Azure_SkyDarkness);
			Shader.SetGlobalFloat ("_Azure_NightIntensity", Azure_NightIntensity);
			Shader.SetGlobalFloat ("_Azure_StarfieldIntensity", Azure_StarfieldIntensity);
			Shader.SetGlobalFloat ("_Azure_MilkyWayIntensity", Azure_MilkyWayIntensity);
			Shader.SetGlobalFloat ("_Azure_MoonDiskSize", Azure_MoonDiskSize + Azure_FixMoonSize);
			Shader.SetGlobalColor ("_Azure_MoonDiskColor", Azure_MoonDiskGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalFloat ("_Azure_MoonDiskBright", 1.0f - Azure_MoonDiskBright);
			Shader.SetGlobalFloat ("_Azure_MoonDiskBrightRange", Azure_MoonDiskBrightRange);
			Shader.SetGlobalFloat ("_Azure_MoonSkyBright", 1.0f - Azure_MoonSkyBright);
			Shader.SetGlobalFloat ("_Azure_MoonSkyBrightRange", Azure_MoonSkyBrightRange);
			Shader.SetGlobalColor ("_Azure_MoonSkyBrightColor", Azure_MoonSkyBrightGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			//3D Moon sphere.
			Shader.SetGlobalFloat ("_Azure_MoonSpherePenunbra", Azure_MoonPenumbra);
			Shader.SetGlobalFloat ("_Azure_MoonSphereShadow", Azure_MoonShadow);
			Shader.SetGlobalFloat ("_Azure_MoonSphereSaturation", Azure_MoonSaturation);

			//Cloud.
			Shader.SetGlobalColor ("_Azure_StaticCloudColor1", Azure_StaticCloudEdgeGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalColor ("_Azure_StaticCloudColor2", Azure_StaticCloudDensityGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalFloat ("_Azure_StaticCloudMultiplier", Azure_StaticCloudMultiplier);
			Shader.SetGlobalColor ("_Azure_DynamicCloudColor1", Azure_DynamicCloudEdgeGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalColor ("_Azure_DynamicCloudColor2", Azure_DynamicCloudDensityGradientColor [Azure_DayOfWeek].Evaluate (Azure_GetGradientTime));
			Shader.SetGlobalFloat ("_Azure_DynamicCloudCovarage", Azure_DynamicCloudDensity);
			Shader.SetGlobalFloat ("_Azure_DynamicCloudDirection", Azure_DynamicCloudDirection);
			Shader.SetGlobalFloat ("_Azure_DynamicCloudSpeed", Azure_DynamicCloudSpeed);

			//Fog.
			Shader.SetGlobalFloat ("_Azure_FogDistance", Azure_FogDistance);
			Shader.SetGlobalFloat ("_Azure_FogScale", Azure_FogScale);
			Shader.SetGlobalFloat ("_Azure_FogExtinction", Azure_FogExtinction);
			Shader.SetGlobalFloat ("_Azure_FogMieDepth", Azure_FogMieDepth);

			Shader.SetGlobalFloat ("_Azure_GammaCorrection", Azure_GammaCorrection);
			break;

		//Curve based on Sun/Moon elevation.
		case 1:
			//Get Curves Value.
			//-------------------------------------------------------------------------------------------------------
			//Scattering.
			Azure_Rayleigh = Azure_RayleighCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_Mie = Azure_MieCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_Kr = Azure_KrCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_Km = Azure_KmCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_SunIntensity = Azure_SunIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_SunDiskSize = Azure_SunDiskSizeCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_SunDiskPropagation = Azure_SunDiskPropagationCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_Exposure = Azure_ExposureCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_SkyDarkness = Azure_SkyDarknessCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_NightIntensity = Azure_NightIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			//Night.
			Azure_StarfieldIntensity = Azure_StarfieldIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_MilkyWayIntensity = Azure_MilkyWayIntensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_MoonDiskSize = Azure_MoonDiskSizeCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveMoonElevation);
			Azure_MoonDiskBright = Azure_MoonDiskBrightCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_MoonDiskBrightRange = Azure_MoonDiskBrightRangeCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_MoonSkyBright = Azure_MoonSkyBrightCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_MoonSkyBrightRange = Azure_MoonSkyBrightRangeCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			//Cloud.
			Azure_StaticCloudMultiplier = Azure_StaticCloudMultiplierCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_DynamicCloudDensity   = Azure_DynamicCloudDensityCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			//Fog.
			Azure_FogDistance = Azure_FogDistanceCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_FogScale = Azure_FogScaleCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);
			Azure_FogExtinction = Azure_FogExtinctionCurveE [Azure_DayOfWeek].Evaluate (Azure_GetCurveSunElevation);

			//Scattering.
			Shader.SetGlobalFloat ("_Azure_Pi316", Azure_Pi316);
			Shader.SetGlobalVector ("_Azure_Br", Azure_Br * Azure_Rayleigh);
			Shader.SetGlobalVector ("_Azure_Bm", Azure_Bm * Azure_Mie);
			Shader.SetGlobalVector ("_Azure_MieG", Azure_MieG);
			Shader.SetGlobalFloat ("_Azure_Pi316", Azure_Pi316);
			Shader.SetGlobalFloat ("_Azure_Pi14", Azure_Pi14);
			Shader.SetGlobalFloat ("_Azure_Kr", Azure_Kr);
			Shader.SetGlobalFloat ("_Azure_Km", Azure_Km);
			Shader.SetGlobalFloat ("_Azure_Pi", Mathf.PI);
			Shader.SetGlobalFloat ("_Azure_SunIntensity", Azure_SunIntensity);
			Shader.SetGlobalColor ("_Azure_RayleighColor", Azure_RayleighGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalColor ("_Azure_MieColor", Azure_MieGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalFloat ("_Azure_LightSpeed", Azure_SunMoonLightSpeed);
			Shader.SetGlobalFloat ("_Azure_SunDiskSize", Azure_SunDiskSize);
			Shader.SetGlobalFloat ("_Azure_SunDiskPropagation", Azure_SunDiskPropagation);
			Shader.SetGlobalFloat ("_Azure_Exposure", Azure_Exposure);
			Shader.SetGlobalFloat ("_Azure_SkyDarkness", Azure_SkyDarkness);
			Shader.SetGlobalFloat ("_Azure_NightIntensity", Azure_NightIntensity);
			//Night.
			Shader.SetGlobalFloat ("_Azure_StarfieldIntensity", Azure_StarfieldIntensity);
			Shader.SetGlobalFloat ("_Azure_MilkyWayIntensity", Azure_MilkyWayIntensity);
			Shader.SetGlobalFloat ("_Azure_MoonDiskSize", Azure_MoonDiskSize + Azure_FixMoonSize);
			Shader.SetGlobalColor ("_Azure_MoonDiskColor", Azure_MoonDiskGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalFloat ("_Azure_MoonDiskBright", 1.0f - Azure_MoonDiskBright);
			Shader.SetGlobalFloat ("_Azure_MoonDiskBrightRange", Azure_MoonDiskBrightRange);
			Shader.SetGlobalFloat ("_Azure_MoonSkyBright", 1.0f - Azure_MoonSkyBright);
			Shader.SetGlobalFloat ("_Azure_MoonSkyBrightRange", Azure_MoonSkyBrightRange);
			Shader.SetGlobalColor ("_Azure_MoonSkyBrightColor", Azure_MoonSkyBrightGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			//3D Moon sphere.
			Shader.SetGlobalFloat ("_Azure_MoonSpherePenunbra", Azure_MoonPenumbra);
			Shader.SetGlobalFloat ("_Azure_MoonSphereShadow", Azure_MoonShadow);
			Shader.SetGlobalFloat ("_Azure_MoonSphereSaturation", Azure_MoonSaturation);
			//Cloud.
			Shader.SetGlobalColor ("_Azure_StaticCloudColor1", Azure_StaticCloudEdgeGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalColor ("_Azure_StaticCloudColor2", Azure_StaticCloudDensityGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalFloat ("_Azure_StaticCloudMultiplier", Azure_StaticCloudMultiplier);
			Shader.SetGlobalColor ("_Azure_DynamicCloudColor1", Azure_DynamicCloudEdgeGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalColor ("_Azure_DynamicCloudColor2", Azure_DynamicCloudDensityGradientColorE [Azure_DayOfWeek].Evaluate (Azure_GetGradientSunElevation));
			Shader.SetGlobalFloat ("_Azure_DynamicCloudCovarage", Azure_DynamicCloudDensity);
			Shader.SetGlobalFloat ("_Azure_DynamicCloudDirection", Azure_DynamicCloudDirection);
			Shader.SetGlobalFloat ("_Azure_DynamicCloudSpeed", Azure_DynamicCloudSpeed);

			//Fog.
			Shader.SetGlobalFloat ("_Azure_FogDistance", Azure_FogDistance);
			Shader.SetGlobalFloat ("_Azure_FogScale", Azure_FogScale);
			Shader.SetGlobalFloat ("_Azure_FogExtinction", Azure_FogExtinction);
			Shader.SetGlobalFloat ("_Azure_FogMieDepth", Azure_FogMieDepth);

			Shader.SetGlobalFloat ("_Azure_GammaCorrection", Azure_GammaCorrection);
			break;
		}
	}

	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Changes cloud mode.
	/// </summary>
	public void AzureSetCloudMode()
	{
		switch(Azure_CloudMode)
		{
		//No Clouds.
		case 0:
			switch(Azure_ShaderMode)
			{
			case 0://Vertex shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Vertex/Sky");
				break;
			case 1://Pixel shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Pixel/Sky");
				break;
			}
			break;

		//2D Static Clouds.
		case 1:
			switch(Azure_ShaderMode)
			{
			case 0://Vertex shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Vertex/Sky Static Clouds");
				break;
			case 1://Pixel shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Pixel/Sky Static Clouds");
				break;
			}
			break;
		//2D Dynamic Clouds.
		case 2:
			switch(Azure_ShaderMode)
			{
			case 0://Vertex shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Vertex/Sky Dynamic Clouds");
				break;
			case 1://Pixel shader.
				Azure_SkyMaterial.shader = Shader.Find("Azure[Sky]/Pixel/Sky Dynamic Clouds");
				break;
			}
			break;
		}
	}

	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Changes fog mode.
	/// </summary>
	public void AzureSetFogMode()
	{
		switch (Azure_FogMode) {
		case 0:
			Azure_FogMaterial.shader = Shader.Find("Azure[Sky]/Realistic Fog Scattering");
			break;

		case 1:
			Azure_FogMaterial.shader = Shader.Find("Azure[Sky]/Projected Fog Scattering");
			break;
		}
	}

	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Sets sky mode between "Skydome" and "Skybox".
	/// </summary>
	public void AzureSetSkyMode()
	{
		switch(Azure_SkyMode)
		{
		case 0://Skydome.
			RenderSettings.skybox = null;
			Azure_Skydome.gameObject.SetActive (true);
			Azure_CullMode = 1;
			Azure_FixMoonSize = 0.0f;
			break;
		case 1://Skybox.
			RenderSettings.skybox = Azure_SkyMaterial;
			Azure_Skydome.gameObject.SetActive (false);
			Azure_CullMode = 2;
			Azure_FixMoonSize = 0.25f;
			break;
		}
	}

	//-------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Create moon render texture.
	/// </summary>
//	void AzureCreateMoonRenderTexture(int renderSize) {
//		Azure_MoonTexture = new RenderTexture(renderSize, renderSize, 0, RenderTextureFormat.ARGBHalf);
//		Azure_MoonTexture.wrapMode   = TextureWrapMode.Clamp;
//		Azure_MoonTexture.anisoLevel = 0;
//		Azure_MoonTexture.name       = "Moon Texture";
//	}
	#endregion
}
