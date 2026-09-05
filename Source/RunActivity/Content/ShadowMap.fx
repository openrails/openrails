// COPYRIGHT 2010, 2011, 2013 by the Open Rails project.
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
//                     S H A D O W   M A P   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

#define VERTEX_OPTION_NONE                          0u
#define VERTEX_OPTION_HAS_SKIN                  (1u << 0)
#define VERTEX_OPTION_NORM_USHORT_WEIGHT        (1u << 1)
#define VERTEX_OPTION_NORM_USHORT_COLOR         (1u << 2)
#define VERTEX_OPTION_NORM_USHORT_POSITION      (1u << 3)
#define VERTEX_OPTION_NORM_USHORT_TEXCOORD      (1u << 4)
#define VERTEX_OPTION_NORM_SBYTE_NORMAL         (1u << 5)
#define VERTEX_OPTION_NORM_SBYTE_TANGENT        (1u << 6)
#define VERTEX_OPTION_NORM_SBYTE_POSITION       (1u << 7)
#define VERTEX_OPTION_NORM_SBYTE_TEXCOORD       (1u << 8)
#define VERTEX_OPTION_INT_SBYTE_POSITION        (1u << 9)
#define VERTEX_OPTION_INT_SBYTE_TEXCOORD        (1u << 10)
#define VERTEX_OPTION_INT_USHORT_POSITION       (1u << 11)
#define VERTEX_OPTION_INT_USHORT_TEXCOORD       (1u << 12)
#define VERTEX_OPTION_SKIN_JOINT_SINGLE         (1u << 13)
#define VERTEX_OPTION_SKIN_JOINT_DOUBLE         (1u << 14)

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

cbuffer PerFrame
{
    float4x4 View; // world -> view
    float4x4 Projection; // view -> projection
    float3 SideVector;
};

cbuffer PerObject
{
    float4x4 World; // model -> world [max number of bones]
    float4 MorphConfig[2]; // 0.x: POS, 0.y: NORM, 0.z: TANG, 0.w: TEX0, 1.x: TEX1, 1.y: COL0, 1.z: targets count, 1.w: attributes count
    float4 MorphWeights[2];
    float ImageBlurStep; // = 1 / shadow map texture width and height
    int VertexShaderOptions;
};

int    ShadowMapIndex;

Texture2D ImageTexture;
SamplerState ImageSampler;

Texture2DArray ShadowMapArray;
SamplerState ShadowMapSampler;

Texture2D BonesTexture;

static const float4x4 Identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };


////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
	float3 Normal   : NORMAL;
	float4x4 Instance : TEXCOORD1;
};

struct VERTEX_INPUT_FOREST
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
	float3 Normal   : NORMAL;
};

struct VERTEX_INPUT_BLUR
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
};

struct VERTEX_INPUT_NORMALMAP
{
	float4 Position    : POSITION;
	float2 TexCoords    : TEXCOORD0;
	float3 Normal      : NORMAL;
	float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
};

struct VERTEX_INPUT_SKINNED
{
	float4 Position    : POSITION;
	float2 TexCoords    : TEXCOORD0;
	float3 Normal      : NORMAL;
	float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
    min16uint4 Joints  : BLENDINDICES0;
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
    float4 MorphTargets[8] : POSITION1;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position       : POSITION;
	float3 TexCoord_Depth : TEXCOORD0;
};

struct VERTEX_OUTPUT_BLUR
{
	float4 Position     : POSITION;
	float2 SampleCentre : TEXCOORD0;
	float2 Sample_01    : TEXCOORD1;
	float2 Sample_23    : TEXCOORD2;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

float4x4 _VSBoneMatrix(int index)
{
    float4 row1 = BonesTexture.Load(int3(0, index, 0));
    float4 row2 = BonesTexture.Load(int3(1, index, 0));
    float4 row3 = BonesTexture.Load(int3(2, index, 0));
    float4 row4 = BonesTexture.Load(int3(3, index, 0));

    return float4x4(row1, row2, row3, row4);
}

float4x4 _VSSkinTransform(in int4 Joints, in float4 Weights)
{
    float4x4 skinTransform = 0;

    skinTransform += _VSBoneMatrix(Joints.x) * (float) Weights.x;
    skinTransform += _VSBoneMatrix(Joints.y) * (float) Weights.y;
    skinTransform += _VSBoneMatrix(Joints.z) * (float) Weights.z;
    skinTransform += _VSBoneMatrix(Joints.w) * (float) Weights.w;

    return skinTransform;
}

float4 _VSUnpackUshort(float2 value)
{
    uint u1 = asuint(value.x);
    uint u2 = asuint(value.y);
    uint4 ushortUint;
    ushortUint.x = u1 & 0xFFFF;
    ushortUint.y = u1 >> 16;
    ushortUint.z = u2 & 0xFFFF;
    ushortUint.w = u2 >> 16;
    return float4(ushortUint);
}

float4 _VSUnpackSByte(float value)
{
    uint u = asuint(value);
    // Left shift moves the sign byte to the most significant bits,
    // then a signed int right shift performs the sign extension.
    int4 sbyteInt;
    sbyteInt.x = int(u << 24) >> 24;
    sbyteInt.y = int(u << 16) >> 24;
    sbyteInt.z = int(u << 8) >> 24;
    sbyteInt.w = int(u) >> 24;
    
    return float4(sbyteInt);
}

VERTEX_OUTPUT _VSPbr(float4 position, float3 normal, float4 tangent,
                        float2 texCoordsBase, float2 texCoordsPbr, float4 color,
                        int4 joints, float4 weights,
                        float4x4 instance, float4 morphTargets[8])
{
    // Workaround for the MonoGame limitation of not being able to supply these vertex attribute formats...
    if ((VertexShaderOptions & VERTEX_OPTION_NORM_SBYTE_POSITION) != 0u)
        position.xyz = max(_VSUnpackSByte(position.x).xyz / 127.0, -1.0);
    else if ((VertexShaderOptions & VERTEX_OPTION_NORM_USHORT_POSITION) != 0u)
        position.xyz = _VSUnpackUshort(position.xy).xyz / 65535.0;
    else if ((VertexShaderOptions & VERTEX_OPTION_INT_SBYTE_POSITION) != 0u)
        position.xyz = _VSUnpackSByte(position.x).xyz;
    else if ((VertexShaderOptions & VERTEX_OPTION_INT_USHORT_POSITION) != 0u)
        position.xyz = _VSUnpackUshort(position.xy).xyz;

    if ((VertexShaderOptions & VERTEX_OPTION_NORM_USHORT_WEIGHT) != 0u)
        weights = _VSUnpackUshort(weights.xy) / 65535.0;

    if ((VertexShaderOptions & VERTEX_OPTION_NORM_SBYTE_TEXCOORD) != 0u)
    {
        texCoordsBase = max(_VSUnpackSByte(texCoordsBase.x).xy / 127.0, -1.0);
        texCoordsPbr = max(_VSUnpackSByte(texCoordsPbr.x).xy / 127.0, -1.0);
    }
    else if ((VertexShaderOptions & VERTEX_OPTION_NORM_USHORT_TEXCOORD) != 0u)
    {
        float4 texCoords = _VSUnpackUshort(float2(texCoordsBase.x, texCoordsPbr.x)) / 65535.0;
        texCoordsBase = texCoords.xy;
        texCoordsPbr = texCoords.zw;
    }
    else if ((VertexShaderOptions & VERTEX_OPTION_INT_SBYTE_TEXCOORD) != 0u)
    {
        texCoordsBase = _VSUnpackSByte(texCoordsBase.x).xy;
        texCoordsPbr = _VSUnpackSByte(texCoordsPbr.x).xy;
    }
    else if ((VertexShaderOptions & VERTEX_OPTION_INT_USHORT_TEXCOORD) != 0u)
    {
        float4 texCoords = _VSUnpackUshort(float2(texCoordsBase.x, texCoordsPbr.x));
        texCoordsBase = texCoords.xy;
        texCoordsPbr = texCoords.zw;
    }

	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	if (determinant(instance) != 0)
		position = mul(position, transpose(instance));

    Out.Position = position;
    
    int attrCount = MorphConfig[1].w;

    [unroll(8)]
    for (int i = 0; i < MorphConfig[1].z; i++)
    {
        float weight = MorphWeights[i / 4][i % 4];
        int offset = attrCount * i;

        if (MorphConfig[0].x != -1)
            Out.Position.xyz += morphTargets[offset + MorphConfig[0].x].xyz * weight;
        if (MorphConfig[0].w != -1)
            Out.TexCoord_Depth.xy += morphTargets[offset + MorphConfig[0].w].xy * weight;
    }

    float4x4 worldTransform = World;
    if ((VertexShaderOptions & VERTEX_OPTION_HAS_SKIN) != 0u)
    {
        if ((VertexShaderOptions & VERTEX_OPTION_SKIN_JOINT_SINGLE) != 0u)
            worldTransform = _VSBoneMatrix(joints.x);
        else
            worldTransform = _VSSkinTransform(joints, weights);
    }

    Out.Position = mul(mul(mul(Out.Position, worldTransform), View), Projection);
	Out.TexCoord_Depth.xy = texCoordsBase;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT VSShadowMap(in VERTEX_INPUT In)
{
    return _VSPbr(In.Position, In.Normal, float4(0, 0, 0, 1), In.TexCoord, float2(0, 0), float4(1, 1, 1, 1), min16uint4(0, 0, 0, 0), float4(0, 0, 0, 0), In.Instance, (float4[8]) 0);
}

VERTEX_OUTPUT VSShadowMapNormalMap(in VERTEX_INPUT_NORMALMAP In)
{
    return _VSPbr(In.Position, In.Normal, In.Tangent, In.TexCoords, In.TexCoordsPbr, In.Color, min16uint4(0, 0, 0, 0), float4(0, 0, 0, 0), In.Instance, (float4[8]) 0);
}

VERTEX_OUTPUT VSShadowMapSkinned(in VERTEX_INPUT_SKINNED In)
{
    return _VSPbr(In.Position, In.Normal, In.Tangent, In.TexCoords, In.TexCoordsPbr, In.Color, In.Joints, In.Weights, In.Instance, (float4[8]) 0);
}

VERTEX_OUTPUT VSShadowMapMorphed(in VERTEX_INPUT_MORPHED In)
{
    return _VSPbr(In.Position, In.Normal, In.Tangent, In.TexCoords, In.TexCoordsPbr, In.Color, In.Joints, In.Weights, (float4x4) 0, In.MorphTargets);
}

VERTEX_OUTPUT VSShadowMapForest(in VERTEX_INPUT_FOREST In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 upVector = float3(0, -1, 0);

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position.xyz;
	newPosition += (In.TexCoord.x - 0.5f) * SideVector * In.Normal.x;
	newPosition += (In.TexCoord.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
    Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapHorzBlur(in VERTEX_INPUT_BLUR In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	float2 offsetTexCoord = In.TexCoord + float2(0.5, 0.5);

	Out.Position = mul(In.Position, Identity);
	Out.SampleCentre = offsetTexCoord * ImageBlurStep;
	Out.Sample_01 = (offsetTexCoord - float2(1.5, 0)) * ImageBlurStep;
	Out.Sample_23 = (offsetTexCoord + float2(1.5, 0)) * ImageBlurStep;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapVertBlur(in VERTEX_INPUT_BLUR In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	float2 offsetTexCoord = In.TexCoord + float2(0.5, 0.5);

	Out.Position = mul(In.Position, Identity);
	Out.SampleCentre = offsetTexCoord * ImageBlurStep;
	Out.Sample_01 = (offsetTexCoord - float2(0, 1.5)) * ImageBlurStep;
	Out.Sample_23 = (offsetTexCoord + float2(0, 1.5)) * ImageBlurStep;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSShadowMap(in VERTEX_OUTPUT In) : SV_Target
{
	float alpha = ImageTexture.Sample(ImageSampler, In.TexCoord_Depth.xy).a;
	
	if(alpha < 0.25)
		discard;
	
	return float4(In.TexCoord_Depth.z, In.TexCoord_Depth.z * In.TexCoord_Depth.z, 0, 0);
}

float4 PSShadowMapBlocker() : SV_Target
{
	return 0;
}

float4 PSShadowMapBlur(in VERTEX_OUTPUT_BLUR In) : SV_Target
{
    float3 uv_idx_c = float3(In.SampleCentre, ShadowMapIndex);
    float3 uv_idx_0 = float3(In.Sample_01, ShadowMapIndex);
    float3 uv_idx_2 = float3(In.Sample_23, ShadowMapIndex);

    float2 centreTap = ShadowMapArray.Sample(ShadowMapSampler, uv_idx_c).rg * 0.4430448;
    float2 tap01 = ShadowMapArray.Sample(ShadowMapSampler, uv_idx_0).rg * 0.2784776;
    float2 tap23 = ShadowMapArray.Sample(ShadowMapSampler, uv_idx_2).rg * 0.2784776;
		
	return float4(tap01 + centreTap + tap23, 0, 0);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ShadowMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMap();
		PixelShader = compile ps_4_0 PSShadowMap();
	}
}

technique ShadowMapNormalMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMapNormalMap();
		PixelShader = compile ps_4_0 PSShadowMap();
	}
}

technique ShadowMapSkinned {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMapSkinned();
		PixelShader = compile ps_4_0 PSShadowMap();
	}
}

technique ShadowMapMorphed {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMapMorphed();
		PixelShader = compile ps_4_0 PSShadowMap();
	}
}

technique ShadowMapForest {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMapForest();
		PixelShader = compile ps_4_0 PSShadowMap();
	}
}

technique ShadowMapBlocker {
	pass Pass_0 {
		VertexShader = compile vs_4_0 VSShadowMap();
		PixelShader = compile ps_4_0 PSShadowMapBlocker();
	}
}

technique ShadowMapBlur {
	pass Blur_X {
		VertexShader = compile vs_4_0 VSShadowMapHorzBlur();
		PixelShader = compile ps_4_0 PSShadowMapBlur();
	}
	pass Blur_Y {
		VertexShader = compile vs_4_0 VSShadowMapVertBlur();
		PixelShader = compile ps_4_0 PSShadowMapBlur();
	}
}
