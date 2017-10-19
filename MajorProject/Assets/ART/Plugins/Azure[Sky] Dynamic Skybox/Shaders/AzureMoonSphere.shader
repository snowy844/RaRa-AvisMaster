Shader "Azure[Sky]/Moon Sphere"
{
	Properties
	{
		_Azure_MoonSphereTextureMap("Moon Texture Map", 2D) = ""{}
		//_Azure_MoonSphereSaturation("Saturation", Range(0.5,2)) = 1.0
		//_Azure_MoonSpherePenunbra("Penunbra", Range(0,4))       = 0.5
		//_Azure_MoonSphereShadow("Shadow Alpha", Range(0,0.25))  = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "IgnoreProjector"="True" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex   : POSITION;
				float3 normal   : NORMAL;
			};

			struct v2f
			{
				float4 vertex     : SV_POSITION;
				float3 WorldPos   : TEXCOORD0;
				float3 nDirection : NORMAL;
			};

			#define           _pi	3.14159265359f

			uniform float3    _Azure_SunDirection;
			uniform sampler2D _Azure_MoonSphereTextureMap;
			float     		  _Azure_MoonSphereSaturation, _Azure_MoonSpherePenunbra, _Azure_MoonSphereShadow;

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex     = UnityObjectToClipPos(v.vertex);
			    o.nDirection = float3(mul(float4(v.normal, 1.0), (float4x4)unity_WorldToObject).xyz);
	            o.WorldPos   = v.vertex.xyz;
				return o;
			}
			
			float4 frag (v2f IN) : SV_Target
			{
				float3 pos = normalize(IN.WorldPos);
				float4 outColor = float4(1.0, 1.0, 1.0,1.0);

				float2 moonUV  = float2(atan2(pos.z, pos.x) + 1.5, acos(pos.y)) / float2(2.0 * _pi, _pi);
				//float2 moonUV  = float2(atan2(IN.nDirection.z, IN.nDirection.x), acos(IN.nDirection.y)) / float2(2.0 * _pi, _pi);
			    float  moonTex = tex2D( _Azure_MoonSphereTextureMap, moonUV).r;

			    //directional lighting.
			    float3 lightDir   = normalize(_Azure_SunDirection);
			    float  light      = max(dot(IN.nDirection, lightDir), 0.0) + _Azure_MoonSphereShadow;
			    //outColor.rgb     += light;
			    outColor.rgb     *= moonTex * pow(light, _Azure_MoonSpherePenunbra);
			    outColor.rgb      = pow(outColor.rgb, _Azure_MoonSphereSaturation);
			    return outColor;
			}
			ENDCG
		}
	}
}
