#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TerrainBlendMapGenerator))]
public class BlendMapGenerator_UI : Editor {

	public override void OnInspectorGUI(){
		TerrainBlendMapGenerator bgen = (TerrainBlendMapGenerator)target;
		if (bgen.showMessages) {
			if (bgen.gameObject.GetComponent<Terrain> () == null) {
				EditorGUILayout.HelpBox ("Terrain Missing!\nThis script must be attached to a terrain! \nClick the help icon to access the online manual.", MessageType.Error);
			} else if (bgen.blend_materials == null) {
				EditorGUILayout.HelpBox ("Materials Missing!\nYou have to assign the materials you want to blend with this terrain.\nMaterials must use one of the included terrain blending shaders!\nClick the help icon to access the online manual.", MessageType.Warning);
			} else if (bgen.blend_materials != null && bgen.blend_materials.Length == 0) {
				EditorGUILayout.HelpBox ("Materials Missing!\nYou have to assign the materials you want to blend with this terrain.\nMaterials must use one of the included terrain blending shaders!\nClick the help icon to access the online manual.", MessageType.Warning);
			} else { // if no errors
				EditorGUILayout.HelpBox ("Click \"create / update blendmap\" to refresh the blending maps\nClick the help icon to access the online manual.", MessageType.Info);
				if (bgen.automaticColorCorrection == true) {
					EditorGUILayout.HelpBox ("Automatic color correction is enabled. this will modify the \"Color Correction\" alpha of blending materials to fit best to the current color space. Disable, if you made manual changes.", MessageType.Info);
				}
			}
			if (bgen.blend_materials != null) {
				for (int ctm = 0; ctm < bgen.blend_materials.Length; ctm++) {
					if (bgen.blend_materials [ctm] == null) {
						EditorGUILayout.HelpBox ("Empty Material Slot!\nMaterial " + ctm.ToString () + " is not assigned!\nEmpty slots will be ignored.", MessageType.Info);
						break;
					}
				}
			}
		}
		DrawDefaultInspector ();
		//bgen.terraintexturepos = EditorGUILayout.Vector4Field("texture offset.x, offset.y + size.x, size.y", bgen.terraintexturepos);

		if(GUILayout.Button(new GUIContent("REFRESH","Click this button to refresh the blending with this terrain. This only affects materials from the \"blend materials\" list."))){
			bgen.create_blendmap();
		}

	}
}
#endif