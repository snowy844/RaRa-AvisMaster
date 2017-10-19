Shader "Azure[Sky]/Projected Fog Scattering"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"


			uniform sampler2D _MainTex;
			uniform sampler2D_float _CameraDepthTexture;
			uniform float4x4  _FrustumCorners;
			uniform float4    _MainTex_TexelSize;

			uniform float4    _Azure_RayleighColor, _Azure_MieColor, _Azure_MoonDiskColor, _Azure_MoonSkyBrightColor;
			uniform float3    _Azure_SunDirection, _Azure_MoonDirection, _Azure_Br, _Azure_Bm, _Azure_MieG;
			uniform float     _Azure_Pi, _Azure_Kr, _Azure_Km, _Azure_Pi316, _Azure_Pi14, _Azure_SunIntensity, _Azure_SkyDarkness, _Azure_NightIntensity, _Azure_Exposure,
			                  _Azure_MoonSkyBright, _Azure_MoonSkyBrightRange, _Azure_MoonDiskBright, _Azure_MoonDiskBrightRange, _Azure_FogDistance, _Azure_FogScale, _Azure_FogExtinction,
			                  _Azure_GammaCorrection, _Azure_LightSpeed, _Azure_FogMieDepth;

			uniform float4x4  _Azure_MoonMatrix;

			struct appdata
			{
				float4 vertex   : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 Position        : SV_POSITION;
    			float2 uv 	           : TEXCOORD0;
				float4 interpolatedRay : TEXCOORD1;
				float2 uv_depth        : TEXCOORD2;
			};

			v2f vert (appdata v)
			{
				v2f o;
    			UNITY_INITIALIZE_OUTPUT(v2f, o);
    			
    			//int index = v.vertex.z;
				v.vertex.z = 0.1;
				o.Position = UnityObjectToClipPos(v.vertex);
				o.uv       = v.texcoord.xy;
				o.uv_depth = v.texcoord.xy;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1-o.uv.y;
				#endif

				//Based on Unity5.6 GlobalFog.
				int index = v.texcoord.x + (2 * o.uv.y);
				o.interpolatedRay   = _FrustumCorners[index];
				o.interpolatedRay.w = index;

				return o;
			}

			float4 frag (v2f IN) : SV_Target
			{
				//Original scene.
				float3 screen = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(IN.uv)).rgb;

				//Transitions.
				//-------------------------------------------------------------------------------------------------------
				float Fade = saturate( _Azure_SunDirection.y + 0.25 ); //Fade the "daysky" when the sun cross the horizon.

				//Reconstruct world space position and direction towards this screen pixel.
			    //-------------------------------------------------------------------------------------------------------
			    float  depth       = Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(IN.uv_depth))));
			    if(depth == 1.0) return float4(screen, 1.0);
                float3 viewDir     = normalize(depth * IN.interpolatedRay.xyz);
			    float  sunCosTheta = dot(viewDir, _Azure_SunDirection);
			    
			    //Optical Depth
			    //-------------------------------------------------------------------------------------------------------
			    //float  zenith = acos(length(1.0 - depth) * _Azure_FogScale);
			    float  zenith = acos(length(viewDir.y));
			    float  z      = (cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0) / _Azure_Pi), -1.253));
			    float  SR     = _Azure_Kr  / z;
			    float  SM     = _Azure_Km  / z;

			    //Total Extinction.
			    //-------------------------------------------------------------------------------------------------------
			    float3 fex    = exp(-(_Azure_Br*SR  + _Azure_Bm*SM) );

			    //Sun Scattering.
			    //-------------------------------------------------------------------------------------------------------
			    //float  rayPhase = 1.0 + pow(sunCosTheta, 2.0);                          				 //Preetham rayleigh phase function.
			    float  rayPhase = 2.0 + 0.5 * pow(sunCosTheta, 2.0);                   				     //Rayleigh phase function based on the Nielsen's paper.
			    float  miePhase = _Azure_MieG.x / pow(_Azure_MieG.y - _Azure_MieG.z * sunCosTheta, 1.5); //The Henyey-Greenstein phase function.

			    float r         = length(float3(0, _Azure_LightSpeed, 0));
				float sunRise   = saturate(dot(float3(0, 500,0), _Azure_SunDirection) / r);

			    float3 BrTheta  = _Azure_Pi316 * _Azure_Br * rayPhase * _Azure_RayleighColor.rgb;
			    float3 BmTheta  = _Azure_Pi14  * _Azure_Bm * miePhase * _Azure_MieColor.rgb * sunRise;
			    	   BmTheta *= lerp(1.0, depth, _Azure_FogMieDepth);
			    float3 BrmTheta = (BrTheta + BmTheta) / (_Azure_Br + _Azure_Bm);

			    float3 inScatter  = BrmTheta * _Azure_SunIntensity * (1.0 - fex);
			    inScatter *= pow((1.0 - fex), _Azure_SkyDarkness);
			    inScatter *= Fade;

			    //Night Sky.
			    //-------------------------------------------------------------------------------------------------------
			    float3 nightSky  = (1.0 - fex) * _Azure_RayleighColor.rgb * _Azure_NightIntensity; //Defaut night sky color.
			           nightSky *= 1.0 - Fade;

			    //Moon Bright.
			    float  moonPosition = 1.0 + dot( viewDir, _Azure_MoonMatrix[2].xyz);
				float3 moonBright   = 1.0 / (_Azure_MoonSkyBright  + moonPosition * _Azure_MoonSkyBrightRange)  * _Azure_MoonSkyBrightColor.rgb;
				       moonBright  += 1.0 / (_Azure_MoonDiskBright + moonPosition * _Azure_MoonDiskBrightRange) * _Azure_MoonDiskColor.rgb;

				//Finalization.
				//-------------------------------------------------------------------------------------------------------
				float moonRise  = saturate(dot(float3(0, 500,0), _Azure_MoonDirection) / r);
				float4 col      = float4(inScatter + nightSky, 1.0);//col.a = Horizon extinction of stars, moom and deep space.
				       col.rgb += max(0.0, moonBright * moonRise);

				//Tonemapping.
				col.rgb  = saturate( 1.0 - exp( -_Azure_Exposure * col.rgb ));
				//Color Correction.
			    col = pow(col,_Azure_GammaCorrection);

			    float dpt = saturate(depth * (_ProjectionParams.z / _Azure_FogDistance));
				col.rgb   = lerp(screen.rgb, col.rgb, dpt);
				return float4(screen.rgb * fex * _Azure_FogExtinction + col.rgb * (1.0 - _Azure_FogExtinction), 1.0);
			}
			ENDCG
		}
	}
}
