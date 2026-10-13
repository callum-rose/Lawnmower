// Voxel-style models (Assets/Docs/VoxelStyle.md) whose palette colour and baked AO are packed per vertex: RGB is the
// flat palette colour (R12), A is the AO (R23), multiplied in by _AOStrength. Lit with the terrain's toon banding.
Shader "Game/Voxel Shade"
{
	Properties
	{
		_AOStrength("AO Strength", Range(0, 1)) = 1
		_DiffuseAmount("Diffuse Amount", Range(0, 1)) = 0.25
		_ShadowCutoff("Shadow Cutoff", Range(0, 1)) = 0.3
		_ShadowSmoothness("Shadow Smoothness", Range(0.001, 1)) = 0.1
		_ShadowColour("Shadow Colour", Color) = (0.55, 0.6, 0.75, 1)
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float _AOStrength;
			float _DiffuseAmount;
			float _ShadowCutoff;
			float _ShadowSmoothness;
			half4 _ShadowColour;
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

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				half4 colour : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				half4 colour : TEXCOORD2;
				float fogFactor : TEXCOORD3;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.positionWS = positions.positionWS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.colour = input.colour;
				output.fogFactor = ComputeFogFactor(positions.positionCS.z);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 normal = normalize(input.normalWS);
				half3 albedo = input.colour.rgb * lerp(1, input.colour.a, _AOStrength);

				Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
				float lit = saturate(dot(normal, light.direction)) * light.shadowAttenuation;
				float band = smoothstep(_ShadowCutoff - _ShadowSmoothness, _ShadowCutoff + _ShadowSmoothness, lit);
				half3 shade = lerp(lerp(1, _ShadowColour.rgb, _DiffuseAmount), 1, band);

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
