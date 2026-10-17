// The level's SDF terrain. Tops take each tile's colour from _TerrainCells (one texel per tile, in object space where
// tile (x, z) covers [x, x + 1] x [z, z + 1]), blended softly across tile edges. Sides and the rounded edge fall back
// to a gradient by height between two stone colours, under the cobbles that edge the shore.
Shader "Game/Terrain"
{
	Properties
	{
		_SideTopColour("Side Top Colour", Color) = (0.36, 0.34, 0.31, 1)
		_SideBottomColour("Side Bottom Colour", Color) = (0.17, 0.17, 0.19, 1)
		_SideTop("Side Gradient Top Height", Float) = 0
		_SideBottom("Side Gradient Bottom Height", Float) = -1.2
		_TopStart("Top Starts At Normal Y", Range(0, 1)) = 0.8
		_TopEnd("Top Complete At Normal Y", Range(0, 1)) = 0.95
		_CellBlend("Tile Colour Blend Width", Range(0.001, 1)) = 0.15
		_DiffuseAmount("Diffuse Amount", Range(0, 1)) = 0.25
		_ShadowCutoff("Shadow Cutoff", Range(0, 1)) = 0.3
		_ShadowSmoothness("Shadow Smoothness", Range(0.001, 1)) = 0.1
		_ShadowColour("Shadow Colour", Color) = (0.55, 0.6, 0.75, 1)
		[Header(Raked Dirt)]
		_DirtColour("Dirt Colour", Color) = (0.7, 0.47, 0.35, 1)
		_RakeLines("Grooves Per Tile", Float) = 6
		_RakeGrooveWidth("Groove Width", Range(0.05, 0.5)) = 0.17
		_RakeTurnSquareness("Turn Squareness", Range(2, 10)) = 4
		_RakeEndMargin("Groove Ends From Tile Edge", Range(0, 0.5)) = 0.1
		_RakeEndFade("Groove End Fade Length", Range(0.01, 1)) = 0.45
		_RakeWobble("Wobble (Tiles)", Range(0, 0.2)) = 0.03
		_RakeWobbleScale("Wobble Frequency", Range(0.1, 8)) = 1.3
		_RakePressureVariation("Pressure Variation", Range(0, 1)) = 0.25
		_RakeDepth("Groove Depth", Range(0, 1)) = 0.6
		_RakeGrooveShade("Groove Shade", Range(0, 1)) = 0.08
		_RakeLighting("Bump Diffuse Lighting", Range(0, 2)) = 0.35
		_DirtGrain("Grain", Range(0, 1)) = 0.2
		[HideInInspector] _TerrainCells("Tile Colours", 2D) = "white" {}
		[HideInInspector] _TerrainRake("Rake Route", 2D) = "black" {}
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			half4 _SideTopColour;
			half4 _SideBottomColour;
			float _SideTop;
			float _SideBottom;
			float _TopStart;
			float _TopEnd;
			float _CellBlend;
			float _DiffuseAmount;
			float _ShadowCutoff;
			float _ShadowSmoothness;
			half4 _ShadowColour;
			float4 _TerrainCells_TexelSize;
			float4 _TerrainRake_TexelSize;
			half4 _DirtColour;
			float _RakeLines;
			float _RakeGrooveWidth;
			float _RakeTurnSquareness;
			float _RakeEndMargin;
			float _RakeEndFade;
			float _RakeWobble;
			float _RakeWobbleScale;
			float _RakePressureVariation;
			float _RakeDepth;
			float _RakeGrooveShade;
			float _RakeLighting;
			float _DirtGrain;
		CBUFFER_END
		ENDHLSL

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_TerrainCells);
			SAMPLER(sampler_TerrainCells);

			#include "RakedDirt.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionOS : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float fogFactor : TEXCOORD3;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.positionWS = positions.positionWS;
				output.positionOS = input.positionOS.xyz;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.fogFactor = ComputeFogFactor(positions.positionCS.z);
				return output;
			}

			// Flat within each tile, blending to its neighbours only within _CellBlend of the edge. Alpha is how much
			// of it is dirt
			half4 TileColour(float2 tile)
			{
				float2 p = tile - 0.5;
				float2 cell = floor(p);
				float2 f = saturate((frac(p) - 0.5) / _CellBlend + 0.5);
				float2 uv = (cell + f + 0.5) * _TerrainCells_TexelSize.xy;
				return SAMPLE_TEXTURE2D(_TerrainCells, sampler_TerrainCells, uv);
			}

			half3 SideColour(float height)
			{
				float v = saturate((height - _SideBottom) / (_SideTop - _SideBottom));
				return lerp(_SideBottomColour.rgb, _SideTopColour.rgb, v);
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 normal = normalize(input.normalWS);
				float topness = smoothstep(_TopStart, _TopEnd, normal.y);
				Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

				half4 top = TileColour(input.positionOS.xz);
				DirtSurface dirt = RakedDirt(input.positionOS.xz, _DirtColour.rgb);
				half3 topColour = lerp(top.rgb, dirt.albedo, top.a);
				half3 albedo = lerp(SideColour(input.positionOS.y), topColour, topness);

				// The grooves bend the normal, as a height map would, wherever the top is dirt
				float3 bumpedOS = normalize(float3(dirt.tilt.x, 1, dirt.tilt.y));
				float3 bumped = normalize(lerp(normal, TransformObjectToWorldNormal(bumpedOS), top.a * topness));

				float lit = saturate(dot(bumped, light.direction)) * light.shadowAttenuation;
				float band = smoothstep(_ShadowCutoff - _ShadowSmoothness, _ShadowCutoff + _ShadowSmoothness, lit);
				half3 shade = lerp(lerp(1, _ShadowColour.rgb, _DiffuseAmount), 1, band);
				// The toon band alone would flatten the grooves wherever the sun is well above them, so the bumped
				// normal also shades them by how much more or less it faces the light than the flat surface does
				float flatLit = saturate(dot(normal, light.direction));
				shade *= 1 + (saturate(dot(bumped, light.direction)) - flatLit) * light.shadowAttenuation * _RakeLighting;

				half3 colour = albedo * shade * light.color;
				colour = MixFog(colour, input.fogFactor);
				return half4(colour, 1);
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			float4 Vert(Attributes input) : SV_POSITION
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

			#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirection = normalize(_LightPosition - positionWS);
			#else
				float3 lightDirection = _LightDirection;
			#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
			#if UNITY_REVERSED_Z
				positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#else
				positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#endif
				return positionCS;
			}

			half4 Frag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask R

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			float4 Vert(float4 positionOS : POSITION) : SV_POSITION
			{
				return TransformObjectToHClip(positionOS.xyz);
			}

			half Frag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }

			ZWrite On

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return half4(NormalizeNormalPerPixel(input.normalWS), 0);
			}
			ENDHLSL
		}
	}
}
