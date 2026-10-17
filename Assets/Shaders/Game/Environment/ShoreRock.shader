// Toon lighting matching Game/VoxelProp, for the smooth shore rocks (Assets/Models/Terrain/Blender/generate_shore_rocks.py).
// Stone colour is the vertex colour, multiplied by _Tint. UV0 carries the baked shade: U is AO, V the moss mask, smooth
// across the mesh with 0.5 at a patch's edge. The mask is thresholded per pixel, its edge broken up by noise, so moss
// edges stay crisp however coarse the mesh, and the moss is textured with two scales of object-space noise: darker and
// lighter clumps, and small bright tips like the lawn's, more muted.
Shader "Game/ShoreRock"
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
		_AOStrength("AO Strength", Range(0, 1)) = 0.55

		[Header(Moss)]
		_MossDark("Moss Dark", Color) = (0.33, 0.43, 0.17, 1)
		_MossLight("Moss Light", Color) = (0.47, 0.57, 0.24, 1)
		_MossTip("Moss Tip", Color) = (0.6, 0.69, 0.32, 1)
		_MossScale("Moss Clump Scale", Float) = 22
		_MossTipScale("Moss Tip Scale", Float) = 70
		_MossTipAmount("Moss Tip Amount", Range(0, 1)) = 0.35
		_MossEdgeSoftness("Moss Edge Softness", Range(0.001, 0.2)) = 0.06
		_MossEdgeNoise("Moss Edge Breakup", Range(0, 0.5)) = 0.1
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
			half4 _MossDark;
			half4 _MossLight;
			half4 _MossTip;
			float _MossScale;
			float _MossTipScale;
			half _MossTipAmount;
			half _MossEdgeSoftness;
			half _MossEdgeNoise;
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
				float2 shade : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				half3 colour : TEXCOORD1;
				float2 shade : TEXCOORD2;
				float3 positionOS : TEXCOORD3;
				half fog : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float Hash(float3 p)
			{
				p = frac(p * 0.3183099 + 0.1);
				p *= 17.0;
				return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
			}

			// Smooth value noise in 0..1
			float ValueNoise(float3 p)
			{
				float3 i = floor(p);
				float3 f = frac(p);
				f = f * f * (3 - 2 * f);
				return lerp(
					lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x),
						lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
					lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
						lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
			}

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.colour = input.color.rgb;
				output.shade = input.shade;
				output.positionOS = input.positionOS.xyz;
				output.fog = ComputeFogFactor(positions.positionCS.z);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float3 p = input.positionOS;

				float clump = ValueNoise(p * _MossScale);
				float tip = ValueNoise(p * _MossTipScale + 17.3);
				float edge = input.shade.y + (ValueNoise(p * _MossScale * 2.3 + 5.1) - 0.5) * _MossEdgeNoise;
				half moss = smoothstep(0.5 - _MossEdgeSoftness, 0.5 + _MossEdgeSoftness, edge);

				half3 mossColour = lerp(_MossDark.rgb, _MossLight.rgb, smoothstep(0.25, 0.75, clump));
				mossColour = lerp(mossColour, _MossTip.rgb, smoothstep(0.62, 0.85, tip) * _MossTipAmount);
				half3 albedo = lerp(input.colour * _Tint.rgb, mossColour, moss);
				half ao = lerp(1, input.shade.x, _AOStrength);

				half nDotL = dot(normalize(input.normalWS), GetMainLight().direction);
				half diffuse = saturate(lerp(1 - _DiffuseAmount, 1, nDotL * 0.5 + 0.5));
				half highlight = smoothstep(_HighlightCutoff, _HighlightCutoff + _BoundarySmoothness, nDotL);
				half shadow = smoothstep(-_ShadowCutoff, -_ShadowCutoff - _BoundarySmoothness, nDotL);

				half3 colour = (albedo * diffuse + highlight * _Highlight - shadow * _Shadow) * ao;
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
