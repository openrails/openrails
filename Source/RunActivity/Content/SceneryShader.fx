// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 World;                // model -> world
float4x4 View;                 // world -> view
float4x4 WorldViewProjection;  // model -> world -> view -> projection

// Shadow map values
float4x4 LightViewProjectionShadowProjection0;  // world -> light view -> light projection -> shadow map projection
float4x4 LightViewProjectionShadowProjection1;
float4x4 LightViewProjectionShadowProjection2;
float4x4 LightViewProjectionShadowProjection3;
float4 ShadowMapLimit;
texture  ShadowMapTexture0;
texture  ShadowMapTexture1;
texture  ShadowMapTexture2;
texture  ShadowMapTexture3;

// Z-bias and lighting coeffecients
float3 ZBias_Lighting;  // x = z-bias, y = diffuse, z = specular

// Fog values
float4 Fog;  // rgb = color of fog; a = distance from camera, everything is
             // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.

float3 LightVector;  // Direction vector to sun (world)

// Headlight values
float4 HeadlightPosition;   // xyz = position; w = lighting fading.
float4 HeadlightDirection;  // xyz = direction (length = distance to light); w = min dot product.
float4 HeadlightColor;      // rgba = color

float  overcast;       // Lower saturation & brightness when overcast
float3 viewerPos;      // Viewer's world coordinates.
bool   ImageTextureIsNight;    // Using night texture

texture ImageTexture;
sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};

texture OverlayTexture;
sampler Overlay = sampler_state
{
	Texture = (OverlayTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	MipMapLodBias = 0;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler ShadowMap0 = sampler_state
{
	Texture = (ShadowMapTexture0);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler ShadowMap1 = sampler_state
{
	Texture = (ShadowMapTexture1);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler ShadowMap2 = sampler_state
{
	Texture = (ShadowMapTexture2);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler ShadowMap3 = sampler_state
{
	Texture = (ShadowMapTexture3);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float4 Color     : COLOR0;
	float3 Normal    : NORMAL;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;  // position x, y, z, w
	float3 RelPosition  : TEXCOORD0; // rel position x, y, z
	float2 TexCoords    : TEXCOORD1; // tex coords x, y
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 LightDir_Fog : TEXCOORD3; // light dir x, y, z; fog fade
	float4 Shadow       : TEXCOORD4; // ps2<shadow map texture and depth x, y, z> ps3<abs position x, y, z, w>
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition.xyz = mul(In.Position, World) - viewerPos;
	Out.TexCoords.xy = In.TexCoords;
	Out.Color = In.Color;
	Out.Normal_Light.xyz = mul(In.Normal, World).xyz;
}

void _VSLightsAndShadows(uniform bool ShaderModel3, in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Normal lighting (range 0.0 - 1.0)
	Out.Normal_Light.w = dot(normalize(Out.Normal_Light.xyz), LightVector) * 0.5 + 0.5;

	// Headlight lighting
	Out.LightDir_Fog.xyz = mul(In.Position, World) - HeadlightPosition.xyz;

	// Fog fading
	Out.LightDir_Fog.w = saturate((length(Out.Position.xyz) - Fog.a) / Fog.a);

	// Absolute position for shadow mapping
	if (ShaderModel3) {
		Out.Shadow = mul(In.Position, World);
	} else {
		Out.Shadow.xyz = mul(mul(In.Position, World), LightViewProjectionShadowProjection0).xyz;
	}
}

VERTEX_OUTPUT VSGeneral(uniform bool ShaderModel3, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTerrain(uniform bool ShaderModel3, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In, Out);
	return Out;
}

VERTEX_OUTPUT VSForest(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 eyeVector = normalize(View._m02_m12_m22);
	float3 upVector = float3(0, -1, 0);
	float3 sideVector = normalize(cross(eyeVector, upVector));

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position;
	newPosition += (In.TexCoords.x - 0.5f) * sideVector * In.Normal.x;
	newPosition += (In.TexCoords.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.xyz = eyeVector;

	_VSLightsAndShadows(false, In, Out);

	return Out;
}

VERTEX_OUTPUT VSSignalLight(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);

	// Apply a small z-bias so that lights are always on top of the shape.
	Out.Position.z *= 0.9999;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// Gets the ambient light effect.
float _PSGetAmbientEffect(in VERTEX_OUTPUT In)
{
	return In.Normal_Light.w * ZBias_Lighting.y;
}

// Gets the vegetation ambient light effect.
float _PSGetVegetationAmbientEffect(in VERTEX_OUTPUT In)
{
	return ZBias_Lighting.y;
}

// Gets the specular light effect.
float _PSGetSpecularEffect(in VERTEX_OUTPUT In)
{
	float3 halfVector = normalize(-In.RelPosition.xyz) + LightVector;
	return In.Normal_Light.w * step(1, ZBias_Lighting.z) * pow(saturate(dot(normalize(In.Normal_Light.xyz), normalize(halfVector))), ZBias_Lighting.z);
}

// Gets the shadow effect.
float3 _PS2GetShadowEffect(in VERTEX_OUTPUT In)
{
	return float3(tex2D(ShadowMap0, In.Shadow.xy).xy, In.Shadow.z);
}
float3 _PS3GetShadowEffect(in VERTEX_OUTPUT In)
{
	float depth = length(In.RelPosition);
	float3 rv;
	if (depth < ShadowMapLimit.x) {
		float3 pos0 = mul(In.Shadow, LightViewProjectionShadowProjection0).xyz;
		rv = float3(tex2D(ShadowMap0, pos0.xy).xy, pos0.z);
	} else {
		if (depth < ShadowMapLimit.y) {
			float3 pos1 = mul(In.Shadow, LightViewProjectionShadowProjection1).xyz;
			rv = float3(tex2D(ShadowMap1, pos1.xy).xy, pos1.z);
		} else {
			if (depth < ShadowMapLimit.z) {
				float3 pos2 = mul(In.Shadow, LightViewProjectionShadowProjection2).xyz;
				rv = float3(tex2D(ShadowMap2, pos2.xy).xy, pos2.z);
			} else {
				if (depth < ShadowMapLimit.w) {
					float3 pos3 = mul(In.Shadow, LightViewProjectionShadowProjection3).xyz;
					rv = float3(tex2D(ShadowMap3, pos3.xy).xy, pos3.z);
				}
			}
		}
	}
	return rv;
}
//void _PSApplyShadowColor(inout float3 Color, in VERTEX_OUTPUT In)
//{
//	float depth = length(In.RelPosition);
//	if (depth < ShadowMapLimit.x) {
//		Color.rgb *= 0.9;
//		Color.r += 0.1;
//	} else {
//		if (depth < ShadowMapLimit.y) {
//			Color.rgb *= 0.9;
//			Color.g += 0.1;
//		} else {
//			if (depth < ShadowMapLimit.z) {
//				Color.rgb *= 0.9;
//				Color.b += 0.1;
//			} else {
//				if (depth < ShadowMapLimit.w) {
//					Color.rgb *= 0.9;
//					Color.rg += 0.1;
//				}
//			}
//		}
//	}
//}
float _PSGetShadowEffect(uniform bool ShaderModel3, uniform bool NormalLighting, in VERTEX_OUTPUT In)
{
	float3 moments;
	if (ShaderModel3)
		moments = _PS3GetShadowEffect(In);
	else
		moments = _PS2GetShadowEffect(In);

	bool not_shadowed = (moments.z <= moments.x);
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.0002, 1.0);
	float m_d = moments.x - moments.z;
	float p = pow(variance / (variance + m_d * m_d), 20);
	if (NormalLighting)
		return saturate(not_shadowed + p) * saturate(In.Normal_Light.w * 5 - 2);
	return saturate(not_shadowed + p);
}

// Gets the overcast effect.
float3 _PSGetOvercastEffect()
{
	return overcast;
}

// Gets the overcast color.
float3 _PSGetOvercastColor(in float4 Color, in VERTEX_OUTPUT In)
{
	// Value used to determine equivalent grayscale color.
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);

	float intensity = dot(Color, LumCoeff);
	return lerp(intensity, Color.rgb, 0.8) * 0.5;
}

// Gets the night-time effect.
float _PSGetNightEffect()
{
	// The following constants define the beginning and the end conditions of
	// the day-night transition. Values refer to the Y postion of LightVector.
	const float startNightTrans = 0.1;
	const float finishNightTrans = -0.1;
	return saturate((LightVector.y - finishNightTrans) / (startNightTrans - finishNightTrans));
}

// Applies the lighting effect of the train's headlights, including
// fade-in/fade-out animations.
void _PSApplyHeadlights(inout float3 Color, in float3 OriginalColor, in VERTEX_OUTPUT In)
{
	float3 surfaceNormal = normalize(In.Normal_Light.xyz);
	float3 headlightToSurface = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(headlightToSurface, normalize(HeadlightDirection.xyz));

	float shading = step(0, coneDot);
	shading *= step(0, dot(surfaceNormal, -headlightToSurface));
	shading *= saturate(0.5 * (1 - HeadlightDirection.w) / (1 - coneDot)); // We want 50% brightness at the given dot product.
	shading *= saturate(1 - length(In.LightDir_Fog.xyz) / length(HeadlightDirection.xyz));
	shading *= HeadlightPosition.w;
	Color += OriginalColor * HeadlightColor.rgb * HeadlightColor.a * shading;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float3 Color, in VERTEX_OUTPUT In)
{
	Color = lerp(Color, Fog.rgb, In.LightDir_Fog.w);
}

float4 PSImage(uniform bool ShaderModel3, in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In) + ImageTextureIsNight));
	// Specular effect next.
	litColor += _PSGetSpecularEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In);
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + ImageTextureIsNight));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	//if (ShaderModel3) _PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSVegetation(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Ambient effect applies first; no shadow effect for vegetation; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetVegetationAmbientEffect(In) + ImageTextureIsNight));
	// No specular effect for vegetation.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + ImageTextureIsNight));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain(uniform bool ShaderModel3, in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In) + ImageTextureIsNight));
	// No specular effect for terrain.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + ImageTextureIsNight));
	// Overlay image for terrain.
	litColor.rgb -= tex2D(Overlay, In.TexCoords.xy * 32) / 2 - 0.25;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Fixed ambient and shadow effects at darkest level.
	float3 litColor = Color.rgb * ShadowBrightness;
	// No specular effect for dark shade.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + ImageTextureIsNight));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSHalfBright(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float HalfShadowBrightness = 0.75;
	const float HalfNightBrightness = 0.6;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Fixed ambient and shadow effects at mid-dark level.
	float3 litColor = Color.rgb * HalfShadowBrightness;
	// No specular effect for half-bright.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect() / 2);
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(HalfNightBrightness, FullBrightness, saturate(_PSGetNightEffect() + ImageTextureIsNight));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSFullBright(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Fixed ambient and shadow effects at brightest level.
	float3 litColor = Color.rgb;
	// No specular effect for full-bright.
	// No overcast effect for full-bright.
	// No night-time effect for full-bright.
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSSignalLight(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
	// No ambient and shadow effects for signal lights.
	// Apply signal coloring effect.
	float3 litColor = lerp(Color.rgb, In.Color.rgb, Color.r);
	// No specular effect, overcast effect, night-time darkening, headlights or fogging effect for signal lights.
	return float4(litColor, Color.a);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique ImagePS2 {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral(false);
		PixelShader = compile ps_2_0 PSImage(false);
	}
}

technique ImagePS3 {
	pass Pass_0 {
		VertexShader = compile vs_3_0 VSGeneral(true);
		PixelShader = compile ps_3_0 PSImage(true);
	}
}

technique Forest {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSForest();
		PixelShader = compile ps_2_0 PSVegetation();
	}
}

technique Vegetation {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral(false);
		PixelShader = compile ps_2_0 PSVegetation();
	}
}

technique TerrainPS2 {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSTerrain(false);
		PixelShader = compile ps_2_0 PSTerrain(false);
	}
}

technique TerrainPS3 {
	pass Pass_0 {
		VertexShader = compile vs_3_0 VSTerrain(true);
		PixelShader = compile ps_3_0 PSTerrain(true);
	}
}

technique DarkShade {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral(false);
		PixelShader = compile ps_2_0 PSDarkShade();
	}
}

technique HalfBright {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral(false);
		PixelShader = compile ps_2_0 PSHalfBright();
	}
}

technique FullBright {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral(false);
		PixelShader = compile ps_2_0 PSFullBright();
	}
}

technique SignalLight {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSSignalLight();
		PixelShader = compile ps_2_0 PSSignalLight();
	}
}
