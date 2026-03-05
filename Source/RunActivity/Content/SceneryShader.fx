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
float4x4 View;          // world -> view
float4x4 Projection;    // view -> projection
float4x4 LightViewProjectionShadowProjection0;  // world -> light view -> light projection -> shadow map projection
float4x4 LightViewProjectionShadowProjection1;
float4x4 LightViewProjectionShadowProjection2;
float4x4 LightViewProjectionShadowProjection3;
texture  ShadowMapTexture0;
texture  ShadowMapTexture1;
texture  ShadowMapTexture2;
texture  ShadowMapTexture3;
float4   ShadowMapLimit;
float4   ZBias_Lighting;  // x = z-bias, y = diffuse (not unlit), z = specular, w = step(1, z)
float4   Fog;  // rgb = color of fog; a = reciprocal of distance from camera, everything is
			   // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.
float    ZFar;
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

texture  BonesTexture; // 4 channels of 32 bit float, containing the 4x4 matrix palette for skinned models
float    BonesCount;

// Keep these in sync with the values defined in RenderProcess.cs
#define MAX_LIGHTS 20 // must not be less than 2
#define CLEARCOAT
#define MAX_MORPH_TARGETS 8

float4   BaseColorFactor; // glTF linear color multiplier
texture  NormalTexture; // linear RGB
float    NormalScale;
texture  EmissiveTexture; // 8 bit sRGB
float3   EmissiveFactor; // glTF linear emissive multiplier
texture  OcclusionTexture; // r = occlusion, can be combined with the MetallicRoughnessTexture
texture  MetallicRoughnessTexture; // g = roughness, b = metalness
texture  ClearcoatTexture;
float    ClearcoatFactor;
texture  ClearcoatRoughnessTexture;
float    ClearcoatRoughnessFactor;
texture  ClearcoatNormalTexture;
float    ClearcoatNormalScale;
float3   OcclusionFactor; // x = occlusion strength, y = roughness factor, z = metallic factor
float4   TextureCoordinates1; // x: baseColor, y: roughness-metallic, z: normal, w: emissive
float4   TextureCoordinates2; // x: clearcoat, y: clearcoat-roughness, z: clearcoat-normal, w: occlusion
float    TexturePacking; // 0: occlusion (R) and roughnessMetallic (GB) separate, 1: roughnessMetallicOcclusion, 2: normalRoughnessMetallic (RG+B+A), 3: occlusionRoughnessMetallic, 4: roughnessMetallicOcclusion + normal (RG) 2 channel separate, 5: occlusionRoughnessMetallic + normal (RG) 2 channel separate
bool     HasNormals; // 0: no, 1: yes
bool     HasTangents; // true: tangents were pre-calculated, false: tangents must be calculated in the pixel shader
int      MorphConfig[9]; // 0-5: position of POSITION, NORMAL, TANGENT, TEXCOORD_0, TEXCOORD_1, COLOR_0 data within MorphTargets, respectively. 6: if the model has skin, set to 1. All: set to -1 if not available. 7: targets count. 8: attributes count.
float    MorphWeights[MAX_MORPH_TARGETS]; // the actual morphing animation state

int		NumLights; // The number of the lights used
float	LightTypes[MAX_LIGHTS]; // 0: directional, 1: point, 2: spot, 3: headlight
float3	LightPositions[MAX_LIGHTS];
float3	LightDirections[MAX_LIGHTS];
float3	LightColorIntensities[MAX_LIGHTS]; // pre-multiplied by intensity
float	LightRangesRcp[MAX_LIGHTS];
float	LightInnerConeCos[MAX_LIGHTS];
float	LightOuterConeCos[MAX_LIGHTS];

static const float M_PI = 3.141592653589793;
static const float RECIPROCAL_PI = 0.31830988618;
static const float RECIPROCAL_PI2 = 0.15915494;
static const float MinRoughness = 0.04;

static const int LightType_Directional = 0;
static const int LightType_Point = 1;
static const int LightType_Spot = 2;
static const int LightType_Headlight = 3; // Pre-PBR linear attenuated headlight.

static const float FullBrightness = 1.0;
static const float ShadowBrightness = 0.5;

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

sampler Clearcoat = sampler_state
{
	Texture = (ClearcoatTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler ClearcoatRoughness = sampler_state
{
	Texture = (ClearcoatRoughnessTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler ClearcoatNormal = sampler_state
{
	Texture = (ClearcoatNormalTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
};

sampler EnvironmentMapSpecular = sampler_state
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
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};

sampler BoneSampler = sampler_state
{
    Texture = (BonesTexture);
    MagFilter = Point; // No interpolation between bone matrices, otherwise the skinning will be wrong.
    MinFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
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
    min16uint4  Joints : BLENDINDICES0;
	float4 Weights     : BLENDWEIGHT0;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
};

struct VERTEX_INPUT_MORPHED
{
	float4 Position    : POSITION;
	float2 TexCoords   : TEXCOORD0;
	float3 Normal      : NORMAL;
    float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
    min16uint4  Joints : BLENDINDICES0;
	float4 Weights     : BLENDWEIGHT0;
	float4 Color       : COLOR0;
    float4 MorphTargets[MAX_MORPH_TARGETS] : POSITION1;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float4 TexCoords    : TEXCOORD1; // tex coords x, y; metallic-roughness tex coords z, w
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 Shadow       : TEXCOORD3; // Level9_1<shadow map texture and depth x, y, z> Level9_3<abs position x, y, z, w>
	float  Fog          : TEXCOORD4; // fog fade
};

struct VERTEX_OUTPUT_PBR
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float4 TexCoords    : TEXCOORD1; // tex coords x, y; metallic-roughness tex coords z, w
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 Shadow       : TEXCOORD3; // Level9_1<shadow map texture and depth x, y, z> Level9_3<abs position x, y, z, w>
    float4 Tangent      : TEXCOORD4; // normal map tangents xyz, fog w
    float3 Bitangent    : TEXCOORD5; // normal map bitangents
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in float3 InNormal, in float4x4 WorldTransform, inout float4 OutPosition, inout float4 OutRelPosition, inout float4 OutNormal_Light)
{
	OutRelPosition.xyz = mul(OutPosition, WorldTransform).xyz - ViewerPos;
	OutPosition = mul(mul(mul(OutPosition, WorldTransform), View), Projection);
	OutRelPosition.w = OutPosition.z;
	OutNormal_Light.xyz = normalize(mul(InNormal, (float3x3)WorldTransform).xyz);
	
	// Normal lighting (range 0.0 - 1.0)
	// For VSForest() it is calculated in Shaders.cs eyeVector.SetValue(), the sun direction is uploaded to this shader negated, to conform with glTF lights extension
	OutNormal_Light.w = dot(OutNormal_Light.xyz, -LightDirections[0]) * 0.5 + 0.5; // [0] is always the sun/moon
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
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = relPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Color = In.Color;
}

void _VSTransferProjection(in VERTEX_INPUT_TRANSFER In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.w = 1;
}

void _VSLightsAndShadows(in float4 InPosition, in float4x4 WorldTransform, in float distance, inout float fog, inout float4 shadow)
{
	// Fog fading
	fog = (2.0 / (1.0 + exp(distance * Fog.a * -2.0))) - 1.0;

	// Absolute position for shadow mapping
	shadow = mul(InPosition, WorldTransform);
}

float4x4 GetBoneMatrix(min16uint index)
{
    float v = (index + 0.5) / BonesCount;

    float4 row1 = tex2Dlod(BoneSampler, float4(0.125, v, 0, 0)); // 0.5 / 4
    float4 row2 = tex2Dlod(BoneSampler, float4(0.375, v, 0, 0)); // 1.5 / 4
    float4 row3 = tex2Dlod(BoneSampler, float4(0.625, v, 0, 0)); // 2.5 / 4
    float4 row4 = tex2Dlod(BoneSampler, float4(0.875, v, 0, 0)); // 3.5 / 4

    return float4x4(row1, row2, row3, row4);
}

float4x4 _VSSkinTransform(in min16uint4 Joints, in float4 Weights)
{
	float4x4 skinTransform = 0;

    skinTransform += GetBoneMatrix(Joints.x) * (float)Weights.x;
    skinTransform += GetBoneMatrix(Joints.y) * (float)Weights.y;
    skinTransform += GetBoneMatrix(Joints.z) * (float)Weights.z;
    skinTransform += GetBoneMatrix(Joints.w) * (float)Weights.w;

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
    Out.Tangent.xyz = mul(normalize(Tangent.xyz), (float3x3)WorldTransform).xyz;
    // Note: to be called after the normal map projection, so the Out.Normal_Light.xyz is already available:
    Out.Bitangent.xyz = cross(Out.Normal_Light.xyz, Out.Tangent.xyz) * Tangent.w;
}

VERTEX_OUTPUT VSGeneral(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	
	_VSInstances(In.Position, In.Normal, In.Instance);

    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Fog, Out.Shadow);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.TexCoords.xy = In.TexCoords;


	return Out;
}

VERTEX_OUTPUT_PBR VSPbrBaseColorMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR) 0;

	_VSInstances(In.Position, In.Normal, In.Instance);

    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Tangent.w, Out.Shadow);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.TexCoords.xy = In.TexCoords;

	Out.Color = float4(1, 1, 1, 1);
	Out.Tangent.xyz = float3(-2, 0, 0);
	Out.Bitangent.xyz = float3(0, 1, 0);

	return Out;
}

VERTEX_OUTPUT_PBR VSNormalMap(in VERTEX_INPUT_NORMALMAP In)
{
	VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR)0;

	_VSInstances(In.Position, In.Normal, In.Instance);
    
    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Tangent.w, Out.Shadow);

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
	float4x4 worldTransform = _VSSkinTransform(In.Joints, In.Weights);
    
    Out.Position = In.Position;
    _VSNormalProjection(In.Normal, worldTransform, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(Out.Position, worldTransform, length(Out.Position.xyz), Out.Tangent.w, Out.Shadow);

	_VSNormalMapTransform(In.Tangent, In.Normal, worldTransform, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	Out.Color = In.Color;
	Out.TexCoords.xy = In.TexCoords;
	Out.TexCoords.zw = In.TexCoordsPbr;

	return Out;
}

VERTEX_OUTPUT_PBR VSMorphing(in VERTEX_INPUT_MORPHED In)
{
    VERTEX_OUTPUT_PBR Out = (VERTEX_OUTPUT_PBR)0;

    float4x4 worldTransform = MorphConfig[6] == 1 ? _VSSkinTransform(In.Joints, In.Weights) : World;

    Out.Position = In.Position;
    float3 normal = In.Normal;
    float4 tangent = In.Tangent;
    Out.Color = In.Color;
    Out.TexCoords.xy = In.TexCoords;
    Out.TexCoords.zw = In.TexCoordsPbr;

    [unroll(MAX_MORPH_TARGETS)]
    for (int i = 0; i < MorphConfig[7]; i++)
    {
        if (MorphConfig[0] != -1)
            Out.Position.xyz += In.MorphTargets[MorphConfig[8] * i + MorphConfig[0]].xyz * MorphWeights[i];
        if (MorphConfig[1] != -1)
            normal.xyz += In.MorphTargets[MorphConfig[8] * i + MorphConfig[1]].xyz * MorphWeights[i];
        if (MorphConfig[2] != -1)
            tangent.xyz += In.MorphTargets[MorphConfig[8] * i + MorphConfig[2]].xyz * MorphWeights[i];
        if (MorphConfig[3] != -1)
            Out.TexCoords.xy += In.MorphTargets[MorphConfig[8] * i + MorphConfig[3]].xy * MorphWeights[i];
        if (MorphConfig[4] != -1)
            Out.TexCoords.zw += In.MorphTargets[MorphConfig[8] * i + MorphConfig[4]].xy * MorphWeights[i];
        if (MorphConfig[5] != -1)
            Out.Color += In.MorphTargets[MorphConfig[8] * i + MorphConfig[5]] * MorphWeights[i];
    }

    _VSNormalProjection(normal, worldTransform, Out.Position, Out.RelPosition, Out.Normal_Light);
    _VSLightsAndShadows(Out.Position, worldTransform, length(Out.Position.xyz), Out.Tangent.w, Out.Shadow);

    _VSNormalMapTransform(tangent, normal, worldTransform, Out);

    // Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
    Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

    return Out;
}


VERTEX_OUTPUT VSTransfer(in VERTEX_INPUT_TRANSFER In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSTransferProjection(In, Out);
	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Fog, Out.Shadow);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTerrain(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
    Out.Position = In.Position;
	_VSNormalProjection(In.Normal, World, Out.Position, Out.RelPosition, Out.Normal_Light);
	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Fog, Out.Shadow);
	Out.TexCoords.xy = In.TexCoords;
	return Out;
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
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light = EyeVector;

	_VSLightsAndShadows(In.Position, World, length(Out.Position.xyz), Out.Fog, Out.Shadow);

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

float pow5(float x)
{
    // x^5 = x * x^2 * x^2
    float x2 = x * x;
    float x4 = x2 * x2;
    return x4 * x;
}

float pow50(float x)
{
    // x^50 = x^(32+16+2) = x32 * x16 * x2
    float x2 = x * x;
    float x4 = x2 * x2;
    float x8 = x4 * x4;
    float x16 = x8 * x8;
    float x32 = x16 * x16;
    return x32 * x16 * x2;
}

// Gets the ambient light effect.
float _PSGetAmbientEffect(in VERTEX_OUTPUT In)
{
	return In.Normal_Light.w * ZBias_Lighting.y;
}

float3 _PSGetShadowEffectMoments(in VERTEX_OUTPUT In)
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

float _PSGetShadowEffect(uniform bool NormalLighting, in VERTEX_OUTPUT In)
{
	float3 moments = _PSGetShadowEffectMoments(In);
	bool not_shadowed = moments.z - moments.x < 0.00005;
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.00005, 1.0);
	float m_d = moments.z - moments.x;
	float p = pow50(variance / (variance + m_d * m_d));
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

float3 _PSApplyMstsLights(in float3 diffuseColor, in VERTEX_OUTPUT In, float shadowFactor)
{
	float diffuseShadowFactor = lerp(ShadowBrightness, FullBrightness, saturate(shadowFactor));
	
	float3 n = In.Normal_Light.xyz;
	float3 v = normalize(-In.RelPosition.xyz);

	float3 diffuseContrib = (float3)0;
	float3 specContrib = (float3)0;
	float attenuation = 1;

	//[fastopt]
	[unroll(MAX_LIGHTS)]
	for (int i = 0; i < NumLights; i++)
	{
        float3 l;
        if (LightTypes[i] == LightType_Directional)
        {
            l = normalize(-LightDirections[i]); // normalize(pointToLight)
            attenuation = 1;
        }
        else
        {
            float3 pointToLight = LightPositions[i] - In.Shadow.xyz; // In.Shadow.xyz is the absolute world position of the point
            float pointLightDistance = length(pointToLight);
            l = pointToLight / pointLightDistance; // normalize(pointToLight)
            attenuation = 1;
            if (LightTypes[i] == LightType_Headlight)
            {
                attenuation *= clamp(1 - pointLightDistance * LightRangesRcp[i], 0, 1); // The pre-PBR headlight used linear range attenuation.
            }
            else
            {
                attenuation /= pow(pointLightDistance, 2); // The realistic range attenuation is inverse-squared.
                attenuation *= clamp(1 - pow(pointLightDistance * LightRangesRcp[i], 4), 0, 1);
            }

            if (LightTypes[i] == LightType_Spot || LightTypes[i] == LightType_Headlight)
                attenuation *= smoothstep(LightOuterConeCos[i], LightInnerConeCos[i], dot(LightDirections[i], -l));
        }

		float3 h = normalize(l + v);

		float NdotH = clamp(dot(n, h), 0.0, 1.0);
		float NdotL = 1;
		if (LightTypes[i] != LightType_Headlight)
			NdotL = clamp(dot(n, l), 0.001, 1.0); // Non-headlight lights use realistic lighting.
		else
			NdotL = step(0, dot(n, l)); // Pre-PBR headlight used full lit pixels within the headlights range everywhere.
 
        float3 intensity = LightColorIntensities[i] * attenuation;

		diffuseContrib += intensity * NdotL * diffuseColor / M_PI * diffuseShadowFactor;
		specContrib += intensity * NdotL * ZBias_Lighting.w * pow(NdotH, ZBias_Lighting.z) * shadowFactor;
        
        // Light 0 is the sun, the shadow factors are needed only for that one. For other lights they do not apply, they no longer needed.
        shadowFactor = 1;
        diffuseShadowFactor = 1;
    }
	return diffuseContrib + specContrib;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float3 Color, in VERTEX_OUTPUT In)
{
	Color = lerp(Color, Fog.rgb, In.Fog);
}

void _PSSceneryFade(inout float4 Color, in VERTEX_OUTPUT In)
{
	if (ReferenceAlpha < 0.01) Color.a = 1;
	Color.a *= saturate((ZFar - length(In.RelPosition.xyz)) / 50);
}

float4 _PSTex2D(sampler s, float4 inTexCoords, float texCoordsSelector)
{
	if (texCoordsSelector == 0)
		return tex2D(s, inTexCoords.xy);
	else
		return tex2D(s, inTexCoords.zw);
}

float3 _PSGetNormal(in VERTEX_OUTPUT_PBR In, bool hasTangents, float normalScale, sampler normalSampler, float texCoordsSelector, bool isFrontFace)
{
	bool hasNormalMap = -1 <= normalScale && normalScale <= 1;
	float3x3 tbn = float3x3(In.Tangent.xyz, In.Bitangent.xyz, In.Normal_Light.xyz);
    if (!hasTangents || !HasNormals)
	{
		float3 ng;
		if (HasNormals)
			ng = normalize(In.Normal_Light.xyz);
		else
			ng = normalize(cross(ddx(In.RelPosition.xyz), ddy(-In.RelPosition.xyz)));
		tbn[2].xyz = ng;
		
		if (hasNormalMap)
		{
            float3 pos_dx = ddx(In.Position.xyz);
            float3 pos_dy = ddy(In.Position.xyz);
            float3 tex_dx = -ddx(float3(In.TexCoords.xy, 0.0));
			float3 tex_dy = -ddy(float3(In.TexCoords.xy, 0.0));
            float tex_dxy = tex_dx.x * tex_dy.y - tex_dy.x * tex_dx.y;
            float3 t = (tex_dy.y * pos_dx - tex_dx.y * pos_dy) / tex_dxy;

			if (hasTangents)
				t = In.Tangent.xyz;
			else
				t = normalize(t - ng * dot(ng, t));
			tbn[0].xyz = t;
			tbn[1].xyz = sign(tex_dxy) * normalize(cross(ng, t));
		}
    }
	if (isFrontFace) // Does it work reversed?! We should negate in case of back face...
	{
		tbn[0] *= -1;
		tbn[1] *= -1;
		tbn[2] *= -1;
	}
	float3 n;
	if (hasNormalMap)
	{
		if (TexturePacking == 2 || TexturePacking == 4 || TexturePacking == 5)
		{
			// Probably this is specific to the BC5 normal maps, which is not supported in MonoGame anyway...
			float2 normalXY = _PSTex2D(normalSampler, In.TexCoords, texCoordsSelector).rg;
			normalXY = float2(2.0, 2.0) * normalXY - float2(1.0, 1.0);
			float normalZ = sqrt(saturate(1.0 - dot(normalXY, normalXY)));
			n = float3(normalXY.xy, normalZ);
		}
		else
		{
			n = _PSTex2D(normalSampler, In.TexCoords, texCoordsSelector).rgb;
			n = 2.0 * n - 1.0;
		}
		n = normalize(mul((n * float3(normalScale, normalScale, 1.0)), tbn));
	}
	else
	{
		n = tbn[2].xyz * sign(normalScale);
	}

    return n;
}

float2 _PSCartesianToPolar(float3 n)
{
	float2 uv;
	uv.x = atan2(n.z, n.x) * RECIPROCAL_PI2 + 0.5;
	uv.y = -asin(n.y) * RECIPROCAL_PI + 0.5;
	return uv;
}

float3 _PSSrgbToLinear(float3 color)
{
	return pow(color.rgb, 2.2);
}

float3 _PSLinearToSrgb(float3 color)
{
	return pow(color.rgb, 1.0 / 2.2);
}

float3 _PSRgbdToLinear(float4 value)
{
	return value.xyz / value.w;
}

float3 _PSGetIBLSpecular(float3 specularColor, float NdotV, float perceptualRoughness, float3 reflection)
{
	float2 val = float2(NdotV, 1.0 - perceptualRoughness);
	float3 brdf = tex2D(BrdfLut, val).rgb;
	brdf.rgb = _PSSrgbToLinear(brdf.rgb);

	float3 specularLight = _PSRgbdToLinear(tex2D(EnvironmentMapSpecular, _PSCartesianToPolar(reflection))).rgb;
	specularLight.rgb = _PSSrgbToLinear(specularLight.rgb);
	return specularLight * (specularColor * brdf.x + brdf.y);
}

float3 _PSGetIBLDiffuse(float3 diffuseColor, float3 n)
{
	float3 diffuseLight = texCUBE(EnvironmentMapDiffuse, n).rgb; // irradiance (washed out)
	//diffuseLight.rgb = _PSSrgbToLinear(diffuseLight.rgb); // If we can upload the image with sRGB texture surfaceformat, no need to convert manually, the GPU will do that for us.
	return diffuseLight * diffuseColor;
}

float4 PSImage(uniform bool ClampTexCoords, in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);

	// Do we still need this?
	if (ClampTexCoords) {
		// We need to clamp the rendering to within the [0..1] range only.
		if (saturate(In.TexCoords.x) != In.TexCoords.x || saturate(In.TexCoords.y) != In.TexCoords.y) {
			Color.a = 0;
		}
	}

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);
	
	float shadowFactor = _PSGetShadowEffect(true, In);
	
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * shadowFactor + ImageTextureIsNight));
	// Specular effect next.
	// 
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Headlights effect use original Color.
	litColor += _PSApplyMstsLights(Color.rgb, In, shadowFactor);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	//_PSApplyShadowColor(litColor, In); // This is a debug method
	return float4(litColor, Color.a);
}

float4 PSImageNoClamp(in VERTEX_OUTPUT In) : COLOR0
{
    return PSImage(false, In);
}

float4 PSImageClamp(in VERTEX_OUTPUT In) : COLOR0
{
    return PSImage(true, In);
}

float4 PSPbr(in VERTEX_OUTPUT_PBR In, bool isFrontFace : SV_IsFrontFace) : COLOR0
{
	// This is for being able to call the original functions for the ambient lighting:
	VERTEX_OUTPUT InGeneral = (VERTEX_OUTPUT)0;
	InGeneral.Position = In.Position;
	InGeneral.Color = In.Color;
	InGeneral.RelPosition = In.RelPosition;
	InGeneral.TexCoords = In.TexCoords;
	InGeneral.Normal_Light = In.Normal_Light;
	InGeneral.Shadow = In.Shadow;
	InGeneral.Fog = In.Tangent.w;

	float4 Color = _PSTex2D(Image, In.TexCoords, TextureCoordinates1.x);
	Color.rgb = _PSSrgbToLinear(Color.rgb);
	// Apply the linear multipliers.
	Color *= In.Color * BaseColorFactor;
	// Alpha testing. Without the > 0 check glithches appear.
	if (ReferenceAlpha > 0 && ReferenceAlpha > Color.a)
		discard;
	
	///////////////////////
	// Contributions from the OpenRails environment:
	_PSSceneryFade(Color, InGeneral);
	float fade = Color.a;
	///////////////////////
	
    float3 litColor;
    
    if (!ZBias_Lighting.y)
    {
    	// Unlit material
        litColor = Color.rgb;
    }
    else
    {
        // Diffuse material

        // Metallic-roughness
        float occlusion = 1;
        float metallic = 1;
        float roughness = 1;
        if (TexturePacking == 0)
        {
            if (OcclusionFactor.x > 0)
                occlusion = _PSTex2D(Occlusion, In.TexCoords, TextureCoordinates2.w).r;

            if (OcclusionFactor.y > 0 || OcclusionFactor.z > 0)
            {
                float3 orm = _PSTex2D(MetallicRoughness, In.TexCoords, TextureCoordinates1.y).rgb;
                roughness = orm.g;
                metallic = orm.b;
            }
        }
        else if (TexturePacking == 1 || TexturePacking == 3 || TexturePacking == 4 || TexturePacking == 5)
        {
            float3 orm = _PSTex2D(MetallicRoughness, In.TexCoords, TextureCoordinates1.y).rgb;
            if (TexturePacking == 3 || TexturePacking == 5)
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
            float4 nrm = _PSTex2D(Normal, In.TexCoords, TextureCoordinates1.z);
            roughness = nrm.b;
            metallic = nrm.a;
            occlusion = _PSTex2D(Occlusion, In.TexCoords, TextureCoordinates2.w).r;
        }

        float perceptualRoughness = clamp(roughness * OcclusionFactor.y, MinRoughness, 1.0);
        float alphaRoughness = perceptualRoughness * perceptualRoughness;
        float roughnessSq = alphaRoughness * alphaRoughness;
	
        metallic = clamp(metallic * OcclusionFactor.z, 0.0, 1.0);
	
        float3 f0 = (float3) 0.04; // (float3)(pow((ior - 1) / (ior + 1), 2)) = (float3)0.04, if ior = 1.5
        float3 f90 = (float3) 1.0;
        float3 diffuseColor = Color.rgb * (f90 - f0);
        diffuseColor *= 1.0 - metallic;
	
        float3 specularColor = lerp(f0, Color.rgb, metallic);
        float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);
        float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
        float3 specularEnvironmentR0 = specularColor.rgb;
        float3 specularEnvironmentR90 = float3(1.0, 1.0, 1.0) * reflectance90;
	
        float3 n = _PSGetNormal(In, HasTangents, NormalScale, Normal, TextureCoordinates1.z, isFrontFace);
        float3 v = normalize(-In.RelPosition.xyz);

        float NdotV = abs(dot(n, v)) + 0.001;

        float3 reflection = normalize(reflect(-v, n));
        litColor = _PSGetIBLSpecular(specularColor, NdotV, perceptualRoughness, reflection);
        litColor += _PSGetIBLDiffuse(diffuseColor, n);
        // Occlusion doesn't apply to lights, so do it in advance
        litColor = lerp(litColor, litColor * occlusion, OcclusionFactor.x);

#ifdef CLEARCOAT
        float3 clearcoat = (float3) 0;
        float clearcoatRoughnessSq = 0;
        float3 clearcoatF0 = (float3) 0.04; // (float3)(pow((ior - 1) / (ior + 1), 2)) = (float3)0.04, if ior = 1.5
        float3 clearcoatNormal = n;
        float clearcoatNdotV = NdotV;
        float clearcoatFactor = ClearcoatFactor;

        if (ClearcoatFactor > 0)
        {
            float clearcoatSample = _PSTex2D(Clearcoat, In.TexCoords, TextureCoordinates2.x).r;
            clearcoatFactor *= clearcoatSample;

	        // TODO: implement clearcoat texturepacking for being able to check whether these two textures are the same
            float clearcoatRoughness = _PSTex2D(ClearcoatRoughness, In.TexCoords, TextureCoordinates2.y).g;
            clearcoatRoughness = clamp(clearcoatRoughness * ClearcoatRoughnessFactor, 0.0, 1.0);

            float clearcoatAlphaRoughness = clearcoatRoughness * clearcoatRoughness;
            clearcoatRoughnessSq = clearcoatAlphaRoughness * clearcoatAlphaRoughness;

		// TODO: implement clearcoat texturepacking for being able to check whether the clearcoat normal is the same as the base normal
            clearcoatNormal = _PSGetNormal(In, HasTangents, ClearcoatNormalScale, ClearcoatNormal, TextureCoordinates2.z, isFrontFace);
            clearcoatNdotV = abs(dot(clearcoatNormal, v)) + 0.001;

            float3 Fr = max((float3) (1.0 - clearcoatRoughness), f0) - f0;
            float3 k_S = f0 + Fr * pow5(1.0 - clearcoatNdotV);

            float3 clearcoatReflection = normalize(reflect(-v, clearcoatNormal));
            clearcoat = _PSGetIBLSpecular(k_S, clearcoatNdotV, clearcoatRoughness, clearcoatReflection);
            clearcoat = lerp(clearcoat, clearcoat * occlusion, OcclusionFactor.x);
        }
#endif

        ///////////////////////
        // Contributions from the OpenRails environment:
        float shadowFactor = _PSGetShadowEffect(true, InGeneral);
        float diffuseShadowFactor = lerp(ShadowBrightness, FullBrightness, saturate(shadowFactor));
        ///////////////////////

        float3 diffuseContrib = (float3) 0;
        float3 specContrib = (float3) 0;
        float attenuation = 1;

	    [fastopt]
        for (int i = 0; i < NumLights; i++)
        {
            float3 l;
            if (LightTypes[i] == LightType_Directional)
            {
                l = normalize(-LightDirections[i]); // normalize(pointToLight)
                attenuation = 1;
            }
            else
            {
                float3 pointToLight = LightPositions[i] - In.Shadow.xyz; // In.Shadow.xyz is the absolute world position of the point
                float pointLightDistance = length(pointToLight);
                l = pointToLight / pointLightDistance; // normalize(pointToLight)
                attenuation = 1;
                if (LightTypes[i] == LightType_Headlight)
                {
                    attenuation *= clamp(1 - pointLightDistance * LightRangesRcp[i], 0, 1); // The pre-PBR headlight used linear range attenuation.
                }
                else
                {
                    attenuation /= pow(pointLightDistance, 2); // The realistic range attenuation is inverse-squared.
                    attenuation *= clamp(1 - pow(pointLightDistance * LightRangesRcp[i], 4), 0, 1);
                }

                if (LightTypes[i] == LightType_Spot || LightTypes[i] == LightType_Headlight)
                    attenuation *= smoothstep(LightOuterConeCos[i], LightInnerConeCos[i], dot(LightDirections[i], -l));
            }

            float NdotL = clamp(dot(n, l), 0.001, 1.0);

            if (NdotL > 0.001 || NdotV > 0.001)
            {
                float3 h = normalize(l + v);

                float NdotH = clamp(dot(n, h), 0.0, 1.0);
                float LdotH = clamp(dot(l, h), 0.0, 1.0);
                float VdotH = clamp(dot(v, h), 0.0, 1.0);

                float fPow = pow5(clamp(1.0 - VdotH, 0.0, 1.0));
                float3 F = specularEnvironmentR0 + (specularEnvironmentR90 - specularEnvironmentR0) * fPow;

                float attenuationL = 2.0 * NdotL / (NdotL + sqrt(roughnessSq + (1.0 - roughnessSq) * (NdotL * NdotL)));
                float attenuationV = 2.0 * NdotV / (NdotV + sqrt(roughnessSq + (1.0 - roughnessSq) * (NdotV * NdotV)));
                float G = attenuationL * attenuationV;

                float f = (NdotH * roughnessSq - NdotH) * NdotH + 1.0;
                float D = roughnessSq / (M_PI * f * f);

                float3 intensity = LightColorIntensities[i] * attenuation;
                
                diffuseContrib += intensity * NdotL * (1.0 - F) * diffuseColor / M_PI * diffuseShadowFactor;
                specContrib += intensity * NdotL * F * G * D / (4.0 * NdotL * NdotV) * shadowFactor;

#ifdef CLEARCOAT
                if (ClearcoatFactor > 0)
                {
                    float clearcoatNdotL = clamp(dot(clearcoatNormal, l), 0.001, 1.0);
                    float clearcoatNdotH = clamp(dot(clearcoatNormal, h), 0.0, 1.0);

                    F = clearcoatF0 + (f90 - clearcoatF0) * fPow;

                    attenuationL = 2.0 * clearcoatNdotL / (clearcoatNdotL + sqrt(clearcoatRoughnessSq + (1.0 - clearcoatRoughnessSq) * (clearcoatNdotL * clearcoatNdotL)));
                    attenuationV = 2.0 * clearcoatNdotV / (clearcoatNdotV + sqrt(clearcoatRoughnessSq + (1.0 - clearcoatRoughnessSq) * (clearcoatNdotV * clearcoatNdotV)));
                    G = attenuationL * attenuationV;

                    f = (clearcoatNdotH * clearcoatRoughnessSq - clearcoatNdotH) * clearcoatNdotH + 1.0;
                    D = clearcoatRoughnessSq / (M_PI * f * f);

                    clearcoat += intensity * clearcoatNdotL * F * G * D / (4.0 * clearcoatNdotL * clearcoatNdotV) * shadowFactor;
                }
#endif
            }
            // Light 0 is the sun, the shadow factors are needed only for that one. For other lights they do not apply, they no longer needed.
            shadowFactor = 1;
            diffuseShadowFactor = 1;
        }
        litColor += diffuseContrib + specContrib;

	    // Emissive color:
        float3 emissive = _PSSrgbToLinear(_PSTex2D(Emissive, In.TexCoords, TextureCoordinates1.w).rgb) * EmissiveFactor;
        litColor += emissive;

#ifdef CLEARCOAT
        if (ClearcoatFactor > 0)
        {
            float3 clearcoatF = clearcoatF0 + (f90 - clearcoatF0) * pow5(clamp(1.0 - clearcoatNdotV, 0.0, 1.0));
            litColor = litColor * (1.0 - clearcoatF * clearcoatFactor) + clearcoat * clearcoatFactor;
        }
#endif

    }

	///////////////////////
	// Contributions from the OpenRails environment:
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, InGeneral), Overcast.x);
	// And fogging is last.
	_PSApplyFog(litColor, InGeneral);
	//_PSApplyShadowColor(litColor, InGeneral); // a debug function only
	///////////////////////

	// Transform back to sRGB:
	litColor = _PSLinearToSrgb(litColor);

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
	litColor += _PSApplyMstsLights(Color.rgb, In, 1);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(true, In) + ImageTextureIsNight));
	// No specular effect for terrain.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), Overcast.x);
	// Night-time darkens everything, except night-time textures.
	litColor *= NightColorModifier;
	// Overlay image for terrain.
	litColor.rgb *= tex2D(Overlay, In.TexCoords.xy * OverlayScale).rgb * 2;
	// Headlights effect use original Color.
	litColor += _PSApplyMstsLights(Color.rgb, In, _PSGetShadowEffect(true, In));
	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	//_PSApplyShadowColor(litColor, In); // This is a debug method
	return float4(litColor, Color.a);
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{
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
	litColor += _PSApplyMstsLights(Color.rgb, In, 1);
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
	litColor += _PSApplyMstsLights(Color.rgb, In, 1);
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
	litColor += _PSApplyMstsLights(Color.rgb, In, 1);
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

technique Image {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSGeneral();
		PixelShader = compile ps_4_0 PSImageNoClamp();
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

technique PbrMorphed {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSMorphing();
		PixelShader = compile ps_4_0 PSPbr();
	}
}

technique Transfer {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSTransfer();
		PixelShader = compile ps_4_0 PSImageClamp();
	}
}

technique Forest {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSForest();
		PixelShader = compile ps_4_0 PSVegetation();
	}
}

technique Vegetation {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSGeneral();
		PixelShader = compile ps_4_0 PSVegetation();
	}
}

technique Terrain {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSTerrain();
		PixelShader = compile ps_4_0 PSTerrain();
	}
}

technique DarkShade {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSGeneral();
		PixelShader = compile ps_4_0 PSDarkShade();
	}
}

technique HalfBright {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSGeneral();
		PixelShader = compile ps_4_0 PSHalfBright();
	}
}

technique FullBright {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSGeneral();
		PixelShader = compile ps_4_0 PSFullBright();
	}
}

technique SignalLight {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSSignalLight();
		PixelShader = compile ps_4_0_level_9_3 PSSignalLight();
	}
}

technique SignalLightGlow {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_3 VSSignalLightGlow();
		PixelShader = compile ps_4_0_level_9_3 PSSignalLight();
	}
}
