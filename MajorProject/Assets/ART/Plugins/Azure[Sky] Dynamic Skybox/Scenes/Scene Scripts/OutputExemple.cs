using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutputExemple : MonoBehaviour
{
	//Drag in the Inspector the GameObject that contain AzureSkyController script.
	public AzureSkyController azureOutput;

	//To store the Light component
	private Light myLight;

	// Use this for initialization
	void Start ()
	{
		//Getting the Light component and save in this variable to use later.
		myLight = GetComponent<Light> ();
	}
	
	// Update is called once per frame
	void Update () {
		//curveMode = 0; Based on the Timeline.
		//curveMode = 1; Based on the Sun Elevation.
		//curveMode = 2; Based on the Moon Elevation.

		//Getting element 0 of "Curve Output" in "Azure Inspector" and applying to the Light.
		//This Curve Output will be based on the Moon Elevation. In the Azure Inspector is set in the Output curve to the light intensity show only when the moon is above in the sky.
		//Note that the curve output is set based on the elevation between -1 and +1.
		myLight.intensity = azureOutput.AzureGetCurveOutput(0,2);

		//Getting element 0 of "Color Output" in "Azure Inspector" and applying to the Light.
		//This Gradient Output will be based on the Timeline.
		myLight.color     = azureOutput.AzureGetGradientOutput(0,0);
	}
}
