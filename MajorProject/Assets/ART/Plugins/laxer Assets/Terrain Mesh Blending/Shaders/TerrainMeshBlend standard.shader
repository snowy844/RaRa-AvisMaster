// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/TerrainMeshBlend/standard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bump Map (RGB)", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_TerrainTex ("Terrain Texture", 2D) = "white" {}
		_TerrainBump ("Terrain Bump Map", 2D) = "bump" {}
		_TerrainGlossiness ("Terrain Smoothness", Range(0,1)) = 0.0
		_UseAlphaSmoothness ("Use alpha smoothness", Range(0,1)) = 0.0
		[Gamma] _TerrainMetallic ("Terrain Metallic", Range(0,1)) = 0.0
		_ColorCorrection ("Color correction", Color) = (0.5,0.5,0.5,0)
		_Blend ("Blend", Range(0,10)) = 0.5
		_BlendOffset ("Blend Offset", Range(-5,5)) = 0.1
		_BlendExponent ("Blend Gradient", Range(0,0.25)) = 0
		[HideInInspector]_Blendmap ("Blendmap", 2D) = "black" {}
		[HideInInspector]_Blendnormalmap ("Blendmap normals", 2D) = "bump" {}
		[HideInInspector]_terrainmappos ("blendmap pos+scale", Vector) = (0,0,1,1)
		[HideInInspector]_terrainmapscale ("terrain size, terrain pos y", Vector) = (500,500,500,0)
	}
	SubShader {
		Tags {
			"Queue" = "Geometry-99"
			"RenderType" = "Opaque"
		}
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows exclude_path:deferred

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
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
		float _BlendExponent;
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
			o.Alpha = _BlendExponent*((c.a*2-1)*(c.a*2-1)-1) + c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
