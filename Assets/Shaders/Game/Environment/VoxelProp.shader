// Toon lighting matching the tile "Base" shader, for the voxel FBXs (Assets/Docs/VoxelStyle.md).
// Colour comes from _Tint (one material per palette colour); baked AO is the vertex colour's R channel.
Shader "Game/VoxelProp"
{
	Properties
	{
		_Tint("Tint", Color) = (1,1,1,1)
		_DiffuseAmount("DiffuseAmount", Range(0, 1)) = 0.27
		_Shadow("Shadow", Vector) = (0.1,0.1,0.1,0)
		_Highlight("Highlight", Vector) = (0.1,0.1,0.1,0)
		_ShadowCutoff("ShadowCutoff", Range(0, 1)) = 0.19
		_HighlightCutoff("HighlightCutoff", Range(0, 1)) = 0.8
		_BoundarySmoothness("BoundarySmoothness", Range(0.001, 1)) = 0.05
		_AOStrength("AO Strength", Range(0, 1)) = 0.8
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		CBUFFER_START(UnityPerMaterial)
			half4 _Tint;
			half3 _Shadow;
			half3 _Highlight;
			half _DiffuseAmount;
			half _ShadowCutoff;
			half _HighlightCutoff;
			half _BoundarySmoothness;
			half _AOStrength;
		CBUFFER_END
		ENDHLSL

		Pass
		{
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				half ao : TEXCOORD1;
				half fog : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.ao = input.color.r;
				output.fog = ComputeFogFactor(positions.positionCS.z);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				half nDotL = dot(normalize(input.normalWS), GetMainLight().direction);

				half diffuse = saturate(lerp(1 - _DiffuseAmount, 1, nDotL * 0.5 + 0.5));
				half highlight = smoothstep(_HighlightCutoff, _HighlightCutoff + _BoundarySmoothness, nDotL);
				half shadow = smoothstep(-_ShadowCutoff, -_ShadowCutoff - _BoundarySmoothness, nDotL);
				half ao = lerp(1, input.ao, _AOStrength);

				half3 colour = (_Tint.rgb * diffuse + highlight * _Highlight - shadow * _Shadow) * ao;
				colour = MixFog(colour, input.fog);
				return half4(colour, 1);
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ColorMask R

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float4 vert(Attributes input) : SV_POSITION
			{
				UNITY_SETUP_INSTANCE_ID(input);
				return TransformObjectToHClip(input.positionOS.xyz);
			}

			half frag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}
	}
}
