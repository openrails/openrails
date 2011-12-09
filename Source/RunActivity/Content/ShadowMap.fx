// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                     S H A D O W   M A P   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
//float4x4 World;               // model -> world (currently unused)
float4x4 View;                // world -> view (currently unused)
//float4x4 Projection;          // view -> projection (currently unused)
float4x4 WorldViewProjection; // model -> world -> view -> projection

float ImageBlurStep; // = 1 / shadow map texture width and height

texture ImageTexture;
sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};

sampler ShadowMap = sampler_state
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
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position       : POSITION;
	float3 TexCoord_Depth : TEXCOORD0;
};

struct VERTEX_OUTPUT_BLUR
{
	float4 Position       : POSITION;
	float2 SampleCentre : TEXCOORD0;
	float2 Sample_01 : TEXCOORD1;
	float2 Sample_23 : TEXCOORD2;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSShadowMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.Position.z = saturate(Out.Position.z);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT VSShadowMapForest(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 eyeVector = normalize(View._m02_m12_m22);
	float3 upVector = float3(0, -1, 0);
	float3 sideVector = normalize(cross(eyeVector, upVector));

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position;
	newPosition += (In.TexCoord.x - 0.5f) * sideVector * In.Normal.x;
	newPosition += (In.TexCoord.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.Position.z = saturate(Out.Position.z);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapHorzBlur(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	const float2 halfPixelOffset = float2(0.5, 0.5);

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.SampleCentre = (In.TexCoord + halfPixelOffset) / ImageBlurStep;
	Out.Sample_01 = (In.TexCoord + halfPixelOffset - float2(1.5, 0)) / ImageBlurStep;
	Out.Sample_23 = (In.TexCoord + halfPixelOffset + float2(1.5, 0)) / ImageBlurStep;

	return Out;
}

VERTEX_OUTPUT_BLUR VSShadowMapVertBlur(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT_BLUR Out;
	
	const float2 halfPixelOffset = float2(0.5, 0.5);
	float2 offsetTexCoord = In.TexCoord + halfPixelOffset;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.SampleCentre = offsetTexCoord / ImageBlurStep;
	Out.Sample_01 = (offsetTexCoord - float2(0, 1.5)) / ImageBlurStep;
	Out.Sample_23 = (offsetTexCoord + float2(0, 1.5)) / ImageBlurStep;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSShadowMap(in VERTEX_OUTPUT In) : COLOR0
{
	float alpha = tex2D(Image, In.TexCoord_Depth.xy).a;
	
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
	float2 centreTap =	tex2D(ShadowMap, In.SampleCentre).rg	* 0.4414401;	
	float2 tap01 =		tex2D(ShadowMap, In.Sample_01).rg * 0.2774689;
	float2 tap23 =		tex2D(ShadowMap, In.Sample_23).rg * 0.2774689;
		
	return float4(tap01 + centreTap + tap23, 0, 0);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ShadowMap {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMap();
		PixelShader = compile ps_2_0 PSShadowMap();
	}
}

technique ShadowMapForest {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMapForest();
		PixelShader = compile ps_2_0 PSShadowMap();
	}
}

technique ShadowMapBlocker {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMap();
		PixelShader = compile ps_2_0 PSShadowMapBlocker();
	}
}

technique ShadowMapBlur {
	pass Blur_X {
		VertexShader = compile vs_2_0 VSShadowMapHorzBlur();
		PixelShader = compile ps_2_0 PSShadowMapBlur();
	}
	pass Blur_Y {
		VertexShader = compile vs_2_0 VSShadowMapVertBlur();
		PixelShader = compile ps_2_0 PSShadowMapBlur();
	}
}
