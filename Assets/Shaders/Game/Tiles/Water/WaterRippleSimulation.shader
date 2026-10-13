// One step of the 2D wave equation for WaterRippleSimulation. R = current height, G = previous height.
// The edges of the simulated area and the inside of every ripple source are held at zero, so waves reflect off them.
// A thin ring around each source with strength > 0 is driven sinusoidally to emit waves.
Shader "Hidden/WaterRippleSimulation"
{
	Properties
	{
		_MainTex("Previous State", 2D) = "black" {}
	}

	SubShader
	{
		ZTest Always ZWrite Off Cull Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			float4 _MainTex_TexelSize;
			float4 _SimRect;
			float _Courant;
			float _Damping;
			float _ForcePhase;
			float _ForceWidth;

			float4 _WaterRipples[16];
			float _WaterRippleCount;

			// One-off pushes queued by WaterRippleSimulation.Splash: xy = world xz, z = radius, w = strength.
			float4 _Splashes[8];
			float _SplashCount;

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

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				return output;
			}

			float Height(float2 uv)
			{
				if (any(uv < 0) || any(uv > 1))
					return 0;
				return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointClamp, uv, 0).r;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.uv;
				float2 state = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointClamp, uv, 0).rg;
				float2 dx = float2(_MainTex_TexelSize.x, 0);
				float2 dy = float2(0, _MainTex_TexelSize.y);
				float neighbours = Height(uv - dx) + Height(uv + dx) + Height(uv - dy) + Height(uv + dy);
				// Damping only the velocity term; scaling the whole update would add a cutoff that stops slow waves travelling.
				float next = state.r + (state.r - state.g) * _Damping + _Courant * (neighbours - 4 * state.r);

				float2 world = _SimRect.xy + uv * _SimRect.zw;
				for (int j = 0; j < (int)_SplashCount; j++)
				{
					float4 splash = _Splashes[j];
					float2 offset = (world - splash.xy) / splash.z;
					next -= splash.w * exp(-dot(offset, offset));
				}

				for (int i = 0; i < (int)_WaterRippleCount; i++)
				{
					float4 source = _WaterRipples[i];
					float d = distance(world, source.xy) - source.z;
					if (d < 0)
						next = 0;
					else if (d < _ForceWidth && source.w > 0)
						next = source.w * sin(_ForcePhase);
				}

				return float4(next, state.r, 0, 0);
			}
			ENDHLSL
		}
	}
}
