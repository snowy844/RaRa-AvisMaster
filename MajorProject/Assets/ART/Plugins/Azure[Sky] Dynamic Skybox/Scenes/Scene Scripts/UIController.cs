using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.PostProcessing;

public class UIController : MonoBehaviour {
	public AzureSkyController Azure;
	//public Camera mainCamera;
	public Text date;
	public Text time;
	public Dropdown fogMode;
	public Dropdown cloudMode;
	public Dropdown timeMode;
	public Slider fogDistance;
	//public PostProcessingBehaviour postProcess;
	public Toggle postProcessToggle;
	public Slider timeSlider;
	public RectTransform selector;


	private string[] month = new string[]{"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
	private string[] day = new string[]{"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
	private Vector2 hours;
	private string minutes;



	//-------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		fogMode.onValueChanged.AddListener(delegate {FogModeChange(fogMode); });
		cloudMode.onValueChanged.AddListener(delegate {CloudModeChange(cloudMode); });
		timeMode.onValueChanged.AddListener(delegate {TimeModeChange(timeMode); });
	}
	
	// Update is called once per frame
	void Update () {
		GetTimeAndDate ();
		Shader.SetGlobalFloat ("_Azure_FogDistance", fogDistance.value);
		//postProcess.enabled = postProcessToggle.isOn;
		Azure.AzureSetTime (timeSlider.value, 0.0f);
	}




	//-------------------------------------------------------------------------------------------------------
	public void SetDay( int day)
	{
		Azure.Azure_DayOfWeek = day;
		if (Azure.Azure_DayOfWeek == 0)	selector.localPosition = new Vector3 (-89.25f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 1)	selector.localPosition = new Vector3 (-58.75f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 2)	selector.localPosition = new Vector3 (-29.5f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 3)	selector.localPosition = new Vector3 (-0.5f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 4)	selector.localPosition = new Vector3 (28.75f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 5)	selector.localPosition = new Vector3 (59.25f, 0.5f, 0);
		if (Azure.Azure_DayOfWeek == 6)	selector.localPosition = new Vector3 (88.75f, 0.5f, 0);
	}




	//-------------------------------------------------------------------------------------------------------
	public void GetTimeAndDate()
	{
		hours = Azure.AzureGetHourAndMinutes();

		date.text = month[Azure.Azure_Month -1] + " " + Azure.Azure_Day.ToString()+", " + Azure.Azure_Year.ToString();
		time.text = day[Azure.Azure_DayOfWeek] + " " + hours.x.ToString("00") +":" + hours.y.ToString("00");
	}




	//-------------------------------------------------------------------------------------------------------
	private void FogModeChange(Dropdown target)
	{
		Azure.Azure_FogMode = target.value;
		Azure.AzureSetFogMode ();
	}
	private void CloudModeChange(Dropdown target)
	{
		Azure.Azure_CloudMode = target.value;
		Azure.AzureSetCloudMode ();
	}
	private void TimeModeChange(Dropdown target)
	{
		Azure.Azure_TimeMode = target.value;
		if (target.value == 0) {
			Azure.Azure_Longitude = 180;
			date.enabled = false;
		}
		if (target.value == 1) {
			Azure.Azure_Longitude = 0;
			date.enabled = true;
		}
	}
}
