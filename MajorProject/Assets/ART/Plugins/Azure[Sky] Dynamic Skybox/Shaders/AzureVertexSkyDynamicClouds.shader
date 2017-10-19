Shader "Azure[Sky]/Vertex/Sky Dynamic Clouds"
{
	Properties
	{
		_Azure_DynamicCloudAltitude("Cloud Altitude", Range(35,100)) = 75
	}
	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "IgnoreProjector"="True" }
	    Cull [_Azure_CullMode] // Render side
		Fog{Mode Off}          // Don't use fog
    	ZWrite Off             // Don't draw to bepth buffer

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		    #pragma target 3.0



			uniform float3 _Azure_SunDirection, _Azure_MoonDirection, _Azure_Br, _Azure_Bm, _Azure_MieG, _Azure_StarfieldColorBalance;
			uniform float  _Azure_Pi316, _Azure_Pi14, _Azure_Kr, _Azure_Km, _Azure_SunIntensity, _Azure_Pi, _Azure_Exposure, _Azure_SkyDarkness,
			               _Azure_NightIntensity, _Azure_StarfieldIntensity, _Azure_MilkyWayIntensity, _Azure_MoonDiskSize,
			               _Azure_MoonDiskBright, _Azure_MoonDiskBrightRange, _Azure_MoonSkyBright, _Azure_MoonSkyBrightRange, _Azure_GammaCorrection, _Azure_SunDiskSize,
			               _Azure_SunDiskPropagation, _Azure_StaticCloudMultiplier, _Azure_LightSpeed;
			uniform float4 _Azure_RayleighColor, _Azure_MieColor, _Azure_MoonDiskColor, _Azure_MoonSkyBrightColor;

			uniform samplerCUBE _Azure_StarfieldTexture, _AzureStarNoiseTexture;
			uniform float4x4    _Azure_SunMatrix, _Azure_MoonMatrix, _Azure_StarfieldMatrix, _Azure_NoiseMatrix;
			uniform sampler2D   _Azure_MoonTexture, _Azure_DynamicCloudTexture;

			uniform float  _Azure_DynamicCloudCovarage, _Azure_DynamicCloudSpeed, _Azure_DynamicCloudDirection, _Azure_DynamicCloudAltitude;
			uniform float4 _Azure_DynamicCloudColor1, _Azure_DynamicCloudColor2;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 Position     : SV_POSITION;
				float3 WorldPos     : TEXCOORD0;
				float3 StarfieldPos : TEXCOORD1;
				float3 MoonPos      : TEXCOORD2;
				float3 NoiseRot     : TEXCOORD3;
				float4 CloudPos     : TEXCOORD4;
				float4 OutColor     : COLOR;
				float4 MoonBright   : COLOR1;
				float4 MieColor     : COLOR2;
				float4 Fex          : COLOR3;
			};
			
			v2f vert (appdata v)
			{
				//Initializations.
				//-------------------------------------------------------------------------------------------------------
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.Position = UnityObjectToClipPos(v.vertex);
				o.WorldPos = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));

				//Dynamic Cloud 2D.
			    //-------------------------------------------------------------------------------------------------------
			    //Cloud Position.
				float3 CloudPos = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));

				//Cloud direction.
				float s = sin (_Azure_DynamicCloudDirection);
                float c = cos (_Azure_DynamicCloudDirection);
                float2x2 rotationMatrix = float2x2( c, -s, s, c);
				CloudPos.y  *= dot(float3(0, _Azure_DynamicCloudAltitude, 0), float3(0,0.2,0));
				CloudPos.xz  = mul(CloudPos.xz, rotationMatrix );

				//Cloud uvs.
				float cloudSpeed = _Azure_DynamicCloudSpeed * _Time;
				CloudPos = normalize(CloudPos);

				o.CloudPos.xy = (CloudPos.xz * 0.25)  - (0.005  ) + float2(cloudSpeed/20, cloudSpeed);
				o.CloudPos.zw = (CloudPos.xz * 0.35)  - (0.0065 ) + float2(cloudSpeed/20, cloudSpeed);

				//Matrix.
			    //-------------------------------------------------------------------------------------------------------
			    o.NoiseRot     = mul((float3x3)_Azure_NoiseMatrix,v.vertex.xyz);//Rotate noise texture to apply star scintillation
				o.StarfieldPos = mul((float3x3)_Azure_SunMatrix,v.vertex.xyz);
				o.StarfieldPos = mul((float3x3)_Azure_StarfieldMatrix, o.StarfieldPos);
    			o.MoonPos      = mul((float3x3)_Azure_MoonMatrix,v.vertex.xyz) * (25 * (1.0 - _Azure_MoonDiskSize));
    			o.MoonPos.x   *= -1.0; //Invert x scale.

				//Transitions.
				//-------------------------------------------------------------------------------------------------------
				float Fade = saturate( _Azure_SunDirection.y + 0.25 ); //Fade the "daysky" when the sun cross the horizon.

				//Directions.
				//-------------------------------------------------------------------------------------------------------
				float3 viewDir     = normalize(v.vertex.xyz);
				float  sunCosTheta = dot( viewDir, _Azure_SunDirection );

				//Optical Depth
			    //-------------------------------------------------------------------------------------------------------
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
				float4 skyColor      = float4(inScatter + nightSky, saturate(fex.b * (viewDir.y + 0.1)));//col.a = Horizon extinction of stars, moom and deep space.
				       skyColor.rgb += max(0.0, moonBright * moonRise);

				//Outputs.
				//-------------------------------------------------------------------------------------------------------
				o.OutColor   = skyColor;
				o.MoonBright = float4(moonBright, 1.0);
				o.MieColor   = saturate(float4(BmTheta / _Azure_Bm, 1.0));
				o.Fex = float4(pow(fex, 3.5), sunCosTheta);
				return o;
			}
			
			float4 frag (v2f IN) : SV_Target
			{
				float3 viewDir = IN.WorldPos - _WorldSpaceCameraPos;

				//Moon.
				//-------------------------------------------------------------------------------------------------------
				float  moonFade    = saturate(dot(-_Azure_MoonMatrix[2].xyz,IN.WorldPos));//Fade other side moon.
				float2 scale = float2(1.0, 1.0);
			    float2 moonTex = tex2D( _Azure_MoonTexture, IN.MoonPos.xy /scale +0.5).ra;
			    float3 MoonColor    = (pow(float3(moonTex.r, moonTex.r, moonTex.r), 2.5) * (_Azure_MoonDiskColor.rgb * 2.0)) * IN.OutColor.a * moonFade;
			           MoonColor   *= 10.0;
			    float  moonMask     = 1.0 - saturate(moonTex.g * 10.0) * moonFade;//Fade behind the moon.
			    //return float4(moonMask,moonMask,moonMask, 1);

				//Starfield.
				//-------------------------------------------------------------------------------------------------------
				float  scintillation = texCUBE(_AzureStarNoiseTexture, IN.NoiseRot.xyz) * 2.0;
				float4 Starfield = texCUBE(_Azure_StarfieldTexture, IN.StarfieldPos.xyz);
				float3 Stars     = Starfield.rgb * Starfield.a * scintillation;
				float3 MilkyWay  = pow(Starfield.rgb, 1.5) * _Azure_MilkyWayIntensity;

				//Solar Disk.
			    //-------------------------------------------------------------------------------------------------------
			    float  sunMask  = saturate(IN.WorldPos.y * 100);
			    float3 sunDisk  = min(2.0, pow((1.0 - IN.Fex.a) * _Azure_SunDiskSize , -_Azure_SunDiskPropagation )) * IN.Fex.rgb * _Azure_SunIntensity;

				//Clouds.
				//-------------------------------------------------------------------------------------------------------
				//Cloud noise.
				float4 tex1  = tex2D(_Azure_DynamicCloudTexture, IN.CloudPos.xy );
				float4 tex2  = tex2D(_Azure_DynamicCloudTexture, IN.CloudPos.zw );
				float noise1 = pow(tex1.g + tex2.g, 0.25);
				float noise2 = pow(tex2.b * tex1.r, 0.5);

				//Color fix.
			    _Azure_DynamicCloudColor2.rgb = pow(_Azure_DynamicCloudColor2.rgb, 2.2);
				_Azure_DynamicCloudColor1.rgb = pow(_Azure_DynamicCloudColor1.rgb, 2.2);

				//Cloud finalization.
				float3 cloud1 = lerp(float3(0, 0, 0), _Azure_DynamicCloudColor2.rgb, noise1);
				float3 cloud2 = lerp(float3(0, 0, 0), _Azure_DynamicCloudColor1.rgb, noise2) * 1.5;
				float3 cloud  = lerp(cloud1, cloud2, noise1 * noise2);

				//Cloud alpha.
				float cloudAlpha = saturate(pow(noise1 * noise2, _Azure_DynamicCloudCovarage));

				//Output.
				//-------------------------------------------------------------------------------------------------------
				float3 OutColor  = IN.OutColor.rgb + max(0.0,((((Stars + MilkyWay)* (1.0 - cloudAlpha) * _Azure_StarfieldColorBalance) * IN.OutColor.a) * _Azure_StarfieldIntensity) * moonMask + MoonColor* (1.0 - cloudAlpha));
				       OutColor  = lerp(OutColor, cloud, cloudAlpha * pow(IN.OutColor.a, 0.35));
				       OutColor += sunDisk * sunMask * saturate((1.0 - cloudAlpha * 1.15));

				//Tonemapping.
				OutColor.rgb  = saturate( 1.0 - exp( -_Azure_Exposure * OutColor ));
				//Color Correction.
			    OutColor = pow(OutColor,_Azure_GammaCorrection);

				return float4(OutColor, 1.0);
			}
			ENDCG
		}
	}
}
