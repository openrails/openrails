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

float4x4 World;         // model -> world [max number of bones]
float4x4 WorldViewProjection;  // model -> world -> view -> projection (in case of skinned model only View x Projection)
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
texture  ImageTexture; // .s: linear RGBA, glTF (PBR): 8 bit sRGB + linear A
texture  OverlayTexture;
float	 OverlayScale;

float4x4 Bones[50]; // model -> world [max number of bones]
float4   BaseColorFactor; // glTF linear color multiplier
texture  NormalTexture; // linear RGB
float    NormalScale;
texture  EmissiveTexture; // 8 bit sRGB
float3   EmissiveFactor; // glTF linear emissive multiplier
texture  OcclusionTexture; // r = occlusion, can be combined with the MetallicRoughnessTexture
texture  MetallicRoughnessTexture; // g = roughness, b = metalness
float3   OcclusionFactor; // x = occlusion strength, y = roughness factor, z = metallic factor
float    HasNormalMap; // 0: doesn't have, 1: has normal map
float3   LightColor;
float4   TextureCoordinates; // x: baseColor, y: roughness-metallic, z: normal, w: emissive
float    TexturePacking; // 0: occlusionRoughnessMetallic (default), 1: roughnessMetallicOcclusion, 2: normalRoughnessMetallic (RG+B+A)

//static const float3 LightColor = float3(1.0, 1.0, 1.0);
static const float M_PI = 3.141592653589793;
static const float MinRoughness = 0.04;

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

sampler Normal = sampler_state
{
	Texture = (NormalTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler Emissive = sampler_state
{
	Texture = (EmissiveTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler Occlusion = sampler_state
{
	Texture = (OcclusionTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler MetallicRoughness = sampler_state
{
	Texture = (MetallicRoughnessTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

samplerCUBE EnvironmentMapSpecular = sampler_state
{
	Texture = (EnvironmentMapSpecularTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

samplerCUBE EnvironmentMapDiffuse = sampler_state
{
	Texture = (EnvironmentMapDiffuseTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

sampler BrdfLut = sampler_state
{
	Texture = (BrdfLutTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
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

struct VERTEX_INPUT_NORMALMAP
{
	float4 Position    : POSITION;
	float2 TexCoords   : TEXCOORD0;
	float3 Normal      : NORMAL;
	float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
};

struct VERTEX_INPUT_SKINNED
{
	float4 Position    : POSITION;
	float2 TexCoords   : TEXCOORD0;
	float3 Normal      : NORMAL;
    float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
	float4 Joints      : BLENDINDICES0;
	float4 Weights     : BLENDWEIGHT0;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float4 TexCoords    : TEXCOORD1; // tex coords x, y; metallic-roughness tex coords z, w
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 LightDir_Fog : TEXCOORD3; // light dir x, y, z; fog fade
	float4 Shadow       : TEXCOORD4; // Level9_1<shadow map texture and depth x, y, z> Level9_3<abs position x, y, z, w>
};

struct VERTEX_OUTPUT_PBR
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float4 TexCoords    : TEXCOORD1; // tex coords x, y; metallic-roughness tex coords z, w
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 LightDir_Fog : TEXCOORD3; // light dir x, y, z; fog fade
	float4 Shadow       : TEXCOORD4; // Level9_1<shadow map texture and depth x, y, z> Level9_3<abs position x, y, z, w>
    float3 Tangent      : TEXCOORD5; // normal map tangents
    float3 Bitangent    : TEXCOORD6; // normal map bitangents
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in float3 InNormal, in float4x4 WorldTransform, inout float4 OutPosition, inout float4 OutRelPosition, inout float4 OutNormal_Light)
{
	OutRelPosition.xyz = mul(OutPosition, WorldTransform).xyz - ViewerPos;
	OutPosition = mul(OutPosition, WorldViewProjection);
	OutRelPosition.w = OutPosition.z;
	OutNormal_Light.xyz = normalize(mul(InNormal, (float3x3)WorldTransform).xyz);
	
	// Normal lighting (range 0.0 - 1.0)
	// Need to calc. here instead of _VSLightsAndShadows() to avoid calling it from VSForest(), where it has gone into pre-shader in Shaders.cs
	OutNormal_Light.w = dot(OutNormal_Light.xyz, LightVector_ZFar.xyz) * 0.5 + 0.5;
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

void _VSLightsAndShadows(uniform bool ShaderModel3, in float4 InPosition, in float4x4 WorldTransform, in float distance, inout float4 lightDir_Fog, inout float4 shadow)
{
	// Headlight lighting
	lightDir_Fog.xyz = mul(InPosition, WorldTransform).xyz - HeadlightPosition.xyz;

	// Fog fading
	lightDir_Fog.w = (2.0 / (1.0 + exp(distance * Fog.a * -2.0))) - 1.0;

	// Absolute position for shadow mapping
	if (ShaderModel3) {
		shadow = mul(InPosition, WorldTransform);
	} else {
		shadow.xyz = mul(mul(InPosition, WorldTransform), LightViewProjectionShadowProjection0).xyz;
	}
}

float4x4 _VSSkinTransform(in float4 Joints, in float4 Weights)
{
	float4x4 skinTransform = 0;

	skinTransform += Bones[Joints.x] * Weights.x;
	skinTransform += Bones[Joints.y] * Weights.y;
	skinTransform += Bones[Joints.z] * Weights.z;
	skinTransform += Bones[Joints.w] * Weights.w;
    
    return skinTransform;
}

void _VSInstances(inout float4 Position, inout float3 Normal, in float4x4 Instance)
{
    if (determinant(Instance) != 0) {
        Position = mul(Position, transpose(Instance));
        Normal = mul(Normal, (float3x3)transpose(Instance));
    }
}

void _VSNormalMapTransform(in float4 Tangent, in float3 Normal, float4x4 WorldTransform, inout VERTEX_OUTPUT_PBR Out)
{
    Out.Tangent = mul(normalize(Tangent.xyz), (float3x3)WorldTransform).xyz;
    // Note: to be called after the normal map projection, so the Out.Normal_Light.xyz is already available:
    Out.Bitangent = cross(Out.Normal_Light.xyz, Out.Tangent) * Tangent.w;
}

VERTEX_OUTPUT VSGeneral(uniform bool ShaderModel3, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	
	if (ShaderModel3) _VSInstances(In.Position, In.Normal, In.Instance);

    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(ShaderModel3, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	//Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.TexCoords.xy = In.TexCoords;


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

VERTEX_OUTPUT_PBR VSPbrBaseColorMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR) 0;

	_VSInstances(In.Position, In.Normal, In.Instance);

    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(true, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.TexCoords.xy = In.TexCoords;

	Out.Color = float4(1, 1, 1, 1);
	Out.Tangent = float3(-2, 0, 0);
	Out.Bitangent = float3(0, 1, 0);

	return Out;
}

VERTEX_OUTPUT_PBR VSNormalMap(in VERTEX_INPUT_NORMALMAP In)
{
	VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR)0;

	_VSInstances(In.Position, In.Normal, In.Instance);
    
    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(true, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

	_VSNormalMapTransform(In.Tangent, In.Normal, World, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.Color = In.Color;
	Out.TexCoords.xy = In.TexCoords;
	Out.TexCoords.zw = In.TexCoordsPbr;

	return Out;
}

VERTEX_OUTPUT_PBR VSSkinned(in VERTEX_INPUT_SKINNED In)
{
	VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR) 0;

	_VSInstances(In.Position, In.Normal, In.Instance);
	float4x4 WorldTransform = _VSSkinTransform(In.Joints, In.Weights);
    
    // Beware: Out.Position will contain Pos*World, and WorldViewProjection is uploaded as View*Projection here,
    // in contrast with e.g. VSGeneral, where Out.Position is just a position, and WorldViewProjection is WVP.
	Out.Position = mul(In.Position, WorldTransform);
	_VSNormalProjection(In.Normal, WorldTransform, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(true, In.Position, WorldTransform, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

	_VSNormalMapTransform(In.Tangent, In.Normal, WorldTransform, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.Color = In.Color;
	Out.TexCoords.xy = In.TexCoords;
	Out.TexCoords.zw = In.TexCoordsPbr;

	return Out;
}

VERTEX_OUTPUT VSTransfer(uniform bool ShaderModel3, in VERTEX_INPUT_TRANSFER In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSTransferProjection(In, Out);
	_VSLightsAndShadows(ShaderModel3, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

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
    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(ShaderModel3, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);
	Out.TexCoords.xy = In.TexCoords;
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

	_VSLightsAndShadows(false, In.Position, World, length(Out.Position.xyz), Out.LightDir_Fog, Out.Shadow);

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

float3 _PSGetNormal(in VERTEX_OUTPUT_PBR In, bool hasNormals, bool hasTangents)
{
	float3x3 tbn = float3x3(In.Tangent, In.Bitangent, In.Normal_Light.xyz);
    if (!hasTangents)
	{
        float3 pos_dx = ddx(In.Position.xyz);
        float3 pos_dy = ddy(In.Position.xyz);
        float3 tex_dx = ddx(float3(In.TexCoords.xy, 0.0));
        float3 tex_dy = ddy(float3(In.TexCoords.xy, 0.0));
        float3 t = (tex_dy.y * pos_dx - tex_dx.y * pos_dy) / (tex_dx.x * tex_dy.y - tex_dy.x * tex_dx.y);

		float3 ng;
        if (hasNormals)
            ng = normalize(In.Normal_Light.xyz);
        else
            ng = cross(pos_dx, pos_dy);

        t = normalize(t - ng * dot(ng, t));
        float3 b = normalize(cross(ng, t));
        row_major float3x3 tbn = float3x3(t, b, ng);
    }
	float3 n;
	if (NormalScale > 0)
	{
		if (TexturePacking == 2)
		{
			// Probably this is specific to the BC5 normal maps, which is not supported in MonoGame anyway...
			float2 normalXY;
			if (TextureCoordinates.z == 0)
				normalXY = tex2D(Normal, In.TexCoords.xy).rg;
			else
				normalXY = tex2D(Normal, In.TexCoords.zw).rg;
			normalXY = float2(2.0, 2.0) * normalXY - float2(1.0, 1.0);
			float normalZ = sqrt(saturate(1.0 - dot(normalXY, normalXY)));
			n = float3(normalXY.xy, normalZ);
		}
		else
		{
			if (TextureCoordinates.z == 0)
				n = tex2D(Normal, In.TexCoords.xy).rgb;
			else
				n = tex2D(Normal, In.TexCoords.zw).rgb;
			n = (2.0 * n - 1.0);
		}
		n = normalize(mul((n * float3(NormalScale, NormalScale, 1.0)), tbn));
	}
	else
	    n = tbn[2].xyz;

    return n;
}

float3 _PSGetIBLContribution(float3 diffuseColor, float3 specularColor, float NdotV, float perceptualRoughness, float3 n, float3 reflection)
{
	float2 val = float2(NdotV, 1.0 - perceptualRoughness);
	float3 brdf = tex2D(BrdfLut, val).rgb;
	brdf.rgb = pow(brdf.rgb, 2.2);

	float3 specularLight = texCUBE(EnvironmentMapSpecular, reflection).rgb;
	specularLight.rgb = pow(specularLight.rgb, 2.2);
	float3 specular = specularLight * (specularColor * brdf.x + brdf.y);

	float3 diffuseLight = texCUBE(EnvironmentMapDiffuse, n).rgb; // irradiance (washed out)
	diffuseLight.rgb = pow(diffuseLight.rgb, 2.2);
	float3 diffuse = diffuseLight * diffuseColor;

	return diffuse + specular;
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

float4 PSPbr(in VERTEX_OUTPUT_PBR In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;

	bool ShaderModel3 = true;
	
	// This is for being able to call the original functions for the ambient lighting:
	VERTEX_OUTPUT InGeneral = (VERTEX_OUTPUT)0;
	InGeneral.Position = In.Position;
	InGeneral.RelPosition = In.RelPosition;
	InGeneral.LightDir_Fog = In.LightDir_Fog;
	InGeneral.TexCoords = In.TexCoords;
	InGeneral.Shadow = In.Shadow;
	InGeneral.Color = In.Color;
	InGeneral.Normal_Light = In.Normal_Light;

	float4 Color;
	if (TextureCoordinates.x == 0)
		Color = tex2D(Image, In.TexCoords.xy);
	else
		Color = tex2D(Image, In.TexCoords.zw);

	// Decode from sRGB to linear.
	Color.rgb = pow(Color.rgb, 2.2);
	// Apply the linear multipliers.
	Color *= In.Color * BaseColorFactor;
	// Alpha testing:
	clip(Color.a - ReferenceAlpha);
	
	///////////////////////
	// Contributions from the OpenRails environment:
	float shadowFactor = _PSGetShadowEffect(true, true, InGeneral);
	float diffuseShadowFactor = lerp(ShadowBrightness, FullBrightness, saturate(shadowFactor));
	float3 headlightContrib = 0;
	_PSApplyHeadlights(headlightContrib, Color.rgb, InGeneral); // inout: headlightContrib
	_PSSceneryFade(Color, InGeneral);
	float fade = Color.a;
	///////////////////////
	
	// Metallic-roughness
	float occlusion = 1;
	float metallic = 1;
	float roughness = 1;
	if (TexturePacking == 0 || TexturePacking == 1)
	{
		float3 orm;
		if (TextureCoordinates.y == 0)
			orm = tex2D(MetallicRoughness, In.TexCoords.xy).rgb;
		else
			orm = tex2D(MetallicRoughness, In.TexCoords.zw).rgb;
		if (TexturePacking == 0)
		{
			occlusion = orm.r;
			roughness = orm.g;
			metallic = orm.b;
		}
		else
		{
			roughness = orm.r;
			metallic = orm.g;
			occlusion = orm.b;
		}
	}
	else if (TexturePacking == 2)
	{
		float4 nrm;
		if (TextureCoordinates.y == 0)
			nrm = tex2D(MetallicRoughness, In.TexCoords.xy);
		else
			nrm = tex2D(MetallicRoughness, In.TexCoords.zw);
		roughness = nrm.b;
		metallic = nrm.a;

		if (TextureCoordinates.z == 0)
			occlusion = tex2D(Occlusion, In.TexCoords.xy).r;
		else
			occlusion = tex2D(Occlusion, In.TexCoords.zw).r;
	}

	float perceptualRoughness = clamp(roughness * OcclusionFactor.y, MinRoughness, 1.0);
	metallic = clamp(metallic * OcclusionFactor.z, 0.0, 1.0);
	
	float3 f0 = float3(0.04, 0.04, 0.04);
	float3 diffuseColor = Color.rgb * (float3(1.0, 1.0, 1.0) - f0);
	diffuseColor *= 1.0 - metallic;
	
	float3 specularColor = lerp(f0, Color.rgb, metallic);
	float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);
	float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    float3 specularEnvironmentR0 = specularColor.rgb;
    float3 specularEnvironmentR90 = float3(1.0, 1.0, 1.0) * reflectance90;
	
	float3 n = _PSGetNormal(In, true, true);
	float3 v = normalize(-In.RelPosition.xyz);
	float3 l = normalize(LightVector_ZFar.xyz);
	float3 h = normalize(l + v);
	float3 reflection = -normalize(reflect(v, n));

	float NdotL = clamp(dot(n, l), 0.001, 1.0);
    float NdotV = abs(dot(n, v)) + 0.001;
    float NdotH = clamp(dot(n, h), 0.0, 1.0);
    float LdotH = clamp(dot(l, h), 0.0, 1.0);
    float VdotH = clamp(dot(v, h), 0.0, 1.0);
	
	float3 F = specularEnvironmentR0 + (specularEnvironmentR90 - specularEnvironmentR0) * pow(clamp(1.0 - VdotH, 0.0, 1.0), 5.0);
	
	float alphaRoughness = perceptualRoughness * perceptualRoughness;
	float roughnessSq = alphaRoughness * alphaRoughness;
	float attenuationL = 2.0 * NdotL / (NdotL + sqrt(roughnessSq + (1.0 - roughnessSq) * (NdotL * NdotL)));
    float attenuationV = 2.0 * NdotV / (NdotV + sqrt(roughnessSq + (1.0 - roughnessSq) * (NdotV * NdotV)));
    float G = attenuationL * attenuationV;
	
    float f = (NdotH * roughnessSq - NdotH) * NdotH + 1.0;
	float D = roughnessSq / (M_PI * f * f);
	
	float3 diffuseContrib = (1.0 - F) * diffuseColor / M_PI * diffuseShadowFactor;
	float3 specContrib = F * G * D / (4.0 * NdotL * NdotV) * shadowFactor;
    float3 litColor = NdotL * LightColor * (diffuseContrib + specContrib + headlightContrib);

	litColor += _PSGetIBLContribution(diffuseColor, specularColor, NdotV, perceptualRoughness, n, reflection);

	// Occlusion:
	litColor = lerp(litColor, litColor * occlusion, OcclusionFactor.x);
	
	// Emissive color:
	float3 emissive;
	if (TextureCoordinates.w == 0)
		emissive = tex2D(Emissive, In.TexCoords.xy).rgb;
	else
		emissive = tex2D(Emissive, In.TexCoords.zw).rgb;
	litColor += pow(emissive, 2.2) * EmissiveFactor;

	///////////////////////
	// Contributions from the OpenRails environment:
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, InGeneral), Overcast.x);
	// And fogging is last.
	_PSApplyFog(litColor, InGeneral);
	//_PSApplyShadowColor(litColor, InGeneral); // a debug function only
	///////////////////////

	// Transform back to sRGB:
	litColor = pow(litColor, 1.0 / 2.2);

	return float4(litColor, fade);
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

technique PbrBaseColorMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSPbrBaseColorMap();
		PixelShader = compile ps_4_0 PSPbr();
	}
}

technique PbrNormalMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSNormalMap();
		PixelShader = compile ps_4_0 PSPbr();
	}
}

technique PbrSkinned {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSSkinned();
		PixelShader = compile ps_4_0 PSPbr();
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
