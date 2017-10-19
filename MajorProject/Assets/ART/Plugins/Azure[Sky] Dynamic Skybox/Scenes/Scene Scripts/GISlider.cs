using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GISlider : MonoBehaviour {
	public AzureSkyController Azure;
	public Slider timeSlider;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		Azure.Azure_Timeline = timeSlider.value;
	}
}
