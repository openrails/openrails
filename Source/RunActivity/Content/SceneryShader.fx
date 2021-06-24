// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 World;         // model -> world
float4x4 WorldViewProjection;  // model -> world -> view -> projection
float4x4 LightViewProjectionShadowProjection0;  // world -> light view -> light projection -> shadow map projection
float4x4 LightViewProjectionShadowProjection1;
float4x4 LightViewProjectionShadowProjection2;
float4x4 LightViewProjectionShadowProjection3;
texture  ShadowMapTexture0;
texture  ShadowMapTexture1;
texture  ShadowMapTexture2;
texture  ShadowMapTexture3;
float4   ShadowMapLimit;
float4   ZBias_Lighting;  // x = z-bias, y = diffuse, z = specular, w = step(1, z)
float4   Fog;  // rgb = color of fog; a = reciprocal of distance from camera, everything is
			   // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.
float4   LightVector_ZFar;  // xyz = direction vector to sun (world), w = z-far distance
float4   HeadlightPosition;     // xyz = position; w = lighting fading.
float4   HeadlightDirection;    // xyz = normalized direction (length = distance to light); w = 0.5 * (1 - min dot product).
float    HeadlightRcpDistance;  // reciprocal length = reciprocal distance to light
float4   HeadlightColor;        // rgba = color
float2   Overcast;      // Lower saturation & brightness when overcast. x = FullBrightness, y = HalfBrightness
float3   ViewerPos;     // Viewer's world coordinates.
float    ImageTextureIsNight;
float    NightColorModifier;
float    HalfNightColorModifier;
float    VegetationAmbientModifier;
float    SignalLightIntensity;
float4   EyeVector;
float3   SideVector;
float    ReferenceAlpha;
texture  ImageTexture;
texture  OverlayTexture;
float	 OverlayScale;

sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};

sampler Overlay = sampler_state
{
	Texture = (OverlayTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	MipLodBias = 0;
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
	float3 Normal    : NORMAL;
	float4x4 Instance : TEXCOORD1;
};

struct VERTEX_INPUT_FOREST
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float3 Normal    : NORMAL;
};

struct VERTEX_INPUT_SIGNAL
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float4 Color     : COLOR0;
};

struct VERTEX_INPUT_TRANSFER
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float2 TexCoords    : TEXCOORD1; // tex coords x, y
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 LightDir_Fog : TEXCOORD3; // light dir x, y, z; fog fade
	float4 Shadow       : TEXCOORD4; // Level9_1<shadow map texture and depth x, y, z> Level9_3<abs position x, y, z, w>
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.xyz = normalize(mul(In.Normal, (float3x3)World).xyz);
	
	// Normal lighting (range 0.0 - 1.0)
	// Need to calc. here instead of _VSLightsAndShadows() to avoid calling it from VSForest(), where it has gone into pre-shader in Shaders.cs
	Out.Normal_Light.w = dot(Out.Normal_Light.xyz, LightVector_ZFar.xyz) * 0.5 + 0.5;
}

void _VSSignalProjection(uniform bool Glow, in VERTEX_INPUT_SIGNAL In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	float3 relPos = mul(In.Position, World).xyz - ViewerPos;
	// Position 1.5cm in front of signal.
	In.Position.z += 0.015;
	if (Glow) {
		// Position glow a further 1.5cm in front of the light.
		In.Position.z += 0.015;
		// The glow around signal lights scales according to distance; there is a cut-off which controls when the glow
		// starts, a scaling factor which determines how quickly it expands (logarithmically), and ZBias_Lighting.x is
		// an overall "glow power" control which determines the effectiveness of glow on any individual light. This is
		// used to have different glows in the day and night, and to prevent theatre boxes from glowing!
		const float GlowCutOffM = 100;
		const float GlowScalingFactor = 40;
		In.Position.xyz *= log(1 + max(0, length(relPos) - GlowCutOffM) / GlowScalingFactor) * ZBias_Lighting.x;
	}
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition.xyz = relPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Color = In.Color;
}

void _VSTransferProjection(in VERTEX_INPUT_TRANSFER In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.w = 1;
}

void _VSLightsAndShadows(uniform bool ShaderModel3, in float4 InPosition, inout VERTEX_OUTPUT Out)
{
	// Headlight lighting
	Out.LightDir_Fog.xyz = mul(InPosition, World).xyz - HeadlightPosition.xyz;

	// Fog fading
	Out.LightDir_Fog.w = (2.0 / (1.0 + exp(length(Out.Position.xyz) * Fog.a * -2.0))) - 1.0;

	// Absolute position for shadow mapping
	if (ShaderModel3) {
		Out.Shadow = mul(InPosition, World);
	} else {
		Out.Shadow.xyz = mul(mul(InPosition, World), LightViewProjectionShadowProjection0).xyz;
	}
}

VERTEX_OUTPUT VSGeneral(uniform bool ShaderModel3, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	
	if (ShaderModel3) {
		if (determinant(In.Instance) != 0) {
			In.Position = mul(In.Position, transpose(In.Instance));
			In.Normal = mul(In.Normal, (float3x3)transpose(In.Instance));
		}
	}

	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In.Position, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

	return Out;
}

VERTEX_OUTPUT VSGeneral9_3(in VERTEX_INPUT In)
{
    return VSGeneral(true, In);
}

VERTEX_OUTPUT VSGeneral9_1(in VERTEX_INPUT In)
{
    return VSGeneral(false, In);
}

VERTEX_OUTPUT VSTransfer(uniform bool ShaderModel3, in VERTEX_INPUT_TRANSFER In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSTransferProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In.Position, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTransfer3(in VERTEX_INPUT_TRANSFER In)
{
    return VSTransfer(true, In);
}

VERTEX_OUTPUT VSTransfer9_1(in VERTEX_INPUT_TRANSFER In)
{
    return VSTransfer(false, In);
}

VERTEX_OUTPUT VSTerrain(uniform bool ShaderModel3, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In.Position, Out);
	return Out;
}

VERTEX_OUTPUT VSTerrain9_3(in VERTEX_INPUT In)
{
    return VSTerrain(true, In);
}

VERTEX_OUTPUT VSTerrain9_1(in VERTEX_INPUT In)
{
    return VSTerrain(false, In);
}

VERTEX_OUTPUT VSForest(in VERTEX_INPUT_FOREST In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 upVector = float3(0, -1, 0); // This constant is also defined in Shareds.cs

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position.xyz;
	newPosition += (In.TexCoords.x - 0.5f) * SideVector * In.Normal.x;
	newPosition += (In.TexCoords.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light = EyeVector;

	_VSLightsAndShadows(false, In.Position, Out);

	return Out;
}

VERTEX_OUTPUT VSSignalLight(in VERTEX_INPUT_SIGNAL In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSSignalProjection(false, In, Out);
	return Out;
}

VERTEX_OUTPUT VSSignalLightGlow(in VERTEX_INPUT_SIGNAL In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSSignalProjection(true, In, Out);
	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// Gets the ambient light effect.
float _PSGetAmbientEffect(in VERTEX_OUTPUT In)
{
	return In.Normal_Light.w * ZBias_Lighting.y;
}

// Gets the specular light effect.
float _PSGetSpecularEffect(in VERTEX_OUTPUT In)
{
	float3 halfVector = normalize(-In.RelPosition.xyz) + LightVector_ZFar.xyz;
	return In.Normal_Light.w * ZBias_Lighting.w * pow(saturate(dot(In.Normal_Light.xyz, normalize(halfVector))), ZBias_Lighting.z);
}

// Gets the shadow effect.
float3 _Level9_1GetShadowEffect(in VERTEX_OUTPUT In)
{
	return float3(tex2D(ShadowMap0, In.Shadow.xy).xy, In.Shadow.z);
}

float3 _Level9_3GetShadowEffect(in VERTEX_OUTPUT In)
{
	float depth = In.RelPosition.w;
	float3 rv = 0;
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

void _PSApplyShadowColor(inout float3 Color, in VERTEX_OUTPUT In)
{
	float depth = In.RelPosition.w;
	if (depth < ShadowMapLimit.x) {
		Color.rgb *= 0.9;
		Color.r += 0.1;
	} else {
		if (depth < ShadowMapLimit.y) {
			Color.rgb *= 0.9;
			Color.g += 0.1;
		} else {
			if (depth < ShadowMapLimit.z) {
				Color.rgb *= 0.9;
				Color.b += 0.1;
			} else {
				if (depth < ShadowMapLimit.w) {
					Color.rgb *= 0.9;
					Color.rg += 0.1;
				}
			}
		}
	}
}

float _PSGetShadowEffect(uniform bool ShaderModel3, uniform bool NormalLighting, in VERTEX_OUTPUT In)
{
	float3 moments;
	if (ShaderModel3)
		moments = _Level9_3GetShadowEffect(In);
	else
		moments = _Level9_1GetShadowEffect(In);

	bool not_shadowed = moments.z - moments.x < 0.00005;
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.00005, 1.0);
	float m_d = moments.z - moments.x;
	float p = pow(variance / (variance + m_d * m_d), 50);
	if (NormalLighting)
		return saturate(not_shadowed + p) * saturate(In.Normal_Light.w * 5 - 2);
	return saturate(not_shadowed + p);
}

// Gets the overcast color.
float3 _PSGetOvercastColor(in float4 Color, in VERTEX_OUTPUT In)
{
	// Value used to determine equivalent grayscale color.
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);

	float intensity = dot(Color.rgb, LumCoeff);
	return lerp(intensity, Color.rgb, 0.8) * 0.5;
}

// Applies the lighting effect of the train's headlights, including
// fade-in/fade-out animations.
void _PSApplyHeadlights(inout float3 Color, in float3 OriginalColor, in VERTEX_OUTPUT In)
{
	float3 headlightToSurface = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(headlightToSurface, HeadlightDirection.xyz);

	float shading = step(0, coneDot);
	shading *= step(0, dot(In.Normal_Light.xyz, -headlightToSurface));
	shading *= saturate(HeadlightDirection.w / (1 - coneDot));
	shading *= saturate(1 - length(In.LightDir_Fog.xyz) * HeadlightRcpDistance);
	shading *= HeadlightPosition.w;
	Color += OriginalColor * HeadlightColor.rgb * HeadlightColor.a * shading;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float3 Color, in VERTEX_OUTPUT In)
{
	Color = lerp(Color, Fog.rgb, In.LightDir_Fog.w);
}

void _PSSceneryFade(inout float4 Color, in VERTEX_OUTPUT In)
{
	if (ReferenceAlpha < 0.01) Color.a = 1;
	Color.a *= saturate((LightVector_ZFar.w - length(In.RelPosition.xyz)) / 50);
}

float4 PSImage(uniform bool ShaderModel3, uniform bool ClampTexCoords, in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	if (ShaderModel3 && ClampTexCoords) {
		// We need to clamp the rendering to within the [0..1] range only.
		if (saturate(In.TexCoords.x) != In.TexCoords.x || saturate(In.TexCoords.y) != In.TexCoords.y) {
			Color.a = 0;
		}
	}

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In) + ImageTextureIsNight));
	// Specular effect next.
	litColor += _PSGetSpecularEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In);
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	if (ShaderModel3) _PSSceneryFade(Color, In);
	//if (ShaderModel3) _PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSImage9_3(in VERTEX_OUTPUT In) : COLOR0
{
    return PSImage(true, false, In);
}

float4 PSImage9_3Clamp(in VERTEX_OUTPUT In) : COLOR0
{
    return PSImage(true, true, In);
}

float4 PSImage9_1(in VERTEX_OUTPUT In) : COLOR0
{
    return PSImage(false, false, In);
}

float4 PSVegetation(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
    // Alpha testing:
    clip(Color.a - ReferenceAlpha);
	// Ambient effect applies first; no shadow effect for vegetation; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * VegetationAmbientModifier;
	// No specular effect for vegetation.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain(uniform bool ShaderModel3, in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(ShaderModel3, true, In) + ImageTextureIsNight));
	// No specular effect for terrain.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Overlay image for terrain.
	litColor.rgb *= tex2D(Overlay, In.TexCoords.xy * OverlayScale).rgb * 2;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	//if (ShaderModel3) _PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain9_3(in VERTEX_OUTPUT In) : COLOR0
{
    return PSTerrain(true, In);
}

float4 PSTerrain9_1(in VERTEX_OUTPUT In) : COLOR0
{
    return PSTerrain(false, In);
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{
	const float ShadowBrightness = 0.5;

	float4 Color = tex2D(Image, In.TexCoords.xy);
    // Alpha testing:
    clip(Color.a - ReferenceAlpha);
	// Fixed ambient and shadow effects at darkest level.
	float3 litColor = Color.rgb * ShadowBrightness;
	// No specular effect for dark shade.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSHalfBright(in VERTEX_OUTPUT In) : COLOR0
{
	const float HalfShadowBrightness = 0.75;

	float4 Color = tex2D(Image, In.TexCoords.xy);
    // Alpha testing:
    clip(Color.a - ReferenceAlpha);
	// Fixed ambient and shadow effects at mid-dark level.
	float3 litColor = Color.rgb * HalfShadowBrightness;
	// No specular effect for half-bright.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.y);
	// Night-time darkens everything, except night-time textures.
	litColor *= HalfNightColorModifier;
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSFullBright(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
    // Alpha testing:
    clip(Color.a - ReferenceAlpha);
	// Fixed ambient and shadow effects at brightest level.
	float3 litColor = Color.rgb;
	// No specular effect for full-bright.
	// No overcast effect for full-bright.
	// No night-time effect for full-bright.
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color.rgb, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSSignalLight(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
    // Alpha testing:
    clip(Color.a - ReferenceAlpha);
	// No ambient and shadow effects for signal lights.
	// Apply signal coloring effect.
	float3 litColor = lerp(Color.rgb, In.Color.rgb, Color.r);
	// No specular effect, overcast effect, night-time darkening, headlights or fogging effect for signal lights.
	return float4(litColor, Color.a * SignalLightIntensity);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique ImageLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSGeneral9_1();
		PixelShader = compile ps_4_0_level_9_1 PSImage9_1();
	}
}

technique ImageLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSGeneral9_3();
		PixelShader = compile ps_4_0_level_9_3 PSImage9_3();
	}
}

technique TransferLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSTransfer9_1();
		PixelShader = compile ps_4_0_level_9_1 PSImage9_1();
	}
}

technique TransferLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSTransfer3();
		PixelShader = compile ps_4_0_level_9_3 PSImage9_3Clamp();
	}
}

technique Forest {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSForest();
		PixelShader = compile ps_4_0_level_9_1 PSVegetation();
	}
}

technique VegetationLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSGeneral9_1();
		PixelShader = compile ps_4_0_level_9_1 PSVegetation();
	}
}

technique VegetationLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSGeneral9_3();
		PixelShader = compile ps_4_0_level_9_3 PSVegetation();
	}
}

technique TerrainLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSTerrain9_1();
		PixelShader = compile ps_4_0_level_9_1 PSTerrain9_1();
	}
}

technique TerrainLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSTerrain9_3();
		PixelShader = compile ps_4_0_level_9_3 PSTerrain9_3();
	}
}

technique DarkShadeLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSGeneral9_1();
		PixelShader = compile ps_4_0_level_9_1 PSDarkShade();
	}
}

technique DarkShadeLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSGeneral9_3();
		PixelShader = compile ps_4_0_level_9_3 PSDarkShade();
	}
}

technique HalfBrightLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSGeneral9_1();
		PixelShader = compile ps_4_0_level_9_1 PSHalfBright();
	}
}

technique HalfBrightLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSGeneral9_3();
		PixelShader = compile ps_4_0_level_9_3 PSHalfBright();
	}
}

technique FullBrightLevel9_1 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSGeneral9_1();
		PixelShader = compile ps_4_0_level_9_1 PSFullBright();
	}
}

technique FullBrightLevel9_3 {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSGeneral9_3();
		PixelShader = compile ps_4_0_level_9_3 PSFullBright();
	}
}

technique SignalLight {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSSignalLight();
		PixelShader = compile ps_4_0_level_9_1 PSSignalLight();
	}
}

technique SignalLightGlow {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSSignalLightGlow();
		PixelShader = compile ps_4_0_level_9_1 PSSignalLight();
	}
}
