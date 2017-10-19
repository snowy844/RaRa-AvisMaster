Shader "Azure[Sky]/SDF Moon"
{
	Properties
	{
		_AzureMoonTextureMap("Moon Texture Map", 2D) = ""{}
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			//#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			uniform float     _Azure_Pi;
			uniform float3    _Azure_SunDirection, _Azure_MoonDirection;
			uniform sampler2D _AzureMoonTextureMap;
			uniform float4x4  _Azure_SunMatrix, _Azure_SunNormalMatrix;


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex        : SV_POSITION;
    			float2 uv 	         : TEXCOORD0;
    			float3 viewDir       : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
			  //v.vertex.z = 0.1;
				o.vertex  = UnityObjectToClipPos(v.vertex);
				o.viewDir = normalize(WorldSpaceViewDir(v.vertex));//ObjSpaceViewDir is similar, but localspace.
				o.uv      = v.uv;
				return o;
			}

			float4 frag (v2f IN) : SV_Target
			{
				float s = dot(_Azure_MoonDirection, float3(0,1,0));
				//float3 sunLight = normalize(_Azure_SunDirection);
				//SDF moon sphere.
				float4 outColor = float4(0.0, 0.0, 0.0,1.0);
				float  size  = 0.5;
			    float2 scale = float2(1.0, 1.0);
			    float2 uv = float2 ( IN.uv.x * 512, IN.uv.y * 512);
			    float2 xy = (2.0 * (IN.uv.xy) -1.0 ) / scale / size;
			    float  r  = dot(xy,xy);

			    float3 normalDirection  = float3( xy, sqrt( 1.0 - r ));
			    float2 moonUV  = float2(atan2(normalDirection.z, normalDirection.x) + 1.5, acos(normalDirection.y)) / float2(2.0 * _Azure_Pi, _Azure_Pi);
			    float  moonTex = tex2D( _AzureMoonTextureMap, moonUV).r;

			    //directional lighting.
			    float3 lightDir   = normalize(_Azure_SunDirection * float3(1,1,1));

			    float  light      = max(dot(normalDirection, lightDir), 0.0);
			    outColor.rgb     += pow(light, 1.0);
			    outColor.rgb     *= moonTex;
			    return outColor;
			}
			ENDCG
		}
	}
}