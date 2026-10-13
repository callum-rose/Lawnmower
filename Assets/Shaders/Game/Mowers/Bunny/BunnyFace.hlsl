#ifndef BUNNY_FACE_INCLUDED
#define BUNNY_FACE_INCLUDED

// Procedural bunny face drawn with signed distance fields, so expressions can be
// blended smoothly instead of swapping decal textures.
//
// All measurements are in the 512x512 canvas of the original bunny_face textures
// (origin top-left, y down) so they can be read straight off the art.
//
// Declare these before including:
//   float4 _FaceLook;   // xy: gaze (-1..1, +y looks down), z: converge (cross-eyed amount)
//   float4 _FaceEyes;   // x: left eye open, y: right eye open, z: pupil scale, w: right eye shading
//   float4 _FaceMouth;  // x: mouth open, y: mouth width scale, z: nose squash (-1..1), w: smile (+) / frown (-)
//   float4 _FaceLids;   // x: lids lowered (0..1), y: lid tilt (+ angry, - sad), z: happy ^ ^ eyes (0..1), w: eye scale
//   float4 _FaceDarkColor, _FaceEyeColor, _FaceNoseColor, _FaceTeethColor;
//
// Kept to the HLSL/GLSL common subset (float literals, no static consts or structs)
// so the same file can be previewed in WebGL with a few #defines.

float BunnyFace_Box(float2 p, float2 halfSize, float radius)
{
	float2 q = abs(p) - halfSize + radius;
	return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

float BunnyFace_Triangle(float2 p, float2 p0, float2 p1, float2 p2)
{
	float2 e0 = p1 - p0;
	float2 e1 = p2 - p1;
	float2 e2 = p0 - p2;
	float2 v0 = p - p0;
	float2 v1 = p - p1;
	float2 v2 = p - p2;
	float2 pq0 = v0 - e0 * saturate(dot(v0, e0) / dot(e0, e0));
	float2 pq1 = v1 - e1 * saturate(dot(v1, e1) / dot(e1, e1));
	float2 pq2 = v2 - e2 * saturate(dot(v2, e2) / dot(e2, e2));
	float s = sign(e0.x * e2.y - e0.y * e2.x);
	float2 d = min(min(float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
	                   float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
	                   float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
	return -sqrt(d.x) * sign(d.y);
}

float BunnyFace_Fill(float d, float aa)
{
	return saturate(0.5 - d / aa);
}

// Premultiplied "over" compositing
void BunnyFace_Over(inout float4 acc, float3 color, float coverage)
{
	acc.rgb = acc.rgb * (1.0 - coverage) + color * coverage;
	acc.a = acc.a * (1.0 - coverage) + coverage;
}

void BunnyFace_Eye(inout float4 acc, float2 p, float aa, float centreX, float open, float2 look, float shading, float side)
{
	float radius = 72.5;
	float happy = saturate(_FaceLids.z);
	float o = min(saturate(open), 1.0 - happy);

	// Scale about the resting eye centre
	float scale = max(_FaceLids.w, 0.01);
	p = float2(centreX, 295.0) + (p - float2(centreX, 295.0)) / scale;
	aa /= scale;

	// Happy eyes bend the closed lid line into an upward arch
	float bendX = saturate(abs(p.x - centreX) / 82.5);
	p.y += happy * 32.0 * (1.0 - bendX * bendX);

	// Closing squashes the eye into a pill, then a lid line slightly below centre
	float2 centre = float2(centreX, lerp(326.0, 295.0, o));
	float2 halfSize = float2(lerp(lerp(82.5, 68.0, happy), radius, smoothstep(0.0, 1.0, o)), max(radius * o, 9.5));
	float eye = BunnyFace_Box(p - centre, halfSize, halfSize.y);

	// Lids cut the eye from the top. Positive tilt lowers the inner corners (angry),
	// negative lowers the outer corners (sad). side is +1 when the inner corner is at +x.
	float lidAmount = saturate(_FaceLids.x);
	float slope = _FaceLids.y * 0.6 * side * saturate(lidAmount * 5.0);
	float lidY = centre.y - halfSize.y + lidAmount * 2.0 * halfSize.y + slope * (p.x - centreX);
	float lid = (lidY - p.y) / sqrt(1.0 + slope * slope);
	float lidLine = max(abs(lid) - 4.5, eye);

	// Contents are drawn in unsquashed space so the pupil squashes with the lid
	float2 q = p - centre;
	q.y /= max(o, 0.05);

	float pupil = length(q - look * 26.0) - 44.0 * _FaceEyes.z;
	float shade = smoothstep(0.6, 1.25, length(q - float2(-30.0, -35.0)) / radius) * shading;

	float3 color = lerp(_FaceEyeColor.rgb * (1.0 - shade), _FaceDarkColor.rgb, BunnyFace_Fill(pupil, aa));
	color = lerp(_FaceDarkColor.rgb, color, smoothstep(0.13, 0.2, o));

	BunnyFace_Over(acc, color, BunnyFace_Fill(max(eye, lid), aa));
	BunnyFace_Over(acc, _FaceDarkColor.rgb, BunnyFace_Fill(lidLine, aa) * saturate(lidAmount * 10.0));
}

void BunnyFace_Mouth(inout float4 acc, float2 p, float aa)
{
	float t = saturate(_FaceMouth.x);

	// Bend the mouth into a smile (ends up) or frown (ends down); teeth follow it partially
	float bendX = (p.x - 256.0) / 73.0;
	float bend = _FaceMouth.w * 22.0 * bendX * bendX;
	float2 toothP = float2(p.x, p.y + bend * 0.35);
	p.y += bend;

	// Closed: thin capsule behind the teeth. Open: tall rounded rect.
	float2 centre = lerp(float2(256.0, 385.0), float2(256.0, 434.5), t);
	float2 halfSize = lerp(float2(60.0, 10.0), float2(73.0, 50.5), t);
	halfSize.x *= _FaceMouth.y;
	float mouth = BunnyFace_Box(p - centre, halfSize, lerp(10.0, 33.0, t));
	BunnyFace_Over(acc, _FaceDarkColor.rgb, BunnyFace_Fill(mouth, aa));

	// Teeth hang from the top of the mouth. They start above it and are clipped to the
	// mouth's top edge (corners included) so no dark sliver shows above them.
	float mouthTop = centre.y - halfSize.y;
	float toothBottom = mouthTop + lerp(35.0, 38.0, t);
	float2 toothHalf = float2(23.0, (toothBottom - mouthTop + 10.0) * 0.5);
	float toothY = toothBottom - toothHalf.y;
	float teeth = min(BunnyFace_Box(toothP - float2(229.0, toothY), toothHalf, 1.5),
	                  BunnyFace_Box(toothP - float2(283.0, toothY), toothHalf, 1.5));

	// Mouth shape with its bottom pushed far down, so only its top edge clips the teeth
	float clipHalfY = halfSize.y + 50.0;
	float topClip = BunnyFace_Box(p - float2(centre.x, mouthTop + clipHalfY), float2(halfSize.x, clipHalfY), lerp(10.0, 33.0, t));
	teeth = max(teeth, topClip);
	BunnyFace_Over(acc, _FaceTeethColor.rgb, BunnyFace_Fill(teeth, aa));
}

void BunnyFace_Nose(inout float4 acc, float2 p, float aa)
{
	// Squash and stretch about the nose centre for twitches
	float squash = _FaceMouth.z * 0.25;
	float2 q = (p - float2(256.0, 369.0)) / float2(1.0 + squash, 1.0 - squash);

	float nose = BunnyFace_Triangle(q, float2(-16.0, -11.0), float2(16.0, -11.0), float2(0.0, 11.0)) - 10.0;
	BunnyFace_Over(acc, _FaceNoseColor.rgb, BunnyFace_Fill(nose, aa));
}

// Returns straight (non-premultiplied) colour and coverage for the face at uv.
float4 BunnyFace(float2 uv)
{
	float2 p = float2(uv.x, 1.0 - uv.y) * 512.0;
	float2 fw = fwidth(p);
	float aa = max(0.5 * (fw.x + fw.y), 0.0001);

	float2 lookL = _FaceLook.xy + float2(_FaceLook.z, 0.0);
	float2 lookR = _FaceLook.xy - float2(_FaceLook.z, 0.0);
	lookL /= max(1.0, length(lookL));
	lookR /= max(1.0, length(lookR));

	float4 acc = float4(0.0, 0.0, 0.0, 0.0);
	BunnyFace_Eye(acc, p, aa, 160.5, _FaceEyes.x, lookL, 0.0, 1.0);
	BunnyFace_Eye(acc, p, aa, 350.5, _FaceEyes.y, lookR, _FaceEyes.w, -1.0);
	BunnyFace_Mouth(acc, p, aa);
	BunnyFace_Nose(acc, p, aa);

	return float4(acc.rgb / max(acc.a, 0.0001), acc.a);
}

#endif
