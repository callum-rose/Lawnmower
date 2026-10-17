// Clear, glowing, heavily refractive water.
// Everything under or around the water is read back from URP's Opaque and Depth textures (both must be enabled on the
// pipeline asset), so rocks, fish, plants and lily pads must render in the opaque queue to be refracted and reflected.
// Floating things (lily pads) should sit just above the surface; the edge glow rings them where they cut through it.
// All effects are computed in world space so neighbouring water tiles join seamlessly.
Shader "Water"
{
	Properties
	{
		[Header(Colour)]
		_ShallowTint("Shallow Tint", Color) = (0.93,0.98,1,1)
		_DeepColour("Deep Colour", Color) = (0.5,0.72,1,1)
		_Absorption("Absorption Per Unit (RGB)", Vector) = (0.6,0.32,0.2,0)
		_AbsorptionScale("Absorption Scale", Range(0, 8)) = 1
		_InscatterAmount("Haze Amount", Range(0, 1)) = 0.7
		_HazeDensity("Haze Density", Range(0, 10)) = 1.8
		_DepthBlur("Depth Blur (world units per unit depth)", Range(0, 0.5)) = 0.12
		_DepthBlurMax("Depth Blur Max (world units)", Range(0, 0.5)) = 0.2

		[Header(Surface Flow)]
		_AnimationSpeed("Animation Speed", Range(0, 2)) = 0.5
		_FlowWaveAmount("Flow Waves Amount", Range(0, 1)) = 0
		_WaveScale("Wave Scale", Float) = 0.75
		_WaveStrength("Wave Strength", Range(0, 4)) = 1
		_FlowDirection("Flow Direction (XY)", Vector) = (0.35,0.2,0,0)
		_FlowSpeed("Flow Speed", Float) = 0.35
		_SwirlStrength("Swirl Strength", Range(0, 2)) = 0.7
		_SwirlScale("Swirl Scale", Float) = 0.45
		_SwirlSpeed("Swirl Speed", Float) = 0.25

		[Header(Ripples)]
		_RippleAmplitude("Ripple Amplitude", Range(0, 0.5)) = 0.2

		[Header(Refraction)]
		_RefractionStrength("Refraction Strength", Range(0, 2)) = 0.45
		_ChromaticAberration("Chromatic Aberration", Range(0, 1)) = 0.35

		[Header(Caustics)]
		[HDR] _CausticColour("Caustic Colour", Color) = (1.3,1.12,0.75,1)
		_CausticIntensity("Caustic Intensity", Range(0, 4)) = 1
		_CausticFocusDepth("Caustic Focus Depth", Range(0.05, 2)) = 0.5
		_CausticDepthFade("Caustic Depth Fade", Range(0, 4)) = 0.5
		_UnderwaterShadow("Underwater Shadow Darkening", Range(0, 1)) = 0.35

		[Header(Reflection)]
		_SkyHorizon("Sky Horizon", Color) = (0.95,0.96,1,1)
		_SkyZenith("Sky Zenith", Color) = (0.7,0.83,1,1)
		_FresnelBias("Fresnel Bias", Range(0, 1)) = 0.12
		_FresnelPower("Fresnel Power", Range(0.5, 8)) = 2.5
		_ReflectionStrength("Reflection Strength", Range(0, 2)) = 1
		_ObjectReflectionStrength("Object Reflection Strength", Range(0, 1)) = 0.55
		_ReflectionDistortion("Object Reflection Distortion", Range(0, 1)) = 0.15
		_ReflectionBlur("Object Reflection Blur (mip level)", Range(0, 5)) = 1.5
		[HDR] _SpecularColour("Specular Colour", Color) = (1.2,1.15,1,1)
		_SpecularPower("Specular Power", Range(8, 2048)) = 400
		_SparkleDensity("Sparkle Density", Float) = 9
		_SparkleAmount("Sparkle Amount", Range(0, 1)) = 0.3
		_SparkleIntensity("Sparkle Intensity", Range(0, 4)) = 2

		[Header(Edge And Glow)]
		[HDR] _EdgeColour("Edge Glow Colour", Color) = (1.1,1.05,0.95,1)
		_EdgeWidth("Edge Glow Width", Range(0.001, 1)) = 0.2
		_EdgeIntensity("Edge Glow Intensity", Range(0, 2)) = 0.45
		_EdgeNoiseScale("Edge Noise Scale", Float) = 4
		_EdgeNoiseStrength("Edge Noise Strength", Range(0, 2)) = 1
		_EdgeBreakup("Edge Breakup", Range(0, 1)) = 0.7
		_EdgeRippleInfluence("Edge Ripple Influence", Range(0, 2)) = 0.6
		_EdgeNoiseSpeed("Edge Noise Speed", Float) = 0.35
		[HDR] _GlowColour("Glow Colour", Color) = (0.9,0.95,1,1)
		_GlowAmount("Glow Amount", Range(0, 1)) = 0.12
		_CrestGlow("Crest Glow", Range(0, 2)) = 0.9

		[Header(Fresnel Rim)]
		[HDR] _FresnelRimColour("Fresnel Rim Colour", Color) = (0.85,0.9,1.2,1)
		_FresnelRimIntensity("Fresnel Rim Intensity", Range(0, 2)) = 0.35
		_FresnelRimPower("Fresnel Rim Power", Range(0.5, 8)) = 3
		_FresnelRimSlope("Fresnel Rim Slope Boost", Range(0, 40)) = 3

		[Header(Toon)]
		_ToonSteps("Toon Steps (0 is smooth)", Range(0, 8)) = 0
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10" }

		Pass
		{
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend One Zero
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float4 _ShallowTint;
			float4 _DeepColour;
			float4 _Absorption;
			float _AbsorptionScale;
			float _InscatterAmount;
			float _HazeDensity;
			float _DepthBlur;
			float _DepthBlurMax;
			float _FlowWaveAmount;
			float _WaveScale;
			float _WaveStrength;
			float4 _FlowDirection;
			float _FlowSpeed;
			float _SwirlStrength;
			float _SwirlScale;
			float _SwirlSpeed;
			float _RefractionStrength;
			float _ChromaticAberration;
			float4 _CausticColour;
			float _CausticIntensity;
			float _CausticFocusDepth;
			float _CausticDepthFade;
			float _UnderwaterShadow;
			float4 _SkyHorizon;
			float4 _SkyZenith;
			float _FresnelBias;
			float _FresnelPower;
			float _ReflectionStrength;
			float _ObjectReflectionStrength;
			float _ReflectionDistortion;
			float _ReflectionBlur;
			float _AnimationSpeed;
			float _RippleAmplitude;
			float4 _SpecularColour;
			float _SpecularPower;
			float _SparkleDensity;
			float _SparkleAmount;
			float _SparkleIntensity;
			float4 _EdgeColour;
			float _EdgeWidth;
			float _EdgeIntensity;
			float _EdgeNoiseScale;
			float _EdgeNoiseStrength;
			float _EdgeBreakup;
			float _EdgeRippleInfluence;
			float _EdgeNoiseSpeed;
			float4 _GlowColour;
			float _GlowAmount;
			float _CrestGlow;
			float4 _FresnelRimColour;
			float _FresnelRimIntensity;
			float _FresnelRimPower;
			float _FresnelRimSlope;
			float _ToonSteps;
			CBUFFER_END

			TEXTURE2D(_PlanarReflectionTexture);
			float _PlanarReflectionEnabled;

			// Ripple height field from WaterRippleSimulation; rect is world xz origin and size.
			TEXTURE2D(_WaterRippleSimTex);
			float4 _WaterRippleSimRect;
			float _WaterRippleSimEnabled;

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 Hash22(float2 p)
			{
				float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
				p3 += dot(p3, p3.yzx + 33.33);
				return frac((p3.xx + p3.yz) * p3.zy);
			}

			float Hash21(float2 p)
			{
				float3 p3 = frac(float3(p.xyx) * 0.1031);
				p3 += dot(p3, p3.yzx + 33.33);
				return frac((p3.x + p3.y) * p3.z);
			}

			// Gradient noise in roughly [-0.7, 0.7].
			float GradientNoise(float2 p)
			{
				float2 i = floor(p);
				float2 f = frac(p);
				float2 u = f * f * f * (f * (f * 6 - 15) + 10);
				float2 ga = Hash22(i) * 2 - 1;
				float2 gb = Hash22(i + float2(1, 0)) * 2 - 1;
				float2 gc = Hash22(i + float2(0, 1)) * 2 - 1;
				float2 gd = Hash22(i + float2(1, 1)) * 2 - 1;
				float a = dot(ga, f);
				float b = dot(gb, f - float2(1, 0));
				float c = dot(gc, f - float2(0, 1));
				float d = dot(gd, f - float2(1, 1));
				return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
			}

			float3 Hash33(float3 p)
			{
				p = frac(p * float3(0.1031, 0.1030, 0.0973));
				p += dot(p, p.yxz + 33.33);
				return frac((p.xxy + p.yxx) * p.zyx);
			}

			// 3D gradient noise in roughly [-0.8, 0.8].
			float GradientNoise3D(float3 p)
			{
				float3 i = floor(p);
				float3 f = frac(p);
				float3 u = f * f * f * (f * (f * 6 - 15) + 10);
				float n000 = dot(Hash33(i) * 2 - 1, f);
				float n100 = dot(Hash33(i + float3(1, 0, 0)) * 2 - 1, f - float3(1, 0, 0));
				float n010 = dot(Hash33(i + float3(0, 1, 0)) * 2 - 1, f - float3(0, 1, 0));
				float n110 = dot(Hash33(i + float3(1, 1, 0)) * 2 - 1, f - float3(1, 1, 0));
				float n001 = dot(Hash33(i + float3(0, 0, 1)) * 2 - 1, f - float3(0, 0, 1));
				float n101 = dot(Hash33(i + float3(1, 0, 1)) * 2 - 1, f - float3(1, 0, 1));
				float n011 = dot(Hash33(i + float3(0, 1, 1)) * 2 - 1, f - float3(0, 1, 1));
				float n111 = dot(Hash33(i + float3(1, 1, 1)) * 2 - 1, f - float3(1, 1, 1));
				return lerp(
					lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
					lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
			}

			float3 Quantise(float3 x)
			{
				if (_ToonSteps < 0.5)
					return x;
				float3 s = x * _ToonSteps;
				return (floor(s) + smoothstep(0.4, 0.6, frac(s))) / _ToonSteps;
			}

			// Slowly evolving vector field that bends the flow into eddies.
			float2 Swirl(float2 p, float t)
			{
				float2 q = p * _SwirlScale;
				float ts = t * _SwirlSpeed;
				return float2(
					GradientNoise(q + float2(0, ts)),
					GradientNoise(q + float2(5.2, 1.3) - float2(ts, 0)));
			}

			float Ripples(float2 p)
			{
				if (_WaterRippleSimEnabled < 0.5)
					return 0;
				float2 uv = (p - _WaterRippleSimRect.xy) / _WaterRippleSimRect.zw;
				if (any(uv < 0) || any(uv > 1))
					return 0;
				return SAMPLE_TEXTURE2D_LOD(_WaterRippleSimTex, sampler_LinearClamp, uv, 0).r * _RippleAmplitude;
			}

			float WaterHeight(float2 p, float t)
			{
				float ripples = Ripples(p);
				float2 flow = _FlowDirection.xy * _FlowSpeed * t;
				p += Swirl(p, t) * _SwirlStrength;
				float2 q = (p - flow) * _WaveScale;
				float h = GradientNoise(q) * 0.5;
				q = mul(float2x2(0.8, 0.6, -0.6, 0.8), q) * 2.03 + float2(1.7, 9.2) - flow * 0.7;
				h += GradientNoise(q + float2(t * 0.11, 0)) * 0.27;
				q = mul(float2x2(0.8, -0.6, 0.6, 0.8), q) * 2.11 + float2(8.3, 2.8);
				h += GradientNoise(q - float2(0, t * 0.17)) * 0.13;
				return h * _FlowWaveAmount + ripples;
			}

			// Light focused by the ripples: bright under crests (converging) and dim under troughs, from the curvature of the
			// surface where sunlight entered above this floor point. Focusing builds with depth up to _CausticFocusDepth.
			float RippleCaustic(float2 surfaceP, float depth, float spread)
			{
				float e = 0.04 + spread;
				float centre = Ripples(surfaceP);
				float curvature = (Ripples(surfaceP + float2(e, 0)) + Ripples(surfaceP - float2(e, 0))
					+ Ripples(surfaceP + float2(0, e)) + Ripples(surfaceP - float2(0, e)) - 4 * centre) / (e * e);
				return -curvature * 0.02 * saturate(depth / _CausticFocusDepth);
			}

			float3 SceneColour(float2 uv)
			{
				return SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv), 0).rgb;
			}

			float SceneEyeDepth(float2 uv)
			{
				float raw = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0).r;
				if (unity_OrthoParams.w > 0.5)
				{
					#if UNITY_REVERSED_Z
					raw = 1 - raw;
					#endif
					return lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
				}
				return LinearEyeDepth(raw, _ZBufferParams);
			}

			float EyeDepth(float3 positionWS)
			{
				return -TransformWorldToView(positionWS).z;
			}

			float2 WorldToScreenUV(float3 positionWS)
			{
				float4 cs = TransformWorldToHClip(positionWS);
				float4 ndc = cs * 0.5;
				float2 uv = (float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w) / cs.w;
				return uv;
			}

			// Whether sunlight reaching an underwater point is blocked by something solid above it (a lily pad, a rock
			// overhang, a fish), found by marching towards the light through the depth buffer. Only on-screen things block.
			float UnderwaterShadow(float3 p, float3 lightDir, float depth)
			{
				float maxDistance = (depth + 0.2) / max(lightDir.y, 0.2);
				[loop]
				for (int i = 0; i < 12; i++)
				{
					float d = 0.03 + maxDistance * (i + 1) / 12.0;
					float3 q = p + lightDir * d;
					float2 uv = WorldToScreenUV(q);
					if (any(uv < 0) || any(uv > 1))
						break;
					float diff = EyeDepth(q) - SceneEyeDepth(uv);
					if (diff > 0.03 && diff < 0.6)
						return 1;
				}
				return 0;
			}

			Varyings Vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.positionWS = positions.positionWS;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float t = _Time.y * _AnimationSpeed;
				float3 positionWS = input.positionWS;
				float2 p = positionWS.xz;

				const float e = 0.03;
				float h = WaterHeight(p, t);
				float hx = WaterHeight(p + float2(e, 0), t);
				float hz = WaterHeight(p + float2(0, e), t);
				float3 normalWS = normalize(float3(-(hx - h) / e * _WaveStrength * 0.25, 1, -(hz - h) / e * _WaveStrength * 0.25));

				float3 toCamera = GetWorldSpaceNormalizeViewDir(positionWS);
				float3 rayDir = -toCamera;
				float surfaceDepth = EyeDepth(positionWS);
				float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

				// Thickness of water behind the surface along the view ray, before and after refraction.
				// Refraction shifts the point seen through the water sideways in world space, further for deeper things.
				float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
				float rayToEye = 1 / max(0.001, dot(rayDir, cameraForward));
				float rawThickness = max(0, SceneEyeDepth(screenUV) - surfaceDepth);
				float3 rawFloorWS = positionWS + rayDir * min(rawThickness, 4) * rayToEye;
				float3 shift = float3(normalWS.x, 0, normalWS.z) * _RefractionStrength * min(positionWS.y - rawFloorWS.y, 2);
				float2 refractedUV = WorldToScreenUV(rawFloorWS + shift);
				float refractedSceneDepth = SceneEyeDepth(refractedUV);
				if (refractedSceneDepth < surfaceDepth)
				{
					refractedUV = screenUV;
					refractedSceneDepth = SceneEyeDepth(screenUV);
				}
				float thickness = max(0, refractedSceneDepth - surfaceDepth);

				float3 floorWS = positionWS + rayDir * thickness * rayToEye;
				float verticalDepth = max(0, positionWS.y - floorWS.y);

				float3 refracted;
				refracted.r = SceneColour(screenUV + (refractedUV - screenUV) * (1 + _ChromaticAberration)).r;
				refracted.g = SceneColour(refractedUV).g;
				refracted.b = SceneColour(screenUV + (refractedUV - screenUV) * (1 - _ChromaticAberration)).b;

				// Depth blur: a disc of taps around the refracted point whose world-space radius grows with depth.
				// Taps that land on things in front of the water (lily pads, the bank) are skipped so they don't smear in.
				float blurWorld = min(verticalDepth * _DepthBlur, _DepthBlurMax);
				float uvPerWorld = abs(UNITY_MATRIX_P[1][1]) * 0.5 / (unity_OrthoParams.w > 0.5 ? 1 : max(refractedSceneDepth, 0.01));
				float2 blurRadius = blurWorld * uvPerWorld * float2(_ScaledScreenParams.y / _ScaledScreenParams.x, 1);
				float blurPixels = blurRadius.y * _ScaledScreenParams.y;
				[branch]
				if (blurPixels > 0.75)
				{
					float3 blurred = 0;
					float weight = 0;
					float rotation = InterleavedGradientNoise(input.positionCS.xy, 0) * 6.2831;
					[unroll]
					for (int i = 0; i < 16; i++)
					{
						float r = sqrt((i + 0.5) / 16);
						float a = i * 2.39996 + rotation;
						float2 uv = refractedUV + float2(cos(a), sin(a)) * r * blurRadius;
						float w = SceneEyeDepth(uv) > surfaceDepth ? 1 : 0;
						blurred += SceneColour(uv) * w;
						weight += w;
					}
					if (weight > 0)
						refracted = lerp(refracted, blurred / weight, saturate(blurPixels - 0.75));
				}

				Light mainLight = GetMainLight();
				// Surfaces facing away from the sun, and anything in shadow, get no caustics.
				float3 floorNormal = normalize(cross(ddy(floorWS), ddx(floorWS)));
				floorNormal = dot(floorNormal, toCamera) < 0 ? -floorNormal : floorNormal;
				float facing = saturate(dot(floorNormal, mainLight.direction));
				float shadow = verticalDepth > 0.01 ? UnderwaterShadow(floorWS, mainLight.direction, verticalDepth) : 0;
				refracted *= 1 - shadow * _UnderwaterShadow;

				float caustic = RippleCaustic(floorWS.xz + mainLight.direction.xz * verticalDepth * 0.5, verticalDepth, blurWorld)
					* facing * (1 - shadow);
				caustic = clamp(caustic * exp(-verticalDepth * _CausticDepthFade) * _CausticIntensity, -0.5, 3);
				float3 caustics = Quantise(max(caustic, 0).xxx) - max(-caustic, 0);
				refracted += refracted * caustics * _CausticColour.rgb * mainLight.color;

				float pathLength = (verticalDepth + thickness) * 0.5;
				float3 transmittance = exp(-_Absorption.rgb * _AbsorptionScale * pathLength);
				float inscatter = (1 - exp(-pathLength * _HazeDensity)) * _InscatterAmount;
				float3 underwater = refracted * transmittance * _ShallowTint.rgb;
				underwater = lerp(underwater, _DeepColour.rgb, inscatter);

				float3 reflectDir = reflect(rayDir, normalWS);
				reflectDir = normalize(float3(reflectDir.x, max(reflectDir.y, 0.08), reflectDir.z));
				float3 sky = lerp(_SkyHorizon.rgb, _SkyZenith.rgb, saturate(reflectDir.y));
				float nDotV = saturate(dot(normalWS, toCamera));
				float fresnel = saturate(saturate(_FresnelBias + (1 - _FresnelBias) * pow(1 - nDotV, _FresnelPower)) * _ReflectionStrength);
				float3 colour = lerp(underwater, sky, fresnel);

				// Mirrored scene from PlanarWaterReflection, rippled by the surface normal. Objects reflect more strongly
				// than physically plausible so they read clearly from the steep game camera.
				[branch]
				if (_PlanarReflectionEnabled > 0.5)
				{
					float uvPerWorld = abs(UNITY_MATRIX_P[1][1]) * 0.5 / (unity_OrthoParams.w > 0.5 ? 1 : surfaceDepth);
					float2 reflectUV = screenUV + normalWS.xz * _ReflectionDistortion * uvPerWorld * float2(_ScaledScreenParams.y / _ScaledScreenParams.x, 1);
					// Mip-blurred, with four offset taps to smooth out the blockiness of the lower mips.
					float2 texel = exp2(_ReflectionBlur) / _ScaledScreenParams.xy;
					float4 planar = SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_LinearClamp, reflectUV + texel * float2(0.5, 1), _ReflectionBlur)
						+ SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_LinearClamp, reflectUV + texel * float2(-1, 0.5), _ReflectionBlur)
						+ SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_LinearClamp, reflectUV + texel * float2(-0.5, -1), _ReflectionBlur)
						+ SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_LinearClamp, reflectUV + texel * float2(1, -0.5), _ReflectionBlur);
					planar *= 0.25;
					colour = lerp(colour, planar.rgb, saturate(planar.a) * saturate(fresnel + _ObjectReflectionStrength));
				}

				float3 halfDir = normalize(mainLight.direction + toCamera);
				float specular = pow(saturate(dot(normalWS, halfDir)), _SpecularPower) * _SpecularPower * 0.02;
				colour += Quantise(saturate(specular)) * _SpecularColour.rgb * mainLight.color;

				float2 cell = floor(p * _SparkleDensity);
				float2 cellUV = frac(p * _SparkleDensity) - 0.5;
				float sparkleSeed = Hash21(cell);
				float twinkle = saturate(sin(t * (2 + sparkleSeed * 3) + sparkleSeed * 40) * 2 - 1);
				cellUV += (Hash22(cell + 17) - 0.5) * 0.5;
				float2 a = abs(cellUV);
				float star = saturate(1 - length(cellUV) * 7) + saturate(1 - a.x * 30) * saturate(1 - a.y * 4) + saturate(1 - a.y * 30) * saturate(1 - a.x * 4);
				float crest = saturate(h * 4 - 0.3);
				float sparkle = step(1 - _SparkleAmount, sparkleSeed) * twinkle * star * crest;
				colour += sparkle * _SparkleIntensity * _SpecularColour.rgb;

				colour += _GlowColour.rgb * (_GlowAmount + saturate(h + 0.1) * _CrestGlow * 0.3);

				// Stylised fresnel rim. From the steep game camera the surface is seen nearly head-on, so the grazing term
				// alone barely shows; the slope term lights ripple faces tipped away from the camera compared with flat water.
				float flatNDotV = saturate(toCamera.y);
				float rim = pow(1 - nDotV, _FresnelRimPower) + saturate((flatNDotV - nDotV) * _FresnelRimSlope);
				colour += Quantise(saturate(rim)) * _FresnelRimColour.rgb * _FresnelRimIntensity;

				// Shallow-water glow: the band's reach wobbles with layered noise and lacy gaps open up inside it, with a
				// thin solid line kept right where the water touches things. The noise is 3D and only moves along Y, so the
				// pattern reshapes in place instead of visibly sliding across the surface.
				float3 edgeP = float3(p.x * _EdgeNoiseScale, t * _EdgeNoiseSpeed, p.y * _EdgeNoiseScale);
				float reach = GradientNoise3D(edgeP) * 0.55
					+ GradientNoise3D(edgeP * float3(2.3, 1.3, 2.3) + float3(0, 0, 3.7)) * 0.3
					+ GradientNoise3D(edgeP * float3(5.3, 1.7, 5.3) + float3(8.1, 0, 0)) * 0.15;
				float lace = GradientNoise3D(edgeP * float3(3.1, 1.5, 3.1) + float3(reach * 2, 0, 0)) * 0.5
					+ GradientNoise3D(edgeP * float3(7.7, 2, 7.7) + float3(1.3, 0, 5.1)) * 0.25;
				// Ripple crests deepen the water at the edge and troughs make it shallower, so the band breathes with them.
				// Clamped so deep troughs out in open water (e.g. a splash) can't make it look shallow.
				float shore = (rawThickness + clamp(Ripples(p) * _EdgeRippleInfluence, -_EdgeWidth, _EdgeWidth)) / _EdgeWidth;
				float band = saturate(1 - shore + reach * _EdgeNoiseStrength);
				float foam = band * band * lerp(1, smoothstep(-0.05, 0.2, lace + band * 0.3 - 0.15), _EdgeBreakup);
				float contact = saturate(1 - rawThickness / (_EdgeWidth * 0.15));
				colour += Quantise(saturate(foam + contact * contact * 0.6)) * _EdgeColour.rgb * _EdgeIntensity;

				return half4(colour, 1);
			}
			ENDHLSL
		}
	}
}
