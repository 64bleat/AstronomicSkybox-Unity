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
				#define r_Magnitude r
				#define g_Luminance g
				#define b_ColorIndex b

				struct appdata_full 
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					fixed4 colorData : COLOR;
				};

				struct vertexOutput 
				{
					float4 pos : SV_POSITION;
					float4 col : COLOR;
					float2 uv : TEXCOORD0;
					half uvMag : TEXCOORD1;
				};

				vertexOutput vert(appdata_full input)
				{
					vertexOutput output;

					output.pos = UnityObjectToClipPos(input.vertex);

					// UV
					output.uv = input.texcoord;
					output.uvMag = input.colorData.r_Magnitude;

					// Base Color
					float shift = pow(abs(input.colorData.b_ColorIndex - 0.5) * 2.0, _ShiftExp);
					if(input.colorData.b_ColorIndex < 0.5)
						output.col = lerp(_Color, _BlueShift, shift);
					else
						output.col = lerp(_Color, _RedShift, shift);
				
					// Brightness
					output.col *= pow(input.colorData.g_Luminance, _LuminosityExp);

					// Scale and Power
					output.col = pow(output.col * _ColorScale, _ColorPower);

					// Non-Negative
					output.col = max(output.col, float4(0,0,0,0));

					// Lower Clamp
					output.col = (max(output.col - _Scale, float4(0,0,0,0))) * _Scale;

					// Upper Clamp
					output.col = min(output.col, float4(_LimitBrightness,_LimitBrightness,_LimitBrightness,1));

					// Multiplier
					output.col *= _Mult;

					return output;
				}

				float4 frag(vertexOutput input) : COLOR
				{
					float d = distance(float2(input.uv.x, input.uv.y), float2(0.5, 0.5));

					if(d > 0.5)
						discard;

					return input.col * (0.5 - d) * 3 * input.uvMag;
				}
			ENDCG
		}
	}
}