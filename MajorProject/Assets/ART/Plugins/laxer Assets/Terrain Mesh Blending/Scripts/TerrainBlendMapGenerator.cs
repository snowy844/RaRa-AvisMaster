#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
[HelpURL("http://www.illusionloop.com/docs/terrainmeshblending")]
[ExecuteInEditMode]
public class TerrainBlendMapGenerator : MonoBehaviour {
	TerrainData terraindata;
	//public TerrainBlendData terrainblenddata;
	[Tooltip("Used internally. A texture containing the height of the terrain. Will be created automatically after clicking \"create / update blendmap\".")]
	public Texture2D blendmap;
	[Tooltip("Used internally. A texture containing the normals of the terrain. Will be created automatically after clicking \"create / update blendmap\".")]
	public Texture2D blendnormalmap;
	[Tooltip("Automatically refresh blendmaps, when terrain changed. (only triggered by heightmap changes)")]
	public bool autoRefresh = true;
	[Tooltip("This will set the color correction of all blending materials to match Linear or Gamma color space(after clicking refresh).")]
	public bool automaticColorCorrection = true; //manipulate color input of materials automatically
	[Tooltip("This will adjust texture scale, offset, normal map, smoothness and metallic(after clicking refresh), to match the terrains texture")]
	public bool automaticTextureData = true; //manipulate scale + offset input of materials automatically
	[Tooltip("Toggle error messages and warnings")]
	public bool showMessages = true;
	[Tooltip("Drag and drop materials that should blend with this terrain here. These materials must use one of the included terrain blending shaders (eg. Custom/TerrainMeshBlend/standard).")]
	public Material[] blend_materials;
	[Tooltip("Draw terrain normals and height information encoded to rgba")]
	[HideInInspector] public bool debug = false;
	[Tooltip("draw less normals")]
	[HideInInspector] public int debug_downscale = 1;

	double updateDelayTimer = 0;

	string[] supportedShaders = {"Custom/TerrainMeshBlend/standard",
		"Custom/TerrainMeshBlend/Diffuse simple",
		"Custom/TerrainMeshBlend/legacy Diffuse",
		"Custom/TerrainMeshBlend/legacy Diffuse overlay",
		"Custom/TerrainMeshBlend/legacy Specular",
		"Custom/TerrainMeshBlend/legacy Specular overlay",
		"Custom/TerrainMeshBlend/full standard",
		"Custom/TerrainMeshBlend/standard overlay",
		"Custom/TerrainMeshBlend/Toon"};

	void Update(){ // draw every frame
		if(debug)
		drawDebug();
		if(autoRefresh && updateDelayTimer != 0)
		AutoUpdateDelay ();
	}

	void OnTerrainChanged()
	{
		if(autoRefresh)
		updateDelayTimer = EditorApplication.timeSinceStartup;
	}

	void AutoUpdateDelay(){//update blendmap after a short time to reduce lag while painting
		if (updateDelayTimer != 0 && EditorApplication.timeSinceStartup - updateDelayTimer > 0.6f){
			updateDelayTimer = 0;
			create_blendmap();
		}
	}

	void drawDebug(){
		for (int ctx = 0; ctx < blendmap.width; ctx++) {
			for (int cty = 0; cty < blendmap.height; cty++) {
				if(ctx%debug_downscale == 0 && cty%debug_downscale == 0){
					Color np = blendnormalmap.GetPixel(ctx,cty);
					Color hp = blendmap.GetPixel(ctx,cty);
					float h = hp.r*1.0f+hp.g* 1f/255.0f+hp.b* 1f/65025.0f+hp.a* 1f/16581375.0f;
					Vector3 n = new Vector3(np.r*2f-1f,np.b*2f-1f,np.g*2f-1f);
					Debug.DrawRay(transform.position + new Vector3(ctx/(float)(blendmap.width-1)*terraindata.size.x,h*terraindata.size.y,cty/(float)(blendmap.height-1)*terraindata.size.z),n,new Color(hp.r,hp.g,hp.b));
				}
			}
		}
	}

	public void create_blendmap () {

		if(!blendmap)
			blendmap = new Texture2D(512,512);
		if(!blendnormalmap)
			blendnormalmap = new Texture2D(512,512);
		if (GetComponent<Terrain>() == null) {
			EditorUtility.DisplayDialog("Terrain missing", "Unable to get Terrain. Make sure this script is attached to a terrain. Make sure that terrain is set up properly.", "OK");
			return;
		}
		terraindata = GetComponent<Terrain>().terrainData;
		if (terraindata == null) {
			EditorUtility.DisplayDialog("TerrainData missing", "Unable to get TerrainData component of Terrain. Make sure this script is attached to a terrain. Make sure that terrain is set up properly.", "mKAY");
			return;
		}

		float[,] heightmap = terraindata.GetHeights(0,0,terraindata.heightmapWidth,terraindata.heightmapHeight);
		blendmap.Resize(terraindata.heightmapWidth,terraindata.heightmapHeight);
		blendnormalmap.Resize(terraindata.heightmapWidth,terraindata.heightmapHeight);
		for(int cty = 0; cty < blendmap.height;cty++){
			for(int ctx = 0; ctx < blendmap.width;ctx++){
				Color hgt = encode_float_rgba(heightmap[ctx,cty]);
				if(UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear){
					hgt.r = Mathf.LinearToGammaSpace(hgt.r);
					hgt.g = Mathf.LinearToGammaSpace(hgt.g);
					hgt.b = Mathf.LinearToGammaSpace(hgt.b);
					hgt.a = Mathf.LinearToGammaSpace(hgt.a);

				}
				blendmap.SetPixel(cty,ctx,hgt);
				Vector3 tn = terraindata.GetInterpolatedNormal((float)ctx/(float)blendmap.width,(float)cty/(float)blendmap.height);
				Color n = new Color(tn.x/ 2 + 0.5f,tn.z / 2 + 0.5f,tn.y/ 2 + 0.5f,1);
				if(UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear){
					n.r = Mathf.LinearToGammaSpace(n.r);
					n.g = Mathf.LinearToGammaSpace(n.g);
					n.b = Mathf.LinearToGammaSpace(n.b);
					n.a = Mathf.LinearToGammaSpace(n.a);
					
				}
				blendnormalmap.SetPixel(ctx,cty,n);
			}
		}
		blendmap.Apply();
		blendnormalmap.Apply();

		if (AssetDatabase.GetAssetPath (blendmap)!= "") {
			EditorUtility.SetDirty(blendmap);//AssetDatabase.CreateAsset (blendmap, AssetDatabase.GetAssetPath (blendmap));
		} else {
			AssetDatabase.CreateAsset(blendmap, AssetDatabase.GenerateUniqueAssetPath("Assets/TerrainBlendMap.asset"));
		}
		if (AssetDatabase.GetAssetPath(blendnormalmap)!= "") {
			EditorUtility.SetDirty(blendnormalmap);//AssetDatabase.CreateAsset(blendnormalmap, AssetDatabase.GetAssetPath(blendnormalmap));
		} else {
			AssetDatabase.CreateAsset(blendnormalmap, AssetDatabase.GenerateUniqueAssetPath("Assets/TerrainBlendNormalMap.asset"));
		}
		for(int ctm = 0;ctm < blend_materials.Length;ctm++){
			if(blend_materials[ctm] != null){
				CheckShader(blend_materials[ctm]); // check if the material uses a correct shader
				if(blend_materials[ctm].HasProperty("_Blendmap")){
					blend_materials[ctm].SetTexture("_Blendmap", blendmap);
				}
				if(blend_materials[ctm].HasProperty("_Blendnormalmap")){
					blend_materials[ctm].SetTexture("_Blendnormalmap", blendnormalmap);
				}
				Vector2 texel = new Vector2(terraindata.size.x/(blendmap.width-1),terraindata.size.z/(blendmap.height-1));
				if(blend_materials[ctm].HasProperty("_terrainmappos")){
					blend_materials[ctm].SetVector("_terrainmappos", new Vector4(transform.position.x,transform.position.z,texel.x,texel.y));
				}
				if(blend_materials[ctm].HasProperty("_terrainmapscale")){
					blend_materials[ctm].SetVector("_terrainmapscale", new Vector4(terraindata.size.x,terraindata.size.y,terraindata.size.z,transform.position.y));
				}
			
				if(automaticColorCorrection == true){ // automatically adjust color correction for linear or gamma space. Alpha is set to 1 in linear color space
					if(blend_materials[ctm].HasProperty("_ColorCorrection")){
						Color colcor = blend_materials[ctm].GetColor("_ColorCorrection");
						if(UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear){
							blend_materials[ctm].SetColor("_ColorCorrection", new Color(colcor.r,colcor.g,colcor.b,1));
						} else{
							blend_materials[ctm].SetColor("_ColorCorrection", new Color(colcor.r,colcor.g,colcor.b,0));
						}
					}
				}
				if(automaticTextureData == true){ // automatically adjust size + offset of textures to match terrain texture
					TryAutoScale(blend_materials[ctm]);
				}
			}
		}
	}

	void TryAutoScale(Material mat){// try to match texture scale + offset automatically
		if (mat.GetTexture ("_TerrainTex")||mat.GetTexture ("_TerrainBump")) {
			Texture2D tex = mat.GetTexture ("_TerrainTex") as Texture2D;
			Texture2D bump = mat.GetTexture ("_TerrainBump") as Texture2D;
			SplatPrototype[] splats = terraindata.splatPrototypes;
			foreach(SplatPrototype spp in splats){
				if(spp.texture == tex || spp.texture == bump){
					mat.SetTextureOffset("_TerrainTex",new Vector2(1f/spp.tileSize.x * spp.tileOffset.x,1f/spp.tileSize.y * spp.tileOffset.y));
					mat.SetTextureScale("_TerrainTex",new Vector2(1f/spp.tileSize.x,1f/spp.tileSize.y));
					mat.SetTextureOffset("_TerrainBump",new Vector2(1f/spp.tileSize.x * spp.tileOffset.x,1f/spp.tileSize.y * spp.tileOffset.y));
					mat.SetTextureScale("_TerrainBump",new Vector2(1f/spp.tileSize.x,1f/spp.tileSize.y));
					mat.SetTexture("_TerrainBump",spp.normalMap);
					mat.SetTexture("_TerrainTex",spp.texture);

					if(mat.HasProperty("_TerrainGlossiness")){
						mat.SetFloat("_TerrainGlossiness", spp.smoothness);
					}
					if(mat.HasProperty("_SpecColor")){
						mat.SetColor("_SpecColor", GetComponent<Terrain>().legacySpecular);
					}
					if(mat.HasProperty("_TerrainShininess")){
						mat.SetFloat("_TerrainShininess",GetComponent<Terrain>().legacyShininess);
					}
					if(mat.HasProperty("_TerrainMetallic")){
						mat.SetFloat("_TerrainMetallic", spp.metallic);
					}
					if(mat.HasProperty("_UseAlphaSmoothness")){
						string texPath = AssetDatabase.GetAssetPath(spp.texture);
						TextureImporter teximp = (TextureImporter)TextureImporter.GetAtPath(texPath);
						// correct smoothness for alpha mode:none
						#if UNITY_5_5_OR_NEWER
						if( teximp.alphaSource == TextureImporterAlphaSource.FromGrayScale || teximp.alphaSource == TextureImporterAlphaSource.FromInput){
						#else
						if(teximp.DoesSourceTextureHaveAlpha() == true || teximp.grayscaleToAlpha == true){
						#endif
							mat.SetFloat("_UseAlphaSmoothness", 1);
						} else{
							mat.SetFloat("_UseAlphaSmoothness", 0);
						}
					}

				}
			}
		}
	}

	void CheckShader(Material mat){//show a warning if the material uses a non blending shader
		foreach (string name in supportedShaders) {
			if (name == mat.shader.name) {
				return;
			}
		}
		if (showMessages) {
			Debug.Log ("Terrain Blending: Warning! The shader '" + mat.shader.name + "' of material '" + mat.name + "' is not a terrain blending shader and will not blend.");
		}
	}


	Color encode_float_rgba(float input){
		Vector4 kEncodeMul = new Vector4(1.0f, 255.0f, 65025.0f, 160581375.0f);
		Vector4 col = kEncodeMul * input;
		col = new Vector4(frac(col.x),frac(col.y),frac(col.z),frac(col.w));
		float kEncodeBit = 1.0f / 255.0f;
		col -= new Vector4(col.y, col.z, col.w, col.w) * kEncodeBit;
		return new Color(col.x,col.y,col.z,col.w);
	}
	float frac(float x){
		return(x - Mathf.Floor(x));
	}
}
#endif