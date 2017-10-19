// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/TerrainMeshBlend/full standard" {
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

		// UI-only data
		[HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
		[HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		
		
		//terrain data
		_TerrainTex ("Terrain Texture", 2D) = "white" {}
		_TerrainBump ("Terrain Bump Map", 2D) = "bump" {}
		_TerrainGlossiness ("Terrain Smoothness", Range(0,1)) = 0.0
		_UseAlphaSmoothness ("Use alpha smoothness", Range(0,1)) = 0.0
		[Gamma] _TerrainMetallic ("Terrain Metallic", Range(0,1)) = 0.0
		_ColorCorrection ("Color correction", Color) = (0.5,0.5,0.5,0)
		_Blend ("Blend", Range(0,10)) = 0.5
		_BlendOffset ("Blend Offset", Range(-5,5)) = 0.1
		[HideInInspector]_Blendmap ("Blendmap", 2D) = "black" {}
		[HideInInspector]_Blendnormalmap ("Blendmap normals", 2D) = "bump" {}
		[HideInInspector]_terrainmappos ("blendmap pos+scale", Vector) = (0,0,1,1)
		[HideInInspector]_terrainmapscale ("terrain size, terrain pos y", Vector) = (500,500,500,0)
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
				
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase 

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers nomrt gles
			

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
			
			#pragma vertex vertDeferred
			#pragma fragment fragDeferred

			#include "UnityStandardCore.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
		//terrain shader
	CGPROGRAM
		#pragma surface surf Standard vertex:vert fullforwardshadows decal:blend
		#pragma multi_compile_fog

		#include "Assets/laxer Assets/Terrain Mesh Blending/CGincs/TerrainMeshBlend.cginc"
		#pragma target 3.0
		sampler2D _Blendmap;
		sampler2D _Blendnormalmap;
		sampler2D _TerrainTex;
		sampler2D _TerrainBump;


		struct Input {
			float2 uv_Blendmap;
			float2 uv_TerrainTex;
			float2 uv_TerrainBump;
			float3 worldPos;
		};
		
		half _Glossiness;
		half _Metallic;
		half _TerrainGlossiness;
		half _TerrainMetallic;
		float _Blend;
		float _BlendOffset;
		float4 _terrainpos;
		float4 _terrainmappos;
		float4 _terrainmapscale;
		float4 _terraintexturepos;
		fixed4 _ColorCorrection;
		half _UseAlphaSmoothness;

		
		void vert (inout appdata_full v, out Input o) {
		    UNITY_INITIALIZE_OUTPUT(Input,o);
            float3 pos = mul(unity_ObjectToWorld, v.vertex).xyz;
            v.texcoord = float4(pos.x - _terrainmappos.x,pos.z -_terrainmappos.y,0,0);
	    	v.normal = mul (unity_WorldToObject, float4(0,1,0,0));
	    	v.tangent.xyz = cross(v.normal, mul (unity_WorldToObject,float4(0,0,1,0)));
			v.tangent.w = -1;
		}
		void surf (Input IN, inout SurfaceOutputStandard o) {
			float2 blendmapuv = (IN.uv_Blendmap+_terrainmappos.zw*0.5) / (_terrainmapscale.xz+_terrainmappos.zw);
			
			float theight = DecodeFloatRGBA(tex2D (_Blendmap, blendmapuv))*_terrainmapscale.y + _terrainmapscale.w;
			float diff = (IN.worldPos.y - theight) - _BlendOffset;
			fixed4 c = tex2D (_TerrainTex, IN.uv_TerrainTex );
			o.Smoothness = lerp(_TerrainGlossiness,c.a,_UseAlphaSmoothness);
			c.a = clamp((1 - diff / _Blend),0,1);
			o.Albedo = _ColorCorrection.rgb * 2 * (1+_ColorCorrection.a*1.3) * c.rgb;
			fixed4 nmap = tex2D (_Blendnormalmap, blendmapuv );
			float3 terrainNormal = float3(nmap.x*2-1,nmap.y*2-1,nmap.z*2-1);
			o.Normal = combineNormals(normalize(float3(nmap.x*2-1,nmap.y*2-1,nmap.z*2-1)),UnpackNormal(tex2D(_TerrainBump, IN.uv_TerrainBump)));
			//o.Normal = transformNormals(terrainNormal,UnpackNormal(tex2D(_TerrainBump, IN.uv_TerrainBump)),cross(terrainNormal, float3(0,1,0)));
			o.Metallic = _TerrainMetallic;
			o.Alpha = c.a;
		}
		ENDCG
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
	
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
			//terrain shader
	CGPROGRAM
		#pragma surface surf Standard vertex:vert fullforwardshadows decal:blend
		#pragma multi_compile_fog

		#include "Assets/laxer Assets/Terrain Mesh Blending/CGincs/TerrainMeshBlend.cginc"
		#pragma target 3.0
		sampler2D _Blendmap;
		sampler2D _Blendnormalmap;
		sampler2D _TerrainTex;
		sampler2D _TerrainBump;


		struct Input {
			float2 uv_Blendmap;
			float2 uv_TerrainTex;
			float2 uv_TerrainBump;
			float3 worldPos;
		};
		
		half _Glossiness;
		half _Metallic;
		half _TerrainGlossiness;
		half _TerrainMetallic;
		float _Blend;
		float _BlendOffset;
		float4 _terrainpos;
		float4 _terrainmappos;
		float4 _terrainmapscale;
		float4 _terraintexturepos;
		fixed4 _ColorCorrection;
		half _UseAlphaSmoothness;

		
		void vert (inout appdata_full v, out Input o) {
		    UNITY_INITIALIZE_OUTPUT(Input,o);
            float3 pos = mul(unity_ObjectToWorld, v.vertex).xyz;
            v.texcoord = float4(pos.x - _terrainmappos.x,pos.z -_terrainmappos.y,0,0);
	    	v.normal = mul (unity_WorldToObject, float4(0,1,0,0));
	    	v.tangent.xyz = cross(v.normal, mul (unity_WorldToObject,float4(0,0,1,0)));
			v.tangent.w = -1;
		}
		void surf (Input IN, inout SurfaceOutputStandard o) {
			float2 blendmapuv = (IN.uv_Blendmap+_terrainmappos.zw*0.5) / (_terrainmapscale.xz+_terrainmappos.zw);
			
			float theight = DecodeFloatRGBA(tex2D (_Blendmap, blendmapuv))*_terrainmapscale.y + _terrainmapscale.w;
			float diff = (IN.worldPos.y - theight) - _BlendOffset;
			fixed4 c = tex2D (_TerrainTex, IN.uv_TerrainTex );
			o.Smoothness = lerp(_TerrainGlossiness,c.a,_UseAlphaSmoothness);
			c.a = clamp((1 - diff / _Blend),0,1);
			o.Albedo = _ColorCorrection.rgb * 2 * (1+_ColorCorrection.a*1.3) * c.rgb;
			fixed4 nmap = tex2D (_Blendnormalmap, blendmapuv );
			float3 terrainNormal = float3(nmap.x*2-1,nmap.y*2-1,nmap.z*2-1);
			o.Normal = combineNormals(normalize(float3(nmap.x*2-1,nmap.y*2-1,nmap.z*2-1)),UnpackNormal(tex2D(_TerrainBump, IN.uv_TerrainBump)));
			//o.Normal = transformNormals(terrainNormal,UnpackNormal(tex2D(_TerrainBump, IN.uv_TerrainBump)),cross(terrainNormal, float3(0,1,0)));
			o.Metallic = _TerrainMetallic;
			o.Alpha = c.a;
		}
		ENDCG
	}
	
	
	
	FallBack "Diffuse"
	CustomEditor "TerrainBlendStandardShaderGUI"
}
