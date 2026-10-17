// Bakes the Water shader's animated Voronoi web into a small tiling texture once a frame, so water pixels sample it
// instead of evaluating the web themselves. Red is the web's edge distance (F2 - F1) at the water's caustic time (_Time.y x
// _TimeScale), green at 1.3x that time. Cells wrap every _Period cells, so the texture tiles. Drawn by WaterCausticBaker.
Shader "Hidden/WaterCausticBake"
{
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _Period;
			float _TimeScale;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float2 Hash22(float2 p)
			{
				float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
				p3 += dot(p3, p3.yzx + 33.33);
				return frac((p3.xx + p3.yz) * p3.zy);
			}

			// Edge distance of an animated Voronoi web (as the Water shader's CausticWeb, before sharpening), with cell
			// coordinates wrapped so it repeats every _Period cells.
			float WebEdgeDistance(float2 p, float t)
			{
				float2 n = floor(p);
				float2 f = frac(p);
				float f1 = 8;
				float f2 = 8;
				[unroll]
				for (int j = -1; j <= 1; j++)
				{
					[unroll]
					for (int i = -1; i <= 1; i++)
					{
						float2 g = float2(i, j);
						float2 cell = n + g;
						cell -= floor(cell / _Period) * _Period;
						float2 o = Hash22(cell);
						o = 0.5 + 0.45 * sin(t + 6.2831 * o);
						float d = length(g + o - f);
						if (d < f1)
						{
							f2 = f1;
							f1 = d;
						}
						else if (d < f2)
						{
							f2 = d;
						}
					}
				}
				return f2 - f1;
			}

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				return output;
			}

			float2 Frag(Varyings input) : SV_Target
			{
				float2 p = input.uv * _Period;
				float t = _Time.y * _TimeScale;
				return float2(WebEdgeDistance(p, t), WebEdgeDistance(p, t * 1.3));
			}
			ENDHLSL
		}
	}
}
