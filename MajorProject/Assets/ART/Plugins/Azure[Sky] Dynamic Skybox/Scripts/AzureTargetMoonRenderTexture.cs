using UnityEngine;
using System.Collections;

public class AzureTargetMoonRenderTexture : MonoBehaviour {

	public RenderTexture MoonRenderRender;

	// Use this for initialization
	void Awake () {
		SetTargetTexture ();
	}

	public void SetTargetTexture() {
		GetComponent<Camera>().targetTexture = MoonRenderRender;
	}
}