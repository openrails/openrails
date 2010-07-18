// INCLUDED FOR EXPERIMENTS WITH SHADOW MAPPING
//-----------------------------------------------------------------------------
// ShadowMap.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

float4x4 World : WORLD;
float4x4 LightView : VIEW;
float4x4 LightProj : PROJECTION;

struct VSOUTPUT_SHADOW
{
	float4 position : POSITION;
	float  depth    : TEXCOORD0;
};

VSOUTPUT_SHADOW VSShadowMap(float4 inPos : POSITION)
{
	float4x4 WorldViewProjection = mul(mul(World, LightView), LightProj);
	
	VSOUTPUT_SHADOW Out = (VSOUTPUT_SHADOW)0;
	
	Out.position = mul(inPos, WorldViewProjection);
	Out.depth    = Out.position.z;
	
	return Out;
}

float4 PSShadowMap(VSOUTPUT_SHADOW In) : COLOR0
{
	return float4(In.depth, In.depth, In.depth, 1.0f);
}

technique ShadowMap
{
	pass Pass_0
	{
        VertexShader = compile vs_2_0 VSShadowMap ( );
        PixelShader = compile ps_2_0 PSShadowMap ( );
	}
}
