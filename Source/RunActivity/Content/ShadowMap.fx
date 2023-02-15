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

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 WorldViewProjection;  // model -> world -> view -> projection
float3   SideVector;
float    ImageBlurStep;  // = 1 / shadow map texture width and height
texture  ImageTexture;
float4x4 Bones[50]; // model -> world [max number of bones]

sampler ImageSampler = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};

sampler ShadowMapSampler = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Point;
};

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
	float2 TexCoord    : TEXCOORD0;
	float3 Normal      : NORMAL;
	float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
};

struct VERTEX_INPUT_SKINNED
{
	float4 Position    : POSITION;
	float2 TexCoord    : TEXCOORD0;
	float3 Normal      : NORMAL;
	float4 Tangent     : TANGENT;
	float2 TexCoordsPbr: TEXCOORD1;
	uint4 Joints       : BLENDINDICES0;
	float4 Weights     : BLENDWEIGHT0;
	float4 Color       : COLOR0;
	float4x4 Instance  : TEXCOORD2;
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

VERTEX_OUTPUT VSShadowMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	if (determinant(In.Instance) != 0) {
		In.Position = mul(In.Position, transpose(In.Instance));
		In.Normal = mul(In.Normal, (float3x3)transpose(In.Instance));
	}

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
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
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapHorzBlur(in VERTEX_INPUT_BLUR In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	float2 offsetTexCoord = In.TexCoord + float2(0.5, 0.5);

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.SampleCentre = offsetTexCoord * ImageBlurStep;
	Out.Sample_01 = (offsetTexCoord - float2(1.5, 0)) * ImageBlurStep;
	Out.Sample_23 = (offsetTexCoord + float2(1.5, 0)) * ImageBlurStep;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapVertBlur(in VERTEX_INPUT_BLUR In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	float2 offsetTexCoord = In.TexCoord + float2(0.5, 0.5);

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.SampleCentre = offsetTexCoord * ImageBlurStep;
	Out.Sample_01 = (offsetTexCoord - float2(0, 1.5)) * ImageBlurStep;
	Out.Sample_23 = (offsetTexCoord + float2(0, 1.5)) * ImageBlurStep;

	return Out;
}

VERTEX_OUTPUT VSShadowMapNormalMap(in VERTEX_INPUT_NORMALMAP In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	if (determinant(In.Instance) != 0) {
		In.Position = mul(In.Position, transpose(In.Instance));
	}

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT VSShadowMapSkinned(in VERTEX_INPUT_SKINNED In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	if (determinant(In.Instance) != 0) {
		In.Position = mul(In.Position, transpose(In.Instance));
	}

	float4x4 skinTransform = 0;

	skinTransform += Bones[In.Joints.x] * In.Weights.x;
	skinTransform += Bones[In.Joints.y] * In.Weights.y;
	skinTransform += Bones[In.Joints.z] * In.Weights.z;
	skinTransform += Bones[In.Joints.w] * In.Weights.w;

	In.Position = mul(In.Position, skinTransform);

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSShadowMap(in VERTEX_OUTPUT In) : COLOR0
{
	float alpha = tex2D(ImageSampler, In.TexCoord_Depth.xy).a;
	
	if(alpha < 0.25)
		discard;
	
	return float4(In.TexCoord_Depth.z, In.TexCoord_Depth.z * In.TexCoord_Depth.z, 0, 0);
}

float4 PSShadowMapBlocker() : COLOR0
{
	return 0;
}

float4 PSShadowMapBlur(in VERTEX_OUTPUT_BLUR In) : COLOR0
{
	float2 centreTap =	tex2D(ShadowMapSampler, In.SampleCentre).rg	* 0.4430448;
	float2 tap01 =		tex2D(ShadowMapSampler, In.Sample_01).rg * 0.2784776;
	float2 tap23 =		tex2D(ShadowMapSampler, In.Sample_23).rg * 0.2784776;
		
	return float4(tap01 + centreTap + tap23, 0, 0);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ShadowMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMap();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMap();
	}
}

technique ShadowMapNormalMap {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMapNormalMap();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMap();
	}
}

technique ShadowMapSkinned {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMapSkinned();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMap();
	}
}

technique ShadowMapForest {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMapForest();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMap();
	}
}

technique ShadowMapBlocker {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMap();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMapBlocker();
	}
}

technique ShadowMapBlur {
	pass Blur_X {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMapHorzBlur();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMapBlur();
	}
	pass Blur_Y {
		VertexShader = compile vs_4_0_level_9_1 VSShadowMapVertBlur();
		PixelShader = compile ps_4_0_level_9_1 PSShadowMapBlur();
	}
}
