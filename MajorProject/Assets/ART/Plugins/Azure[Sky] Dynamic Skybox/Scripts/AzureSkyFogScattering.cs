//Based on Unity's GlobalFog.
using System;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
[AddComponentMenu("Azure[Sky]/Fog Scattering")]
public class AzureSkyFogScattering : MonoBehaviour
{
	public Material fogMaterial;


	//=======================================================================================================
	//-------------------------------------------------------------------------------------------------------
	//[ImageEffectOpaque]
	void OnRenderImage(RenderTexture source, RenderTexture destination) 
	{
		//-------------------------------------------------------------------------------------------------------
		GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
		if (fogMaterial==null)
		{
			Graphics.Blit (source, destination);
			#if UNITY_EDITOR
			Debug.Log("Warning. Apply the <b>AzureLite Fog Material</b> to ('AzureSkyLiteFogScattering') script in the Main Camera Inspector");
			#endif
			return;
		}
		//-------------------------------------------------------------------------------------------------------
		Camera    camera          = GetComponent<Camera>();
		Transform cameraTransform = camera.transform;

		Vector3[] frustumCorners = new Vector3[4];
		camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, camera.stereoActiveEye, frustumCorners);
		var bottomLeft  = cameraTransform.TransformVector(frustumCorners[0]);
		var topLeft     = cameraTransform.TransformVector(frustumCorners[1]);
		var topRight    = cameraTransform.TransformVector(frustumCorners[2]);
		var bottomRight = cameraTransform.TransformVector(frustumCorners[3]);

		Matrix4x4 frustumCornersArray = Matrix4x4.identity;
		frustumCornersArray.SetRow(0, bottomLeft);
		frustumCornersArray.SetRow(1, bottomRight);
		frustumCornersArray.SetRow(2, topLeft);
		frustumCornersArray.SetRow(3, topRight);

		fogMaterial.SetMatrix ("_FrustumCorners", frustumCornersArray);
		Graphics.Blit(source, destination, fogMaterial, 0);
	}
}
