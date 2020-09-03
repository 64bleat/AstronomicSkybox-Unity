Shader "Skybox/Stars" 
{
	Properties
	{
		_BlueShift("BlueShift", Color) = (0,0,1,1)
		_Color("NeutralColor", Color) = (1,1,1,1)
		_RedShift("RedShift", Color) = (1,0,0,1)
		_ShiftExp("ShiftExponent", Float) = 1
		_LuminosityExp("LuminosityExponent", Float) = 0.7
		_LimitBrightness("MaxBrightness", Float) = 7
		_ColorScale("ColorScale", Float) = 195
		_ColorPower("ColorPower", Float) = 195
		_Scale("Brightness", Float) = 1.0
		_Mult("Multiplier", Float) = 1.0
	}

	CGINCLUDE
		float4 _BlueShift;
		float4 _Color;
		float4 _RedShift;
		float _ShiftExp;
		float _LuminosityExp;
		float _LimitBrightness;
		float _ColorScale;
		float _ColorPower;
		float _Scale;
		float _Mult;
	ENDCG

	SubShader
	{
		Tags{"Queue" = "Transparent" "RenderType" = "Transparent"}
		ZWrite Off
		Blend One One
		Pass 
		{
			CGPROGRAM
				#pragma vertex vert  
				#pragma fragment frag 

				//Special info encoded in vertex color
				#define r_Magnitude r
				#define g_Luminance g
				#define b_ColorIndex b

				struct appdata
				{
					float4 vertex : POSITION;
					float4 uv : TEXCOORD0;
					fixed4 cInfo : COLOR;
				};

				struct v2f 
				{
					float4 pos : SV_POSITION;
					float4 col : COLOR;
					float2 uv : TEXCOORD0;
					half uvMag : TEXCOORD1;
				};

				v2f vert(appdata input)
				{
					v2f o;

					o.pos = UnityObjectToClipPos(input.vertex);

					// UV
					o.uv = input.uv;
					o.uvMag = input.cInfo.r_Magnitude;

					// Base Color
					float shift = pow(abs(input.cInfo.b_ColorIndex - 0.5) * 2.0, _ShiftExp);
					if(input.cInfo.b_ColorIndex < 0.5)
						o.col = lerp(_Color, _BlueShift, shift);
					else
						o.col = lerp(_Color, _RedShift, shift);
				
					// Brightness
					o.col *= pow(input.cInfo.g_Luminance, _LuminosityExp);

					// Scale and Power
					o.col = pow(o.col * _ColorScale, _ColorPower);

					// Non-Negative
					o.col = max(o.col, float4(0,0,0,0));

					// Lower Clamp
					o.col = (max(o.col - _Scale, float4(0,0,0,0))) * _Scale;

					// Upper Clamp
					o.col = min(o.col, float4(_LimitBrightness,_LimitBrightness,_LimitBrightness,1));

					// Multiplier
					o.col *= _Mult;

					return o;
				}

				float4 frag(v2f input) : COLOR
				{
					float d = length(input.uv.xy - 0.5);

					if(d > 0.5)
						discard;

					return input.col * (0.5 - d) * 3 * input.uvMag;
				}
			ENDCG
		}
	}
}