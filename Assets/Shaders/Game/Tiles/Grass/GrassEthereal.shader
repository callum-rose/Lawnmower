// Soft, translucent grass with glowing tips (Pixar's Soul "Great Before" look).
// Vertex wind/bend/mower motion and toon lighting match Grass.shader, so materials can switch between the two.
Shader "Grass Ethereal"
{
	Properties
	{
		_MainTex2("MainTex", 2D) = "white" {}
		_Tint2("Tint", Color) = (1,1,1,0)
		_DiffuseAmount2("DiffuseAmount", Range(0, 1)) = 0.2
		_Shadow2("Shadow", Vector) = (0.1,0.1,0.1,0)
		_Highlight2("Highlight", Vector) = (0.1,0.1,0.1,0)
		_ShadowCutoff2("ShadowCutoff", Range(0, 1)) = 0.2
		_HighlightCutoff2("HighlightCutoff", Range(0, 1)) = 0.8
		_BoundarySmoothness2("BoundarySmoothness", Range(0.001, 1)) = 0.1
		[Toggle(_USEAO2_ON)] _UseAO2("Use AO", Float) = 0
		_AOMap2("AO Map", 2D) = "white" {}
		_AOStrength2("AO Strength", Range(0, 1)) = 0
		_WindOffsetMagnitude("Wind Offset Magnitude", Range(0, 0.002)) = 0
		_BendOffsetMagnitude("Bend Offset Magnitude", Range(0, 0.001)) = 0

		[Header(Translucency)]
		_BaseAlpha("Base Alpha", Range(0, 1)) = 1
		_TipAlpha("Tip Alpha", Range(0, 1)) = 0.35
		_AlphaFadePower("Alpha Fade Power", Range(0.1, 8)) = 4

		[Header(Variation)]
		_BodyColumn("Body Colour Column", Range(0, 1)) = 0.375
		_BodyVariation("Body Colour Variation", Range(0, 1)) = 0.25
		_GlowHueVariation("Glow Hue Variation", Range(0, 1)) = 0.3
		_GlowBrightnessVariation("Glow Brightness Variation", Range(0, 1)) = 0.35

		[Header(Tip Glow)]
		_GlowColourA("Glow Colour A", Color) = (0.75,1,0.2,1)
		_GlowColourB("Glow Colour B", Color) = (0.2,1,0.55,1)
		_GlowColourC("Glow Colour C", Color) = (0.2,0.9,1,1)
		_GlowIntensity("Glow Intensity", Range(0, 2)) = 1.4
		_GlowPower("Glow Power", Range(0.5, 8)) = 4.5
		_GlowVariationScale("Glow Variation Scale", Float) = 4
		_SparkleChance("Sparkle Chance", Range(0, 1)) = 0.05
		_SparkleBoost("Sparkle Boost", Range(0, 4)) = 0.7

		[Header(Shimmer)]
		_ShimmerAmount("Shimmer Amount", Range(0, 1)) = 0.35
		_ShimmerSpeed("Shimmer Speed", Float) = 1.5

		[Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		Pass
		{
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_instancing
			#pragma multi_compile _GraphicsSettingsLow _GraphicsSettingsHigh
			#pragma shader_feature_local _USEAO2_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex2_ST;
			float4 _Tint2;
			float4 _AOMap2_ST;
			float3 _Highlight2;
			float3 _Shadow2;
			float _WindOffsetMagnitude;
			float _BendOffsetMagnitude;
			float _DiffuseAmount2;
			float _HighlightCutoff2;
			float _BoundarySmoothness2;
			float _ShadowCutoff2;
			float _AOStrength2;
			float _BaseAlpha;
			float _TipAlpha;
			float _AlphaFadePower;
			float4 _GlowColourA;
			float4 _GlowColourB;
			float4 _GlowColourC;
			float _GlowIntensity;
			float _GlowPower;
			float _GlowVariationScale;
			float _SparkleChance;
			float _BodyColumn;
			float _BodyVariation;
			float _GlowHueVariation;
			float _GlowBrightnessVariation;
			float _SparkleBoost;
			float _ShimmerAmount;
			float _ShimmerSpeed;
			float _ZWrite;
			float _Cull;
			CBUFFER_END

			sampler2D BakedNoiseTex;
			float WindNoiseScale;
			float2 WindNoiseVelocity;
			float BendNoiseScale;
			float3 MowerPosition;
			float MowerWindZoneWidth;
			float MowerWindSize;
			float MowerWindFlutterSpeed;
			sampler2D _MainTex2;
			sampler2D _AOMap2;

			float4 SampleSignedNoise(float2 uv)
			{
				return tex2Dlod(BakedNoiseTex, float4(uv * 0.15, 0, 0)) * 2 - 1;
			}

			float3 GetVertexOffset(float3 positionWS, float2 uv0, float2 uv1)
			{
				#if defined(_GraphicsSettingsLow)
				return 0;
				#else
				float2 windUV = uv0.x * 10 + positionWS.xz * WindNoiseScale + WindNoiseVelocity * _TimeParameters.x;
				float4 windNoise = SampleSignedNoise(windUV);
				float3 wind = float3(windNoise.r, 0, windNoise.g) * _WindOffsetMagnitude;

				float4 bendNoise = SampleSignedNoise(uv1 * BendNoiseScale);
				float3 bend = float3(bendNoise.r, 0, bendNoise.g) * _BendOffsetMagnitude;

				float3 fromMower = positionWS - MowerPosition;
				float3 fromMowerFlat = float3(fromMower.x, 0, fromMower.z);
				float zone = dot(fromMowerFlat, fromMowerFlat) / MowerWindZoneWidth;
				float falloff = exp(-zone * zone);
				float flutter = 0.75 + SampleSignedNoise(fromMower.xz + _TimeParameters.x * MowerWindFlutterSpeed).r * 0.25;
				float3 mower = float3(0, -0.001, 0) * falloff + fromMowerFlat * falloff * MowerWindSize * flutter;

				return (wind + bend + mower) * uv0.y * uv0.y;
				#endif
			}

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				input.positionOS.xyz += GetVertexOffset(positionWS, input.uv0, input.uv1);

				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.uv = float4(input.uv0, input.uv1);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv0 = input.uv.xy;
				float2 uv1 = input.uv.zw;
				float height = saturate(uv0.y);

				// Toon lighting, as in Grass.shader
				float nDotL = dot(_MainLightPosition.xyz, input.normalWS);
				float diffuse = saturate((1 - _DiffuseAmount2) + (nDotL + 1) * 0.5 * _DiffuseAmount2);
				float highlight = smoothstep(_HighlightCutoff2, _HighlightCutoff2 + _BoundarySmoothness2, nDotL);
				float shadow = smoothstep(-_ShadowCutoff2, -_ShadowCutoff2 - _BoundarySmoothness2, nDotL);

				// Pull each blade's colour column towards one shared column so a tile reads as a single lawn type
				float2 bodyUV = float2(lerp(_BodyColumn, uv0.x, _BodyVariation), uv0.y);
				float3 colour = tex2D(_MainTex2, bodyUV * _MainTex2_ST.xy + _MainTex2_ST.zw).rgb * _Tint2.rgb * diffuse
					+ highlight * _Highlight2 - shadow * _Shadow2;

				#ifdef _USEAO2_ON
				float3 ao = (1 - _AOStrength2) + tex2D(_AOMap2, uv1 * _AOMap2_ST.xy + _AOMap2_ST.zw).rgb * _AOStrength2;
				colour *= ao;
				#endif

				// uv1 is constant across each blade, so it doubles as a per-blade seed
				float bladeSeed = frac(sin(dot(uv1, float2(12.9898, 78.233))) * 43758.5453);
				float patch = tex2D(BakedNoiseTex, uv1 * _GlowVariationScale * 0.15).r;

				// Cycle A -> B -> C -> A so neighbouring blades pick different hues
				float hue = frac(bladeSeed + patch * 0.6) * 3;
				float3 glowPalette = hue < 1 ? lerp(_GlowColourA.rgb, _GlowColourB.rgb, hue)
					: hue < 2 ? lerp(_GlowColourB.rgb, _GlowColourC.rgb, hue - 1)
					: lerp(_GlowColourC.rgb, _GlowColourA.rgb, hue - 2);
				float3 glowColour = lerp(_GlowColourB.rgb, glowPalette, _GlowHueVariation);
				float shimmerPhase = bladeSeed * 6.2832;
				float shimmer = 1 - _ShimmerAmount * (0.5 + 0.5 * sin(_TimeParameters.x * _ShimmerSpeed + shimmerPhase));
				float sparkle = step(1 - _SparkleChance, frac(bladeSeed * 13.17)) * _SparkleBoost;
				float bladeBrightness = lerp(1, lerp(0.35, 1, frac(bladeSeed * 7.31)), _GlowBrightnessVariation) + sparkle;
				float3 glow = glowColour * pow(height, _GlowPower) * _GlowIntensity * bladeBrightness * shimmer;

				float alpha = lerp(_BaseAlpha, _TipAlpha, pow(height, _AlphaFadePower));

				// Premultiplied: the body fades out towards the tip while the glow stays additive
				return half4(colour * alpha + glow, alpha);
			}
			ENDHLSL
		}
	}
	Fallback "Hidden/InternalErrorShader"
}
