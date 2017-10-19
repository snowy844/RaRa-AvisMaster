using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditorInternal;

[CustomEditor(typeof(AzureSkyController))]
public class AzureSkyControllerEditor : Editor
{
	private string[] TimeMode      = new string[]{"Simple", "Realistic"};
	private string[] CurveMode     = new string[]{"Timeline", "Elevation"};
	private string[] AmbientSource = new string[]{"Skybox", "Gradient", "Color"};
	private string[] ShaderMode    = new string[]{"Vertex", "Pixel"};
	private string[] CloudMode     = new string[]{"No Clouds", "2D Static Clouds", "2D Dynamic Clouds"};
	private string[] SkyMode       = new string[]{"Skydome", "Skybox"};
	private string[] FogMode       = new string[]{"Realistic", "Projected"};
	private string[] ComputeMode   = new string[]{"Pre-Computed", "Realtime"};

	private Texture2D logoTex, tab;
	private int tabSize = 1;
	private int labelWidth = 114;
	private Color col1 = new Color(1,1,1,1);//Normal.
	private Color col2 = new Color(0,0,0,0);//All Transparent.
	private Color col3 = new Color(0.35f,0.65f,1,1);//Blue.
	private Color col4 = new Color(0.15f,0.5f,1,0.35f);//Blue semi transparent.
	private Color col5 = new Color(0.75f, 1.0f, 0.75f, 1.0f);//Green;
	private Color col6 = new Color(1.0f, 0.5f, 0.5f, 1.0f);//Red;
	private Color curveColor = Color.yellow;

	private string installPath;
	private string inspectorGUIPath;
	private Rect   bgRect;
	private float  curveValueWidth = 50;

	//Days of week.
	private string Sunday    ="Sanday";
	private string Monday    ="M";
	private string Tuesday   ="T";
	private string Wednesday ="W";
	private string Thursday  ="T";
	private string Friday    ="F";
	private string Saturday  ="S";
	private int    Day;
	private string[] month = new string[]{
		"January",
		"February",
		"March",
		"April",
		"May",
		"June",
		"July",
		"August",
		"September",
		"October",
		"November",
		"December"
	};
	private string[] day = new string[]{
		"Sunday",
		"Monday",
		"Tuesday",
		"Wednesday",
		"Thursday",
		"Friday",
		"Saturday"
	};
	private Vector2 hours;

	//Show/Hide strings.
	private string ShowHideTimeOfDay;
	private string ShowHideObjectsAndMaterial;
	private string ShowHideScattering;
	private string ShowHideNightSky;
	private string ShowHideCloud;
	private string ShowHideFog;
	private string ShowHideLighting;
	private string ShowHideOptions;
	private string ShowHideOutputs;

	//Gradient Colors.
	SerializedProperty RaySunColor;
	SerializedProperty RaySunColorE;
	SerializedProperty MieSunColor;
	SerializedProperty MieSunColorE;
	SerializedProperty MoonDiskColor;
	SerializedProperty MoonDiskColorE;
	SerializedProperty MoonSkyBrightColor;
	SerializedProperty MoonSkyBrightColorE;
	SerializedProperty SunLightColor;
	SerializedProperty SunLightColorE;
	SerializedProperty MoonLightColor;
	SerializedProperty MoonLightColorE;
	SerializedProperty AmbientColor;
	SerializedProperty AmbientColorE;
	SerializedProperty EquatorColor;
	SerializedProperty EquatorColorE;
	SerializedProperty GroundColor;
	SerializedProperty GroundColorE;
	SerializedProperty StaticCloudEdgeColor;
	SerializedProperty StaticCloudEdgeColorE;
	SerializedProperty StaticCloudDensityColor;
	SerializedProperty StaticCloudDensityColorE;
	SerializedProperty DynamicCloudEdgeColor;
	SerializedProperty DynamicCloudEdgeColorE;
	SerializedProperty DynamicCloudDensityColor;
	SerializedProperty DynamicCloudDensityColorE;
	SerializedProperty SkyColor;
	SerializedProperty SkyColorE;

	//Outputs.
	private ReorderableList    reorderableCurveList;
	private ReorderableList    reorderableGradientList;
	private SerializedProperty serializedCurve;
	private SerializedProperty serializedGradient;

	void OnEnable()
	{
		string scriptLocation = AssetDatabase.GetAssetPath (MonoScript.FromScriptableObject (this));
		installPath           = scriptLocation.Replace ("/Editor/AzureSkyControllerEditor.cs", "");
		inspectorGUIPath      = installPath + "/Editor/InspectorGUI";

		//Gradient Color Serialize.
		//-------------------------------------------------------------------------------------------------------
		RaySunColor   = serializedObject.FindProperty("Azure_RayleighGradientColor");
		RaySunColorE  = serializedObject.FindProperty("Azure_RayleighGradientColorE");
		MieSunColor   = serializedObject.FindProperty("Azure_MieGradientColor");
		MieSunColorE  = serializedObject.FindProperty("Azure_MieGradientColorE");
		SkyColor  = serializedObject.FindProperty("Azure_SkyColorGradientColor");
		SkyColorE = serializedObject.FindProperty("Azure_SkyColorGradientColorE");
		MoonDiskColor = serializedObject.FindProperty("Azure_MoonDiskGradientColor");
		MoonDiskColorE= serializedObject.FindProperty("Azure_MoonDiskGradientColorE");
		MoonSkyBrightColor  = serializedObject.FindProperty("Azure_MoonSkyBrightGradientColor");
		MoonSkyBrightColorE = serializedObject.FindProperty("Azure_MoonSkyBrightGradientColorE");
		SunLightColor   = serializedObject.FindProperty("Azure_SunLightGradientColor");
		SunLightColorE  = serializedObject.FindProperty("Azure_SunLightGradientColorE");
		MoonLightColor  = serializedObject.FindProperty("Azure_MoonLightGradientColor");
		MoonLightColorE = serializedObject.FindProperty("Azure_MoonLightGradientColorE");
		AmbientColor  = serializedObject.FindProperty("Azure_UnityAmbientGradientColor");
		AmbientColorE = serializedObject.FindProperty("Azure_UnityAmbientGradientColorE");
		EquatorColor  = serializedObject.FindProperty("Azure_UnityEquatorGradientColor");
		EquatorColorE = serializedObject.FindProperty("Azure_UnityEquatorGradientColorE");
		GroundColor   = serializedObject.FindProperty("Azure_UnityGroundGradientColor");
		GroundColorE  = serializedObject.FindProperty("Azure_UnityGroundGradientColorE");
		//clouds.
		StaticCloudEdgeColor     = serializedObject.FindProperty("Azure_StaticCloudEdgeGradientColor");
		StaticCloudEdgeColorE    = serializedObject.FindProperty("Azure_StaticCloudEdgeGradientColorE");
		StaticCloudDensityColor  = serializedObject.FindProperty("Azure_StaticCloudDensityGradientColor");
		StaticCloudDensityColorE = serializedObject.FindProperty("Azure_StaticCloudDensityGradientColorE");
		DynamicCloudEdgeColor     = serializedObject.FindProperty("Azure_DynamicCloudEdgeGradientColor");
		DynamicCloudEdgeColorE    = serializedObject.FindProperty("Azure_DynamicCloudEdgeGradientColorE");
		DynamicCloudDensityColor  = serializedObject.FindProperty("Azure_DynamicCloudDensityGradientColor");
		DynamicCloudDensityColorE = serializedObject.FindProperty("Azure_DynamicCloudDensityGradientColorE");

		//Create Curve Outputs.
		//-------------------------------------------------------------------------------------------------------
		serializedCurve      = serializedObject.FindProperty ("Azure_OutputCurveList");
		reorderableCurveList = new ReorderableList (serializedObject, serializedCurve, false, true, true, true);
		reorderableCurveList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			rect.y += 2;
			EditorGUI.LabelField(rect, "element index " + index.ToString());
			EditorGUI.PropertyField(new Rect (rect.x+100, rect.y, rect.width-100, EditorGUIUtility.singleLineHeight), serializedCurve.GetArrayElementAtIndex(index), GUIContent.none);
		};
		reorderableCurveList.onAddCallback = (ReorderableList l) =>
		{
			var index = l.serializedProperty.arraySize;
			l.serializedProperty.arraySize++;
			l.index = index;
			serializedCurve.GetArrayElementAtIndex(index).animationCurveValue = AnimationCurve.Linear(0,0,24,0);
		};
		reorderableCurveList.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Curve Output", EditorStyles.boldLabel);
			//EditorGUI.LabelField(new Rect (rect.x+90, rect.y, rect.width, rect.height), "(returns a Float)", EditorStyles.miniBoldLabel);
		};
		reorderableCurveList.drawElementBackgroundCallback = (rect, index, active, focused) => {
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, col4);
			tex.Apply ();
			if (active)
				GUI.DrawTexture (rect, tex as Texture);
		};

		//Create Gradient Outputs.
		//-------------------------------------------------------------------------------------------------------
		serializedGradient      = serializedObject.FindProperty ("Azure_OutputGradientList");
		reorderableGradientList = new ReorderableList (serializedObject, serializedGradient, false, true, true, true);
		reorderableGradientList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			rect.y += 2;
			EditorGUI.LabelField(rect, "element index " + index.ToString());
			EditorGUI.PropertyField(new Rect (rect.x+100, rect.y, rect.width-100, EditorGUIUtility.singleLineHeight), serializedGradient.GetArrayElementAtIndex(index), GUIContent.none);
		};
		reorderableGradientList.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Gradient Output", EditorStyles.boldLabel);
			//EditorGUI.LabelField(new Rect (rect.x+107, rect.y, rect.width, rect.height), "(returns a Color)", EditorStyles.miniBoldLabel);
		};
		reorderableGradientList.drawElementBackgroundCallback = (rect, index, active, focused) => {
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, col4);
			tex.Apply ();
			if (active)
				GUI.DrawTexture (rect, tex as Texture);
		};
	}

	public override void OnInspectorGUI()
	{
		//Get target
		//-------------------------------------------------------------------------------------------------------
		AzureSkyController Target = (AzureSkyController)target;
		Undo.RecordObject (Target, "Undo Azure Sky Lite Properties");
		serializedObject.Update ();
		curveColor = Target.Azure_CurveColorField;
		Day = Target.Azure_DayOfWeek;

		//Show and Hide text.
		//-------------------------------------------------------------------------------------------------------
		if (Target.Azure_ShowTimeOfDayTab) ShowHideTimeOfDay = "| Hide"; else ShowHideTimeOfDay = "| Show";
		if (Target.Azure_ShowObjectsAndMaterialsTab) ShowHideObjectsAndMaterial = "| Hide"; else ShowHideObjectsAndMaterial = "| Show";
		if (Target.Azure_ShowScatteringTab) ShowHideScattering = "| Hide"; else ShowHideScattering = "| Show";
		if (Target.Azure_ShowNightSkyTab) ShowHideNightSky = "| Hide"; else ShowHideNightSky = "| Show";
		if (Target.Azure_ShowLightingTab) ShowHideLighting = "| Hide"; else ShowHideLighting = "| Show";
		if (Target.Azure_ShowCloudTab) ShowHideCloud = "| Hide"; else ShowHideCloud = "| Show";
		if (Target.Azure_ShowFogTab) ShowHideFog = "| Hide"; else ShowHideFog = "| Show";
		if (Target.Azure_ShowOptionsTab) ShowHideOptions = "| Hide"; else ShowHideOptions = "| Show";
		if (Target.Azure_ShowOutputsTab) ShowHideOutputs = "| Hide"; else ShowHideOutputs = "| Show";

		//Set text of the Days buttons.
		//-------------------------------------------------------------------------------------------------------
		if (Target.Azure_DayOfWeek == 0) Sunday    = "Sunday";	    else Sunday    = "S";
		if (Target.Azure_DayOfWeek == 1) Monday    = "Monday";	    else Monday    = "M";
		if (Target.Azure_DayOfWeek == 2) Tuesday   = "Tuesday";	    else Tuesday   = "T";
		if (Target.Azure_DayOfWeek == 3) Wednesday = "Wednesday";	else Wednesday = "W";
		if (Target.Azure_DayOfWeek == 4) Thursday  = "Thursday";	else Thursday  = "T";
		if (Target.Azure_DayOfWeek == 5) Friday    = "Friday";	    else Friday    = "F";
		if (Target.Azure_DayOfWeek == 6) Saturday  = "Saturday";	else Saturday  = "S";

		//Get Textures.
		//-------------------------------------------------------------------------------------------------------
		logoTex = AssetDatabase.LoadAssetAtPath (inspectorGUIPath + "/AzureSkyLogo2.png", typeof (Texture2D))as Texture2D;
		tab     = AssetDatabase.LoadAssetAtPath (inspectorGUIPath + "/InspectorTab.png", typeof (Texture2D))as Texture2D;
		EditorGUILayout.Space ();

		//Logo.
		//-------------------------------------------------------------------------------------------------------
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.DrawTexture (new Rect (bgRect.x,bgRect.y, 245,30), logoTex);
		EditorGUILayout.Space ();
		GUILayout.Label ("Version 4.0.1", EditorStyles.miniLabel);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-21, bgRect.width, 2), tab);

		#region Time of Day Tab
		//Time of Day Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowTimeOfDayTab = !Target.Azure_ShowTimeOfDayTab;
		GUI.color = col1;
		Target.Azure_ShowTimeOfDayTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowTimeOfDayTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "TIME OF DAY", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideTimeOfDay);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);

		if (Target.Azure_ShowTimeOfDayTab)
		{
			GUI.color = col3;//Set blue color.
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button (Sunday)) {
				Target.Azure_DayOfWeek = 0;
			}
			if (Target.Azure_NumberOfDays < 2)
				GUI.color = col4;
			if (GUILayout.Button (Monday)) {
				Target.Azure_DayOfWeek = 1;
			}
			if (Target.Azure_NumberOfDays < 3)
				GUI.color = col4;
			if (GUILayout.Button (Tuesday)) {
				Target.Azure_DayOfWeek = 2;
			}
			if (Target.Azure_NumberOfDays < 4)
				GUI.color = col4;
			if (GUILayout.Button (Wednesday)) {
				Target.Azure_DayOfWeek = 3;
			}
			if (Target.Azure_NumberOfDays < 5)
				GUI.color = col4;
			if (GUILayout.Button (Thursday)) {
				Target.Azure_DayOfWeek = 4;
			}
			if (Target.Azure_NumberOfDays < 6)
				GUI.color = col4;
			if (GUILayout.Button (Friday)) {
				Target.Azure_DayOfWeek = 5;
			}
			if (Target.Azure_NumberOfDays < 7)
				GUI.color = col4;
			if (GUILayout.Button (Saturday)) {
				Target.Azure_DayOfWeek = 6;
			}
			GUI.color = col1;
			EditorGUILayout.EndHorizontal ();

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
			EditorGUILayout.Space ();
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Time Mode", GUILayout.Width(labelWidth));
			Target.Azure_TimeMode = EditorGUILayout.Popup(Target.Azure_TimeMode, TimeMode);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Curve Mode", GUILayout.Width(labelWidth));
			Target.Azure_CurveMode = EditorGUILayout.Popup(Target.Azure_CurveMode, CurveMode);
			EditorGUILayout.EndHorizontal ();

			if(Target.Azure_CurveMode == 0 && Target.Azure_TimeMode == 1)
			{
				EditorGUILayout.HelpBox("To avoid potential problems with synchronizing the position of the sun and moon relative to the timeline, consider switching the curve mode to \"Elevation\" when using realistic time mode.",MessageType.Warning);
			}

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

			switch(Target.Azure_TimeMode)
			{
			case 0:
				//Timeline.
				EditorGUILayout.BeginHorizontal ();
				//GUILayout.Label ("Timeline", GUILayout.Width(55));
				GUILayout.Label ("Timeline", GUILayout.Width(labelWidth));
				Target.Azure_Timeline = EditorGUILayout.Slider (Target.Azure_Timeline, 0, 24);
				EditorGUILayout.EndHorizontal ();
				//Latitude.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Latitude", GUILayout.Width(labelWidth));
				Target.Azure_Latitude = EditorGUILayout.IntSlider (Target.Azure_Latitude, -90, 90);
				EditorGUILayout.EndHorizontal ();
				//Longitude.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Longitude", GUILayout.Width(labelWidth));
				Target.Azure_Longitude = EditorGUILayout.IntSlider (Target.Azure_Longitude, -180, 180);
				EditorGUILayout.EndHorizontal ();
				//Nº of Days.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Nº of Days", GUILayout.Width(labelWidth));
				Target.Azure_NumberOfDays = EditorGUILayout.IntSlider (Target.Azure_NumberOfDays, 1, 7);
				EditorGUILayout.EndHorizontal ();
				//Day Cycle.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Day Cycle in Minutes");
				Target.Azure_DayCycle = EditorGUILayout.FloatField(Target.Azure_DayCycle, GUILayout.Width(50));
				if(Target.Azure_DayCycle < 0.0f){Target.Azure_DayCycle = 0.0f;}
				EditorGUILayout.EndHorizontal ();

				//Time Curve.
				GUI.color = col4;
				EditorGUILayout.BeginVertical ("Box");
				GUI.color = col1;
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.Space();
				GUILayout.Label ("Day and Night Length", EditorStyles.boldLabel);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				//Set Time by Curve?
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Set Time of Day by Curve?");
				Target.Azure_SetTimeByCurve = EditorGUILayout.Toggle (Target.Azure_SetTimeByCurve, GUILayout.Width(15));
				EditorGUILayout.EndHorizontal ();
				//Day and Night Length Curve Field.
				EditorGUILayout.BeginHorizontal ();
				GUI.color = col3;
				if (GUILayout.Button ("R", GUILayout.Width(25), GUILayout.Height(25))) { Target.Azure_DayNightLengthCurve = AnimationCurve.Linear (0, 0, 24, 24); }
				GUI.color = col1;
				Target.Azure_DayNightLengthCurve = EditorGUILayout.CurveField (Target.Azure_DayNightLengthCurve, curveColor, new Rect(0,0,24,24), GUILayout.Height(25));
				EditorGUILayout.EndHorizontal ();
				//Show Current Time of Day by Curve.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Current Time by Curve:");
				GUILayout.TextField (Target.Azure_TimeOfDayByCurve.ToString(), GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();

				hours = Target.AzureGetHourAndMinutes();

				GUILayout.Label (day[Target.Azure_DayOfWeek] + " " + hours.x.ToString("00") +":" + hours.y.ToString("00"));
				break;

			case 1:
				//Date.
				GUILayout.Label ("DATE:");
				//Hour.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Timeline", GUILayout.Width(labelWidth));
				Target.Azure_Timeline = EditorGUILayout.Slider (Target.Azure_Timeline, 0, 24);
				EditorGUILayout.EndHorizontal ();
				//Minute.
				//EditorGUILayout.BeginHorizontal ();
				//GUILayout.Label ("Minute", GUILayout.Width(labelWidth));
				//Target.Azure_Minute = EditorGUILayout.IntSlider (Target.Azure_Minute, 0, 59);
				//EditorGUILayout.EndHorizontal ();
				//Day.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Day", GUILayout.Width(labelWidth));
				Target.Azure_Day = EditorGUILayout.IntSlider (Target.Azure_Day, 1, Target.Azure_MaxDayMonth);
				EditorGUILayout.EndHorizontal ();
				//Month.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Month", GUILayout.Width(labelWidth));
				Target.Azure_Month = EditorGUILayout.IntSlider (Target.Azure_Month, 1, 12);
				EditorGUILayout.EndHorizontal ();
				//Year.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Year", GUILayout.Width(labelWidth));
				Target.Azure_Year = EditorGUILayout.IntSlider (Target.Azure_Year, 0000, 9999);
				EditorGUILayout.EndHorizontal ();
				//Nº of Days.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Nº of Days", GUILayout.Width(labelWidth));
				Target.Azure_NumberOfDays = EditorGUILayout.IntSlider (Target.Azure_NumberOfDays, 1, 7);
				EditorGUILayout.EndHorizontal ();
				//Time and Date buttons.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Start at Current Time System?");
				Target.Azure_StartAtCurrentTime = EditorGUILayout.Toggle ( Target.Azure_StartAtCurrentTime, GUILayout.Width(15));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Start at Current Date System?");
				Target.Azure_StartAtCurrentDate = EditorGUILayout.Toggle ( Target.Azure_StartAtCurrentDate, GUILayout.Width(15));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if(GUILayout.Button("Get Current Time"))
				{
					Target.AzureGetCurrentTime();
				}
				if(GUILayout.Button("Get Current Date"))
				{
					Target.AzureGetCurrentDate();
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
				//Location.
				GUILayout.Label ("LOCATION:");
				//Latitude.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Latitude", GUILayout.Width(labelWidth));
				Target.Azure_Latitude = EditorGUILayout.IntSlider (Target.Azure_Latitude, -90, 90);
				EditorGUILayout.EndHorizontal ();
				//Longitude.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Longitude", GUILayout.Width(labelWidth));
				Target.Azure_Longitude = EditorGUILayout.IntSlider (Target.Azure_Longitude, -180, 180);
				EditorGUILayout.EndHorizontal ();
				//UTC.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("UTC", GUILayout.Width(labelWidth));
				Target.Azure_UTC = EditorGUILayout.IntSlider (Target.Azure_UTC, -12, 12);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
				//Length.
				GUILayout.Label ("LENGTH:");
				//Day Cycle.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Day Cycle in Minutes");
				Target.Azure_DayCycle = EditorGUILayout.FloatField(Target.Azure_DayCycle, GUILayout.Width(50));
				EditorGUILayout.EndHorizontal ();

				//Time Curve.
				GUI.color = col4;
				EditorGUILayout.BeginVertical ("Box");
				GUI.color = col1;
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.Space();
				GUILayout.Label ("Day and Night Length", EditorStyles.boldLabel);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				//Set Time by Curve?
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Set Time of Day by Curve?");
				Target.Azure_SetTimeByCurve = EditorGUILayout.Toggle (Target.Azure_SetTimeByCurve, GUILayout.Width(15));
				EditorGUILayout.EndHorizontal ();
				//Day and Night Length Curve Field.
				EditorGUILayout.BeginHorizontal ();
				GUI.color = col3;
				if (GUILayout.Button ("R", GUILayout.Width(25), GUILayout.Height(25))) { Target.Azure_DayNightLengthCurve = AnimationCurve.Linear (0, 0, 24, 24); }
				GUI.color = col1;
				Target.Azure_DayNightLengthCurve = EditorGUILayout.CurveField (Target.Azure_DayNightLengthCurve, curveColor, new Rect(0,0,24,24), GUILayout.Height(25));
				EditorGUILayout.EndHorizontal ();
				//Show Current Time of Day by Curve.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Current Time by Curve:");
				GUILayout.TextField (Target.Azure_TimeOfDayByCurve.ToString(), GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (month[Target.Azure_Month -1] + " " + Target.Azure_Day.ToString()+", " + Target.Azure_Year.ToString());

				hours = Target.AzureGetHourAndMinutes();

				GUILayout.Label (day[Target.Azure_DayOfWeek] + " " + hours.x.ToString("00") +":" + hours.y.ToString("00"));
				EditorGUILayout.EndHorizontal ();
				break;
			}
				
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-3, bgRect.width, 2), tab);
		}
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		#endregion

		#region Objects & Materials Tab
		//Objects & Materials Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowObjectsAndMaterialsTab = !Target.Azure_ShowObjectsAndMaterialsTab;
		GUI.color = col1;
		Target.Azure_ShowObjectsAndMaterialsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowObjectsAndMaterialsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "OBJECTS & MATERIALS", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideObjectsAndMaterial);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();
		if (Target.Azure_ShowObjectsAndMaterialsTab)
		{
			GUI.color = col5;
			if (!Target.Azure_SunDirectionalLight) {
				GUI.color = col6;
				//Debug.LogError ("The sun directional light is missing! Please attach a directional light to 'Sun Dir Light' propertie in OBJECTS & MATERIALS tab.");
			}
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Sun Light", GUILayout.Width(labelWidth));
			Target.Azure_SunDirectionalLight  =  (Transform)EditorGUILayout.ObjectField (Target.Azure_SunDirectionalLight, typeof(Transform), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

			GUI.color = col5;
			if(!Target.Azure_MoonDirectionalLight)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Moon Light", GUILayout.Width(labelWidth));
			Target.Azure_MoonDirectionalLight  =  (Transform)EditorGUILayout.ObjectField (Target.Azure_MoonDirectionalLight, typeof(Transform), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

			GUI.color = col5;
			if(!Target.Azure_Skydome)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Skydome", GUILayout.Width(labelWidth));
			Target.Azure_Skydome  =  (Transform)EditorGUILayout.ObjectField (Target.Azure_Skydome, typeof(Transform), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

			GUI.color = col5;
			if(!Target.Azure_SkyMaterial)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Sky Material", GUILayout.Width(labelWidth));
			Target.Azure_SkyMaterial  =  (Material)EditorGUILayout.ObjectField (Target.Azure_SkyMaterial, typeof(Material), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

			GUI.color = col5;
			if(!Target.Azure_FogMaterial)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Fog Material", GUILayout.Width(labelWidth));
			Target.Azure_FogMaterial  =  (Material)EditorGUILayout.ObjectField (Target.Azure_FogMaterial, typeof(Material), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

//			GUI.color = col5;
//			if(!Target.Azure_MoonMaterial)
//				GUI.color = col6;
//			EditorGUILayout.BeginHorizontal ();
//			GUILayout.Label("Moon Material", GUILayout.Width(labelWidth));
//			Target.Azure_MoonMaterial  =  (Material)EditorGUILayout.ObjectField (Target.Azure_MoonMaterial, typeof(Material), true);
//			EditorGUILayout.EndHorizontal ();
//			GUI.color = col1;

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Scattering Tab
		//Scattering Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowScatteringTab = !Target.Azure_ShowScatteringTab;
		GUI.color = col1;
		Target.Azure_ShowScatteringTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowScatteringTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "SCATTERING", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideScattering);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if (Target.Azure_ShowScatteringTab)
		{
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Compute Mode", GUILayout.Width(labelWidth));
			Target.Azure_ComputeMode = EditorGUILayout.Popup(Target.Azure_ComputeMode, ComputeMode);
			EditorGUILayout.EndHorizontal ();
			switch(Target.Azure_CurveMode)
			{
			//Curve Based on Timeline.
			case 0:
					if(Target.Azure_ComputeMode == 1)
					{
						//Rayleigh Color.
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("Wavelength", GUILayout.Width(labelWidth));
					    EditorGUILayout.PropertyField(SkyColor.GetArrayElementAtIndex(Day), GUIContent.none);
						EditorGUILayout.EndHorizontal ();
					}
					//PHYSICS.
					//GUILayout.Label ("PHYSICS:");
					//Rayleigh.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Rayleigh", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_RayleighCurve[Day] = AnimationCurve.Linear(0.0f,1.0f,24.0f,1.0f); }
					Target.Azure_RayleighCurve[Day] = EditorGUILayout.CurveField (Target.Azure_RayleighCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,5.0f));
					GUILayout.TextField (Target.Azure_Rayleigh.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Mie.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MieCurve[Day] = AnimationCurve.Linear(0.0f,1.0f,24.0f,1.0f); }
					Target.Azure_MieCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MieCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,30.0f));
					GUILayout.TextField (Target.Azure_Mie.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Kr.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Kr", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_KrCurve[Day] = AnimationCurve.Linear(0.0f,8.4f,24.0f,8.4f); }
					Target.Azure_KrCurve[Day] = EditorGUILayout.CurveField (Target.Azure_KrCurve[Day], curveColor, new Rect(0.0f,1.0f,24.0f,29.0f));
					GUILayout.TextField (Target.Azure_Kr.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Km.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Km", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_KmCurve[Day] = AnimationCurve.Linear(0.0f,1.25f,24.0f,1.25f); }
					Target.Azure_KmCurve[Day] = EditorGUILayout.CurveField (Target.Azure_KmCurve[Day], curveColor, new Rect(0.0f,1.0f,24.0f,29.0f));
					GUILayout.TextField (Target.Azure_Km.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Sun Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sun Intensity", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunIntensityCurve[Day] = AnimationCurve.Linear(0.0f,15.0f,24.0f,15.0f); }
					Target.Azure_SunIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_SunIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,75.0f));
					GUILayout.TextField (Target.Azure_SunIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Rayleigh Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Rayleigh Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(RaySunColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Mie Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(MieSunColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Light Speed.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Light Speed", GUILayout.Width(labelWidth));
					Target.Azure_SunMoonLightSpeed = EditorGUILayout.Slider (Target.Azure_SunMoonLightSpeed, 0, 100);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();

					//DAY/NIGHT.
					//GUILayout.Label ("DAY/NIGHT:");
					//Darkness.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Day Darkness", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SkyDarknessCurve[Day] = AnimationCurve.Linear(0.0f,0.0f,24.0f,0.0f); }
					Target.Azure_SkyDarknessCurve[Day] = EditorGUILayout.CurveField (Target.Azure_SkyDarknessCurve[Day], curveColor, new Rect(0.0f,-1.0f,24.0f,6.0f));
					GUILayout.TextField (Target.Azure_SkyDarkness.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Night Intensity", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_NightIntensityCurve[Day] = AnimationCurve.Linear(0.0f,0.5f,24.0f,0.5f); }
					Target.Azure_NightIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_NightIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_NightIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Exposure.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Exposure", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_ExposureCurve[Day] = AnimationCurve.Linear(0.0f,2.0f,24.0f,2.0f); }
					Target.Azure_ExposureCurve[Day] = EditorGUILayout.CurveField (Target.Azure_ExposureCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,10.0f));
					GUILayout.TextField (Target.Azure_Exposure.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();

					//SUN DISK.
					GUILayout.Label ("Sun Disk:");
					//Sun Disk size.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Size", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunDiskSizeCurve[Day] = AnimationCurve.Linear(0.0f,500.0f,24.0f,500.0f); }
					Target.Azure_SunDiskSizeCurve[Day] = EditorGUILayout.CurveField (Target.Azure_SunDiskSizeCurve[Day], curveColor, new Rect(0.0f,25.0f,24.0f,975));
					GUILayout.TextField (Target.Azure_SunDiskSize.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Sun Disk Propagation.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Propagation", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunDiskPropagationCurve[Day] = AnimationCurve.Linear(0.0f,5.0f,24.0f,5.0f); }
					Target.Azure_SunDiskPropagationCurve[Day] = EditorGUILayout.CurveField (Target.Azure_SunDiskPropagationCurve[Day], curveColor, new Rect(0.0f,1.0f,24.0f,14));
					GUILayout.TextField (Target.Azure_SunDiskPropagation.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
				break;

			//Curve Based on Elevation.
			case 1:
				if(Target.Azure_ComputeMode == 1)
				{
					//Rayleigh Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Wavelength", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(SkyColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
				}
				//PHYSICS.
				//GUILayout.Label ("PHYSICS:");
				//Rayleigh.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Rayleigh", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_RayleighCurveE[Day] = AnimationCurve.Linear(-1.0f,1.0f,1.0f,1.0f); }
				Target.Azure_RayleighCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_RayleighCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,5.0f));
				GUILayout.TextField (Target.Azure_Rayleigh.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Mie.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Mie", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MieCurveE[Day] = AnimationCurve.Linear(-1.0f,1.0f,1.0f,1.0f); }
				Target.Azure_MieCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MieCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,30.0f));
				GUILayout.TextField (Target.Azure_Mie.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Kr.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Kr", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_KrCurveE[Day] = AnimationCurve.Linear(-1.0f,8.4f,1.0f,8.4f); }
				Target.Azure_KrCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_KrCurveE[Day], curveColor, new Rect(-1.0f,1.0f,2.0f,29.0f));
				GUILayout.TextField (Target.Azure_Kr.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Km.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Km", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_KmCurveE[Day] = AnimationCurve.Linear(-1.0f,1.25f,1.0f,1.25f); }
				Target.Azure_KmCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_KmCurveE[Day], curveColor, new Rect(-1.0f,1.0f,2.0f,29.0f));
				GUILayout.TextField (Target.Azure_Km.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Sun Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Sun Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,15.0f,1.0f,15.0f); }
				Target.Azure_SunIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_SunIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,75.0f));
				GUILayout.TextField (Target.Azure_SunIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Rayleigh Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Rayleigh Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(RaySunColorE.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				//Mie Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Mie Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(MieSunColorE.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				//Light Speed.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Light Speed", GUILayout.Width(labelWidth));
				Target.Azure_SunMoonLightSpeed = EditorGUILayout.Slider (Target.Azure_SunMoonLightSpeed, 0, 100);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();

				//DAY/NIGHT.
				//GUILayout.Label ("DAY/NIGHT:");
				//Darkness.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Day Darkness", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SkyDarknessCurveE[Day] = AnimationCurve.Linear(-1.0f,0.0f,1.0f,0.0f); }
				Target.Azure_SkyDarknessCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_SkyDarknessCurveE[Day], curveColor, new Rect(-1.0f,-1.0f,2.0f,6.0f));
				GUILayout.TextField (Target.Azure_SkyDarkness.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Night Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_NightIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.5f,1.0f,0.5f); }
				Target.Azure_NightIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_NightIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
				GUILayout.TextField (Target.Azure_NightIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Exposure.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Exposure", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_ExposureCurveE[Day] = AnimationCurve.Linear(-1.0f,2.0f,1.0f,2.0f); }
				Target.Azure_ExposureCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_ExposureCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,10.0f));
				GUILayout.TextField (Target.Azure_Exposure.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();

				//SUN DISK.
				GUILayout.Label ("Sun Disk:");
				//Sun Disk size.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Size", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunDiskSizeCurveE[Day] = AnimationCurve.Linear(-1.0f,500.0f,1.0f,500.0f); }
				Target.Azure_SunDiskSizeCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_SunDiskSizeCurveE[Day], curveColor, new Rect(-1.0f,25.0f,2.0f,975));
				GUILayout.TextField (Target.Azure_SunDiskSize.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Sun Disk Propagation.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Propagation", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunDiskPropagationCurveE[Day] = AnimationCurve.Linear(-1.0f,5.0f,1.0f,5.0f); }
				Target.Azure_SunDiskPropagationCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_SunDiskPropagationCurveE[Day], curveColor, new Rect(-1.0f,1.0f,2.0f,14));
				GUILayout.TextField (Target.Azure_SunDiskPropagation.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				break;
			}

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space();
		#endregion

		#region Night Tab
		//Night Sky Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowNightSkyTab = !Target.Azure_ShowNightSkyTab;
		GUI.color = col1;
		Target.Azure_ShowNightSkyTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowNightSkyTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "NIGHT", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideNightSky);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowNightSkyTab)
		{
			switch(Target.Azure_CurveMode)
			{
			//Curve Based on Timeline.
			case 0:
					//STARFIELD.
					GUILayout.Label ("STARFIELD:");
					GUI.color = col5;
					if(!Target.Azure_StarfieldTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Starfield Cubemap", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldTexture  =  (Cubemap)EditorGUILayout.ObjectField (Target.Azure_StarfieldTexture, typeof(Cubemap), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Noise Cubemap.
					GUI.color = col5;
					if(!Target.Azure_StarNoiseTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Noise Cubemap", GUILayout.Width(labelWidth));
					Target.Azure_StarNoiseTexture  =  (Cubemap)EditorGUILayout.ObjectField (Target.Azure_StarNoiseTexture, typeof(Cubemap), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Stars Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Stars", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_StarfieldIntensityCurve[Day] = AnimationCurve.Linear(0.0f,0.0f,24.0f,0.0f); }
					Target.Azure_StarfieldIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_StarfieldIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,10.0f));
					GUILayout.TextField (Target.Azure_StarfieldIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Milky Way Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Milky Way", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MilkyWayIntensityCurve[Day] = AnimationCurve.Linear(0.0f,0.0f,24.0f,0.0f); }
					Target.Azure_MilkyWayIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MilkyWayIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_MilkyWayIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Stars Scintillation.
					EditorGUILayout.BeginHorizontal ();
					//GUILayout.Label ("Timeline", GUILayout.Width(55));
					GUILayout.Label ("Scintillation", GUILayout.Width(labelWidth));
					Target.Azure_StarsScintillation = EditorGUILayout.Slider (Target.Azure_StarsScintillation, 0, 20);
					EditorGUILayout.EndHorizontal ();

					//Color Balance.
					//GUILayout.Label ("Color Balance:");
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance R", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.x = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.x, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance G", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.y = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.y, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance B", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.z = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.z, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position X", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.x = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.x, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position Y", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.y = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.y, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position Z", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.z = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.z, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
					//MOON.
					GUILayout.Label ("MOON:");
					GUI.color = col5;
					if(!Target.Azure_MoonTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture Render/2D", GUILayout.Width(labelWidth));
					Target.Azure_MoonTexture  =  (Texture)EditorGUILayout.ObjectField (Target.Azure_MoonTexture, typeof(Texture), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					EditorGUILayout.Space ();
					//Moon Disk Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Disk Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(MoonDiskColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Monn Disk Size.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Disk Size", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskSizeCurve[Day] = AnimationCurve.Linear(0.0f,0.5f,24.0f,0.5f); }
					Target.Azure_MoonDiskSizeCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskSizeCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_MoonDiskSize.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Disk Bright.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shine Emission", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskBrightCurve[Day] = AnimationCurve.Linear(0.0f,0.15f,24.0f,0.15f); }
					Target.Azure_MoonDiskBrightCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskBrightCurve[Day], curveColor, new Rect(0.0f,-10.0f,24.0f,11.0f));
					GUILayout.TextField (Target.Azure_MoonDiskBright.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Disk Bright Range.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shine Range", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskBrightRangeCurve[Day] = AnimationCurve.Linear(0.0f,200.0f,24.0f,200.0f); }
					Target.Azure_MoonDiskBrightRangeCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskBrightRangeCurve[Day], curveColor, new Rect(0.0f,100.0f,24.0f,300.0f));
					GUILayout.TextField (Target.Azure_MoonDiskBrightRange.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					//Moon Sky Bright Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Bright Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(MoonSkyBrightColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Moon Sky Bright.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Bright", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonSkyBrightCurve[Day] = AnimationCurve.Linear(0.0f,0.15f,24.0f,0.15f); }
					Target.Azure_MoonSkyBrightCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonSkyBrightCurve[Day], curveColor, new Rect(0.0f,-10.0f,24.0f,11.0f));
					GUILayout.TextField (Target.Azure_MoonSkyBright.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Sky Bright Range.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Bright Range", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonSkyBrightRangeCurve[Day] = AnimationCurve.Linear(0.0f,50.0f,24.0f,50.0f); }
					Target.Azure_MoonSkyBrightRangeCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonSkyBrightRangeCurve[Day], curveColor, new Rect(0.0f,10.0f,24.0f,90.0f));
					GUILayout.TextField (Target.Azure_MoonSkyBrightRange.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

					//3D MOON SPHERE.
					GUILayout.Label ("3D MOON SPHERE:");
					//Moon Penumbra.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Penumbra", GUILayout.Width(labelWidth));
					Target.Azure_MoonPenumbra = EditorGUILayout.Slider (Target.Azure_MoonPenumbra, 0, 4);
					EditorGUILayout.EndHorizontal ();
					//Moon Shadow.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shadow", GUILayout.Width(labelWidth));
					Target.Azure_MoonShadow = EditorGUILayout.Slider (Target.Azure_MoonShadow, 0, 0.25f);
					EditorGUILayout.EndHorizontal ();
					//Moon Penumbra.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Saturation", GUILayout.Width(labelWidth));
					Target.Azure_MoonSaturation = EditorGUILayout.Slider (Target.Azure_MoonSaturation, 0.5f, 2);
					EditorGUILayout.EndHorizontal ();
				break;

			//Curve Based on sun/moon elevation.
			case 1:
					//STARFIELD.
					GUILayout.Label ("STARFIELD:");
					GUI.color = col5;
					if(!Target.Azure_StarfieldTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Starfield Cubemap", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldTexture  =  (Cubemap)EditorGUILayout.ObjectField (Target.Azure_StarfieldTexture, typeof(Cubemap), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Noise Cubemap.
					GUI.color = col5;
					if(!Target.Azure_StarNoiseTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Noise Cubemap", GUILayout.Width(labelWidth));
					Target.Azure_StarNoiseTexture  =  (Cubemap)EditorGUILayout.ObjectField (Target.Azure_StarNoiseTexture, typeof(Cubemap), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Stars Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Stars", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_StarfieldIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.0f,1.0f,0.0f); }
					Target.Azure_StarfieldIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_StarfieldIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,10.0f));
					GUILayout.TextField (Target.Azure_StarfieldIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Milky Way Intensity.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Milky Way", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MilkyWayIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.0f,1.0f,0.0f); }
					Target.Azure_MilkyWayIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MilkyWayIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
					GUILayout.TextField (Target.Azure_MilkyWayIntensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Stars Scintillation.
					EditorGUILayout.BeginHorizontal ();
					//GUILayout.Label ("Timeline", GUILayout.Width(55));
					GUILayout.Label ("Scintillation", GUILayout.Width(labelWidth));
					Target.Azure_StarsScintillation = EditorGUILayout.Slider (Target.Azure_StarsScintillation, 0, 20);
					EditorGUILayout.EndHorizontal ();

					//Color Balance.
					//GUILayout.Label ("Color Balance:");
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance R", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.x = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.x, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance G", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.y = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.y, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Color Balance B", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldColorBalance.z = EditorGUILayout.Slider (Target.Azure_StarfieldColorBalance.z, 1.0f, 2.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position X", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.x = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.x, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position Y", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.y = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.y, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Position Z", GUILayout.Width(labelWidth));
					Target.Azure_StarfieldPosition.z = EditorGUILayout.Slider (Target.Azure_StarfieldPosition.z, 0.0f, 360.0f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);
					//MOON.
					GUILayout.Label ("MOON:");
					GUI.color = col5;
					if(!Target.Azure_MoonTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture Render/2D", GUILayout.Width(labelWidth));
					Target.Azure_MoonTexture  =  (Texture)EditorGUILayout.ObjectField (Target.Azure_MoonTexture, typeof(Texture), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					EditorGUILayout.Space ();
					//Moon Disk Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Disk Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(MoonDiskColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Monn Disk Size.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Disk Size", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskSizeCurveE[Day] = AnimationCurve.Linear(-1.0f,0.5f,1.0f,0.5f); }
					Target.Azure_MoonDiskSizeCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskSizeCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
					GUILayout.TextField (Target.Azure_MoonDiskSize.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Disk Bright.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shine Emission", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskBrightCurveE[Day] = AnimationCurve.Linear(-1.0f,0.15f,1.0f,0.15f); }
					Target.Azure_MoonDiskBrightCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskBrightCurveE[Day], curveColor, new Rect(-1.0f,-10.0f,2.0f,11.0f));
					GUILayout.TextField (Target.Azure_MoonDiskBright.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Disk Bright Range.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shine Range", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonDiskBrightRangeCurveE[Day] = AnimationCurve.Linear(-1.0f,200.0f,1.0f,200.0f); }
					Target.Azure_MoonDiskBrightRangeCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonDiskBrightRangeCurveE[Day], curveColor, new Rect(-1.0f,100.0f,2.0f,300.0f));
					GUILayout.TextField (Target.Azure_MoonDiskBrightRange.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					//Moon Sky Bright Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Bright Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(MoonSkyBrightColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Moon Sky Bright.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Bright", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonSkyBrightCurveE[Day] = AnimationCurve.Linear(-1.0f,0.15f,1.0f,0.15f); }
					Target.Azure_MoonSkyBrightCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonSkyBrightCurveE[Day], curveColor, new Rect(-1.0f,-10.0f,2.0f,11.0f));
					GUILayout.TextField (Target.Azure_MoonSkyBright.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Moon Sky Bright Range.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Bright Range", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonSkyBrightRangeCurveE[Day] = AnimationCurve.Linear(-1.0f,50.0f,1.0f,50.0f); }
					Target.Azure_MoonSkyBrightRangeCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonSkyBrightRangeCurveE[Day], curveColor, new Rect(-1.0f,10.0f,2.0f,90.0f));
					GUILayout.TextField (Target.Azure_MoonSkyBrightRange.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					EditorGUILayout.Space ();
					GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

					//3D MOON SPHERE.
					GUILayout.Label ("3D MOON SPHERE:");
					//Moon Penumbra.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Penumbra", GUILayout.Width(labelWidth));
					Target.Azure_MoonPenumbra = EditorGUILayout.Slider (Target.Azure_MoonPenumbra, 0, 4);
					EditorGUILayout.EndHorizontal ();
					//Moon Shadow.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Shadow", GUILayout.Width(labelWidth));
					Target.Azure_MoonShadow = EditorGUILayout.Slider (Target.Azure_MoonShadow, 0, 0.25f);
					EditorGUILayout.EndHorizontal ();
					//Moon Penumbra.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Saturation", GUILayout.Width(labelWidth));
					Target.Azure_MoonSaturation = EditorGUILayout.Slider (Target.Azure_MoonSaturation, 0.5f, 2);
					EditorGUILayout.EndHorizontal ();
					break;
			}

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Cloud Tab
		//Cloud Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowCloudTab = !Target.Azure_ShowCloudTab;
		GUI.color = col1;
		Target.Azure_ShowCloudTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowCloudTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "CLOUD", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideCloud);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowCloudTab)
		{
			switch(Target.Azure_CurveMode)
			{
			//Curve Based on Timeline.
			case 0:
				//Cloud Mode.
				//0 = No Clouds;
				//1 = Cheap static clouds;
				//2 = Advenced static clouds;
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Cloud Mode", GUILayout.Width(labelWidth));
				Target.Azure_CloudMode = EditorGUILayout.Popup(Target.Azure_CloudMode, CloudMode);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_CloudMode)
				{
				case 1:
					//Static Cloud Texture.
					//GUILayout.Label ("Static Cloud Texture:");
					GUI.color = col5;
					if(!Target.Azure_StaticCloudTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture2D", GUILayout.Width(labelWidth));
					Target.Azure_StaticCloudTexture  =  (Texture2D)EditorGUILayout.ObjectField (Target.Azure_StaticCloudTexture, typeof(Texture2D), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Edge Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Edge Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(StaticCloudEdgeColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Density Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(StaticCloudDensityColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Cloud Multiplier.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Multiplier", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_StaticCloudMultiplierCurve[Day] = AnimationCurve.Linear(0.0f,1.0f,24.0f,1.0f); }
					Target.Azure_StaticCloudMultiplierCurve[Day] = EditorGUILayout.CurveField (Target.Azure_StaticCloudMultiplierCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,2.5f));
					GUILayout.TextField (Target.Azure_StaticCloudMultiplier.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					break;
				case 2:
					GUI.color = col5;
					if(!Target.Azure_DynamicCloudTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture2D", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudTexture  =  (Texture2D)EditorGUILayout.ObjectField (Target.Azure_DynamicCloudTexture, typeof(Texture2D), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Edge Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Edge Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(DynamicCloudEdgeColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Density Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(DynamicCloudDensityColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Cloud Density.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_DynamicCloudDensityCurve[Day] = AnimationCurve.Linear(0.0f,0.75f,24.0f,0.75f); }
					Target.Azure_DynamicCloudDensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_DynamicCloudDensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,5.0f));
					GUILayout.TextField (Target.Azure_DynamicCloudDensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Cloud Direction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Direction", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudDirection = EditorGUILayout.Slider (Target.Azure_DynamicCloudDirection, -3, 3);
					EditorGUILayout.EndHorizontal ();
					//Cloud Speed.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Speed", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudSpeed = EditorGUILayout.Slider (Target.Azure_DynamicCloudSpeed, 0, 0.5f);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;

			//Curve Based on sun/moon elevation.
			case 1:
				//Cloud Mode.
				//0 = No Clouds;
				//1 = Cheap static clouds;
				//2 = Advenced static clouds;
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Cloud Mode", GUILayout.Width(labelWidth));
				Target.Azure_CloudMode = EditorGUILayout.Popup(Target.Azure_CloudMode, CloudMode);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_CloudMode)
				{
				case 1:
					//Static Cloud Texture.
					//GUILayout.Label ("Static Cloud Texture:");
					GUI.color = col5;
					if(!Target.Azure_StaticCloudTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture2D", GUILayout.Width(labelWidth));
					Target.Azure_StaticCloudTexture  =  (Texture2D)EditorGUILayout.ObjectField (Target.Azure_StaticCloudTexture, typeof(Texture2D), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Edge Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Edge Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(StaticCloudEdgeColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Density Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(StaticCloudDensityColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Cloud Multiplier.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Multiplier", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_StaticCloudMultiplierCurveE[Day] = AnimationCurve.Linear(-1.0f,1.0f,1.0f,1.0f); }
					Target.Azure_StaticCloudMultiplierCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_StaticCloudMultiplierCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,2.5f));
					GUILayout.TextField (Target.Azure_StaticCloudMultiplier.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					break;
				case 2:
					GUI.color = col5;
					if(!Target.Azure_DynamicCloudTexture)
						GUI.color = col6;
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label("Texture2D", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudTexture  =  (Texture2D)EditorGUILayout.ObjectField (Target.Azure_DynamicCloudTexture, typeof(Texture2D), true);
					EditorGUILayout.EndHorizontal ();
					GUI.color = col1;
					//Edge Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Edge Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(DynamicCloudEdgeColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Density Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(DynamicCloudDensityColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Cloud Density.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Density", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_DynamicCloudDensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.75f,1.0f,0.75f); }
					Target.Azure_DynamicCloudDensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_DynamicCloudDensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,5.0f));
					GUILayout.TextField (Target.Azure_DynamicCloudDensity.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Cloud Direction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Direction", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudDirection = EditorGUILayout.Slider (Target.Azure_DynamicCloudDirection, -3, 3);
					EditorGUILayout.EndHorizontal ();
					//Cloud Speed.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Speed", GUILayout.Width(labelWidth));
					Target.Azure_DynamicCloudSpeed = EditorGUILayout.Slider (Target.Azure_DynamicCloudSpeed, 0, 0.5f);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;
			}
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Fog Tab
		//Fog Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowFogTab = !Target.Azure_ShowFogTab;
		GUI.color = col1;
		Target.Azure_ShowFogTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowFogTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "FOG", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideFog);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowFogTab)
		{
			switch(Target.Azure_CurveMode)
			{
			//Curve Based on Timeline.
			case 0:
				//Fog Mode.
				//0 = Realistic.
				//1 = Projected.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Fog Mode", GUILayout.Width(labelWidth));
				Target.Azure_FogMode = EditorGUILayout.Popup(Target.Azure_FogMode, FogMode);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_FogMode)
				{
				case 0:
					//Distance.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Distance", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogDistanceCurve[Day] = AnimationCurve.Linear(0.0f,75.0f,24.0f,75.0f); }
					Target.Azure_FogDistanceCurve[Day] = EditorGUILayout.CurveField (Target.Azure_FogDistanceCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,10000.0f));
					GUILayout.TextField (Target.Azure_FogDistance.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Scale.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Scale", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogScaleCurve[Day] = AnimationCurve.Linear(0.0f,0.75f,24.0f,0.75f); }
					Target.Azure_FogScaleCurve[Day] = EditorGUILayout.CurveField (Target.Azure_FogScaleCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogScale.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Extinction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Extinction", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogExtinctionCurve[Day] = AnimationCurve.Linear(0.0f,0.15f,24.0f,0.15f); }
					Target.Azure_FogExtinctionCurve[Day] = EditorGUILayout.CurveField (Target.Azure_FogExtinctionCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogExtinction.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Mie depth.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie Depth", GUILayout.Width(labelWidth));
					Target.Azure_FogMieDepth = EditorGUILayout.Slider (Target.Azure_FogMieDepth, 0.0f, 1.0f);
					EditorGUILayout.EndHorizontal ();
					break;
				case 1:
					//Distance.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Distance", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogDistanceCurve[Day] = AnimationCurve.Linear(0.0f,75.0f,24.0f,75.0f); }
					Target.Azure_FogDistanceCurve[Day] = EditorGUILayout.CurveField (Target.Azure_FogDistanceCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,10000.0f));
					GUILayout.TextField (Target.Azure_FogDistance.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Extinction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Extinction", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogExtinctionCurve[Day] = AnimationCurve.Linear(0.0f,0.15f,24.0f,0.15f); }
					Target.Azure_FogExtinctionCurve[Day] = EditorGUILayout.CurveField (Target.Azure_FogExtinctionCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogExtinction.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Mie depth.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie Depth", GUILayout.Width(labelWidth));
					Target.Azure_FogMieDepth = EditorGUILayout.Slider (Target.Azure_FogMieDepth, 0.0f, 1.0f);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;
			//Curve Based on Timeline.
			case 1:
				//Fog Mode.
				//0 = Realistic.
				//1 = Projected.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Fog Mode", GUILayout.Width(labelWidth));
				Target.Azure_FogMode = EditorGUILayout.Popup(Target.Azure_FogMode, FogMode);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_FogMode)
				{
				case 0:
					//Distance.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Distance", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogDistanceCurveE[Day] = AnimationCurve.Linear(-1.0f,75.0f,1.0f,75.0f); }
					Target.Azure_FogDistanceCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_FogDistanceCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,10000.0f));
					GUILayout.TextField (Target.Azure_FogDistance.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Scale.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Scale", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogScaleCurveE[Day] = AnimationCurve.Linear(-1.0f,0.75f,1.0f,0.75f); }
					Target.Azure_FogScaleCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_FogScaleCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogScale.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Extinction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Extinction", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogExtinctionCurveE[Day] = AnimationCurve.Linear(-1.0f,0.15f,1.0f,0.15f); }
					Target.Azure_FogExtinctionCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_FogExtinctionCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogExtinction.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Mie depth.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie Depth", GUILayout.Width(labelWidth));
					Target.Azure_FogMieDepth = EditorGUILayout.Slider (Target.Azure_FogMieDepth, 0.0f, 1.0f);
					EditorGUILayout.EndHorizontal ();
					break;

				case 1:
					//Distance.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Distance", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogDistanceCurveE[Day] = AnimationCurve.Linear(-1.0f,75.0f,1.0f,75.0f); }
					Target.Azure_FogDistanceCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_FogDistanceCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,10000.0f));
					GUILayout.TextField (Target.Azure_FogDistance.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Extinction.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Extinction", GUILayout.Width(labelWidth-23));
					if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_FogExtinctionCurveE[Day] = AnimationCurve.Linear(-1.0f,0.15f,1.0f,0.15f); }
					Target.Azure_FogExtinctionCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_FogExtinctionCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,1.0f));
					GUILayout.TextField (Target.Azure_FogExtinction.ToString(), GUILayout.Width (curveValueWidth));
					EditorGUILayout.EndHorizontal ();
					//Mie depth.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Mie Depth", GUILayout.Width(labelWidth));
					Target.Azure_FogMieDepth = EditorGUILayout.Slider (Target.Azure_FogMieDepth, 0.0f, 1.0f);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;
			}

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Lightong Tab
		//Lighting Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowLightingTab = !Target.Azure_ShowLightingTab;
		GUI.color = col1;
		Target.Azure_ShowLightingTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowLightingTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "LIGHTING", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideLighting);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowLightingTab)
		{
			switch(Target.Azure_CurveMode)
			{
			//Curve Based on Timeline.
			case 0:
				GUILayout.Label ("SUN LIGHT:");
				//Sun Light Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunLightIntensityCurve[Day] = AnimationCurve.Linear(0.0f,0.0f,24.0f,0.0f); }
				Target.Azure_SunLightIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_SunLightIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,8.0f));
				GUILayout.TextField (Target.Azure_SunLightIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Sun Light Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(SunLightColor.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

				GUILayout.Label ("MOON LIGHT:");
				//Moon Light Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonLightIntensityCurve[Day] = AnimationCurve.Linear(0.0f,0.0f,24.0f,0.0f); }
				Target.Azure_MoonLightIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_MoonLightIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,8.0f));
				GUILayout.TextField (Target.Azure_MoonLightIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Moon Light Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(MoonLightColor.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

				GUILayout.Label ("AMBIENT:");
				//Ambient source.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Source:", GUILayout.Width(labelWidth));
				Target.Azure_UnityAmbientSource = EditorGUILayout.Popup(Target.Azure_UnityAmbientSource, AmbientSource);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_UnityAmbientSource)
				{
				case 0:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
					//Ambient Intensity.
					if(UnityEngine.RenderSettings.skybox != null)
					{
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
						if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_AmbientIntensityCurve[Day] = AnimationCurve.Linear(0.0f,01.0f,24.0f,1.0f); }
						Target.Azure_AmbientIntensityCurve[Day] = EditorGUILayout.CurveField (Target.Azure_AmbientIntensityCurve[Day], curveColor, new Rect(0.0f,0.0f,24.0f,8.0f));
						GUILayout.TextField (Target.Azure_AmbientIntensity.ToString(), GUILayout.Width (curveValueWidth));
						EditorGUILayout.EndHorizontal ();
					}
					else
						{
						EditorGUILayout.HelpBox("Please! Set \"Sky Mode\" to Skybox in the Options tab if you intend to use the ambient source as skybox.",MessageType.Info );
						}
					break;

				case 1:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
					//Ambient Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(AmbientColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Equator Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Equator Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(EquatorColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Ground Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Ground Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(GroundColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					break;

				case 2:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
					//Ambient Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Ambient Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(AmbientColor.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;

			//Curve Based on sun/moon elevation.
			case 1:
				GUILayout.Label ("SUN LIGHT:");
				//Sun Light Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_SunLightIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.0f,1.0f,0.0f); }
				Target.Azure_SunLightIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_SunLightIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,8.0f));
				GUILayout.TextField (Target.Azure_SunLightIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Sun Light Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(SunLightColorE.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

				GUILayout.Label ("MOON LIGHT:");
				//Moon Light Intensity.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
				if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_MoonLightIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,0.0f,1.0f,0.0f); }
				Target.Azure_MoonLightIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_MoonLightIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,8.0f));
				GUILayout.TextField (Target.Azure_MoonLightIntensity.ToString(), GUILayout.Width (curveValueWidth));
				EditorGUILayout.EndHorizontal ();
				//Moon Light Color.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Color", GUILayout.Width(labelWidth));
				EditorGUILayout.PropertyField(MoonLightColorE.GetArrayElementAtIndex(Day), GUIContent.none);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
				GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

				GUILayout.Label ("AMBIENT:");
				//Ambient source.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Source:", GUILayout.Width(labelWidth));
				Target.Azure_UnityAmbientSource = EditorGUILayout.Popup(Target.Azure_UnityAmbientSource, AmbientSource);
				EditorGUILayout.EndHorizontal ();
				switch(Target.Azure_UnityAmbientSource)
				{
				case 0:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
					//Ambient Intensity.
					if(UnityEngine.RenderSettings.skybox != null)
					{
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
						if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.Azure_AmbientIntensityCurveE[Day] = AnimationCurve.Linear(-1.0f,01.0f,1.0f,1.0f); }
						Target.Azure_AmbientIntensityCurveE[Day] = EditorGUILayout.CurveField (Target.Azure_AmbientIntensityCurveE[Day], curveColor, new Rect(-1.0f,0.0f,2.0f,8.0f));
						GUILayout.TextField (Target.Azure_AmbientIntensity.ToString(), GUILayout.Width (curveValueWidth));
						EditorGUILayout.EndHorizontal ();
					}
					else
						{
							EditorGUILayout.HelpBox("Please! Set \"Sky Mode\" to Skybox in the Options tab if you intend to use the ambient source as skybox.",MessageType.Info );
						}
					break;

				case 1:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
					//Ambient Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Sky Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(AmbientColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Equator Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Equator Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(EquatorColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					//Ground Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Ground Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(GroundColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					break;

				case 2:
					RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
					//Ambient Color.
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Ambient Color", GUILayout.Width(labelWidth));
					EditorGUILayout.PropertyField(AmbientColorE.GetArrayElementAtIndex(Day), GUIContent.none);
					EditorGUILayout.EndHorizontal ();
					break;
				}
				break;
			}
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Options Tab
		//Options Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowOptionsTab = !Target.Azure_ShowOptionsTab;
		GUI.color = col1;
		Target.Azure_ShowOptionsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowOptionsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "OPTIONS", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideOptions);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowOptionsTab)
		{
			//if(Target.Azure_SkyMode == 0)
			//{
			//GUI.color = col4;
			EditorGUILayout.BeginVertical ("Box");
			//GUI.color = col1;
			//Automatic Skydome Size?
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Automatic Skydome Size?");
			Target.Azure_AutomaticSkydomeSize = EditorGUILayout.Toggle (Target.Azure_AutomaticSkydomeSize, GUILayout.Width(15));
			EditorGUILayout.EndHorizontal ();
			if(!Target.Azure_AutomaticSkydomeSize)
			{
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Custom Skydome Size");
				Target.Azure_SkydomeSize = EditorGUILayout.FloatField(Target.Azure_SkydomeSize, GUILayout.Width(50));
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				//Size Percentage.
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Size Percentage", GUILayout.Width(labelWidth));
				Target.Azure_SkydomeSizePercentage = EditorGUILayout.Slider (Target.Azure_SkydomeSizePercentage, 50, 100);
				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.EndVertical();
			//}
			//Follow Main Camera?
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Follow Active Main Camera?");
			Target.Azure_FollowActiveMainCamera = EditorGUILayout.Toggle (Target.Azure_FollowActiveMainCamera, GUILayout.Width(15));
			EditorGUILayout.EndHorizontal ();
			//Auto Sun Intensity.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Automatic Sun Intensity?");
			Target.Azure_AutomaticSunIntensity = EditorGUILayout.Toggle (Target.Azure_AutomaticSunIntensity, GUILayout.Width(15));
			EditorGUILayout.EndHorizontal ();
			//Auto Moon Intensity.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Automatic Moon Intensity?");
			Target.Azure_AutomaticMoonIntensity = EditorGUILayout.Toggle (Target.Azure_AutomaticMoonIntensity, GUILayout.Width(15));
			EditorGUILayout.EndHorizontal ();
			//Gamma.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Gamma", GUILayout.Width(labelWidth));
			Target.Azure_GammaCorrection = EditorGUILayout.Slider (Target.Azure_GammaCorrection, 0.4545f, 2.2f);
			EditorGUILayout.EndHorizontal ();

			//Sky Render Side.
			//0=Two sides;
			//1=Front;
			//2=Back;
//			EditorGUILayout.BeginHorizontal ();
//			GUILayout.Label ("Render Side", GUILayout.Width(labelWidth));
//			Target.Azure_CullMode = EditorGUILayout.Popup(Target.Azure_CullMode, CullMode);
//			EditorGUILayout.EndHorizontal ();

			//Sky Mode.
			//0=Skydome;
			//1=Skybox;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Sky Mode", GUILayout.Width(labelWidth));
			Target.Azure_SkyMode = EditorGUILayout.Popup(Target.Azure_SkyMode, SkyMode);
			EditorGUILayout.EndHorizontal ();
			//Shader Mode.
			//0=Vertex;
			//1=Pixel;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Shader Mode", GUILayout.Width(labelWidth));
			Target.Azure_ShaderMode = EditorGUILayout.Popup(Target.Azure_ShaderMode, ShaderMode);
			EditorGUILayout.EndHorizontal ();
			//GUILayout.TextField (Target.Azure_CullMode.ToString(), GUILayout.Width (curveValueWidth));
			switch(Target.Azure_ShaderMode)
			{
			case 0:
				EditorGUILayout.HelpBox("Vertex Shader can produce some artifacts if used in low resolution meshes, consider using Pixel Shader if you are using Azure sky material as Unity's standard skybox.",MessageType.Info );
				break;
			case 1:
				EditorGUILayout.HelpBox("Pixel Shader requires more processing, consider using Vertex Shader along with a Skydome for better performance.",MessageType.Warning);
				break;
			}
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Outputs Tab
		//Outputs Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.Azure_ShowOutputsTab = !Target.Azure_ShowOutputsTab;
		GUI.color = col1;
		Target.Azure_ShowOutputsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.Azure_ShowOutputsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "OUTPUTS", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideOutputs);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.Azure_ShowOutputsTab)
		{
			EditorGUILayout.Space();
			reorderableCurveList.DoLayoutList();
			EditorGUILayout.Space();
			reorderableGradientList.DoLayoutList();
			EditorGUILayout.Space ();
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		}
		EditorGUILayout.Space ();
		#endregion

		//Update every frame.
		//-------------------------------------------------------------------------------------------------------
		Target.AzureSetCloudMode();
		Target.AzureSetFogMode ();
		Target.AzureSetSkyMode ();
		Shader.SetGlobalInt ("_Azure_CullMode", Target.Azure_CullMode);

		// Refresh the Inspector.
		//-------------------------------------------------------------------------------------------------------
		serializedObject.ApplyModifiedProperties();
		EditorUtility.SetDirty(target);
	}
}