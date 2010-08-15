////////////////////////////////////////////////////////////////////////////////
//                     S H A D O W   M A P   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
//float4x4 World;               // model -> world (currently unused)
//float4x4 View;                // world -> view (currently unused)
//float4x4 Projection;          // view -> projection (currently unused)
float4x4 WorldViewProjection; // model -> world -> view -> projection

texture ImageTexture;
sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position       : POSITION;
	float3 TexCoord_Depth : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSShadowMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSShadowMap(in VERTEX_OUTPUT In) : COLOR0
{
	if (tex2D(Image, In.TexCoord_Depth.xy).a < 0.25)
		discard;
	return float4(In.TexCoord_Depth.z, In.TexCoord_Depth.z * In.TexCoord_Depth.z, 0, 0);
}

float4 PSShadowMapBlocker(in VERTEX_OUTPUT In) : COLOR0
{
	return 0;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ShadowMap
{
	pass Pass_0
	{
        VertexShader = compile vs_2_0 VSShadowMap ( );
        PixelShader = compile ps_2_0 PSShadowMap ( );
	}
}

technique ShadowMapBlocker
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 VSShadowMap ( );
		PixelShader = compile ps_2_0 PSShadowMapBlocker ( );
	}
}
