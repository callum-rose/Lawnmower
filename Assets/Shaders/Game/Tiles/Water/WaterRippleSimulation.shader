// One step of the 2D wave equation for WaterRippleSimulation. R = current height, G = previous height.
// The masked-out parts of the simulated area and the inside of every ripple source are held at zero, so waves reflect
// off them. So are the area's edges, unless _EdgeAbsorbWidth > 0: then a band inside them damps waves gradually and the
// outermost texels pass what's left out (a first-order Mur boundary), for water that carries on past the simulated area.
// A thin ring around each source with strength > 0 is driven sinusoidally to emit waves.
Shader "Hidden/WaterRippleSimulation"
{
	Properties
	{
		_MainTex("Previous State", 2D) = "black" {}
		_Mask("Open Water (R)", 2D) = "white" {}
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
			// Same size as the state; red below 0.5 is a wall, e.g. the bank of a pond that isn't a rectangle.
			TEXTURE2D(_Mask);
			float4 _SimRect;
			// Courant number squared
			float _Courant;
			// (c - 1) / (c + 1) for Courant number c
			float _MurCoefficient;
			float _Damping;
			float _ForcePhase;
			float _ForceWidth;
			// World units
			float _EdgeAbsorbWidth;

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

			bool IsOpen(float2 uv)
			{
				return SAMPLE_TEXTURE2D_LOD(_Mask, sampler_PointClamp, uv, 0).r >= 0.5;
			}

			float Height(float2 uv)
			{
				if (any(uv < 0) || any(uv > 1) || !IsOpen(uv))
					return 0;
				return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointClamp, uv, 0).r;
			}

			// The wave equation's next height here, before sources and splashes.
			float Propagate(float2 uv)
			{
				if (!IsOpen(uv))
					return 0;
				float2 state = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointClamp, uv, 0).rg;
				float2 dx = float2(_MainTex_TexelSize.x, 0);
				float2 dy = float2(0, _MainTex_TexelSize.y);
				float neighbours = Height(uv - dx) + Height(uv + dx) + Height(uv - dy) + Height(uv + dy);
				// Damping only the velocity term; scaling the whole update would add a cutoff that stops slow waves travelling.
				// Scaling the whole update in the edge band also reflects waves off the band, more than a wall would.
				float velocity = (state.r - state.g) * _Damping;
				if (_EdgeAbsorbWidth > 0)
				{
					float2 fromEdges = min(uv, 1 - uv) * _SimRect.zw;
					float absorb = saturate(1 - min(fromEdges.x, fromEdges.y) / _EdgeAbsorbWidth);
					velocity *= 1 - 0.1 * absorb * absorb;
				}

				return state.r + velocity + _Courant * (neighbours - 4 * state.r);
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.uv;
				if (!IsOpen(uv))
					return 0;
				float height = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_PointClamp, uv, 0).r;
				float next = Propagate(uv);
				if (_EdgeAbsorbWidth > 0)
				{
					// On the outermost texels, carry the wave arriving from the texel inside straight on out.
					float2 texel = _MainTex_TexelSize.xy;
					float2 inward = float2(uv.x < texel.x ? 1 : uv.x > 1 - texel.x ? -1 : 0,
						uv.y < texel.y ? 1 : uv.y > 1 - texel.y ? -1 : 0);
					if (inward.x != 0)
						inward.y = 0;
					if (any(inward != 0))
					{
						float2 inside = uv + inward * texel;
						next = Height(inside) + _MurCoefficient * (Propagate(inside) - height);
					}
				}

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

				return float4(next, height, 0, 0);
			}
			ENDHLSL
		}
	}
}
