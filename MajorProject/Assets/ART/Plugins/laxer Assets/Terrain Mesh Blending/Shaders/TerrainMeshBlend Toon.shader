// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TerrainMeshBlend/Toon" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" { }
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
	
	
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
	#include "UnityCG.cginc"
	
	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		UNITY_FOG_COORDS(0)
		fixed4 color : COLOR;
	};
	
	uniform float _Outline;
	uniform float4 _OutlineColor;
	
	v2f vert(appdata v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
		float2 offset = TransformViewToProjection(norm.xy);

		o.pos.xy += offset * o.pos.z * _Outline;
		o.color = _OutlineColor;
		UNITY_TRANSFER_FOG(o,o.pos);
		return o;
	}
	ENDCG
	
	SubShader {
	Tags { "RenderType"="Opaque" }
		//Toon Shader Pass
		UsePass "Toon/Basic/BASE"
		//Toon Outline Pass
		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Front
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_APPLY_FOG(i.fogCoord, i.color);
				return i.color;
			}
			ENDCG
		}
		///////Terrain Blending Pass//////
		CGPROGRAM
		#pragma surface surf Standard vertex:vertb fullforwardshadows decal:blend exclude_path:deferred
		#pragma multi_compile_fog

		#include "Assets/laxer Assets/Terrain Mesh Blending/CGincs/TerrainMeshBlend.cginc"

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


		void vertb (inout appdata_full v, out Input o) {
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
}
