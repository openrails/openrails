////////////////////////////////////////////////////////////////////////////////
//                     S H A D O W   M A P   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 World;               // model -> world
//float4x4 View;                // world -> view (currently unused)
//float4x4 Projection;          // view -> projection (currently unused)
float4x4 WorldViewProjection; // model -> world -> view -> projection

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float  Depth : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSShadowMap(VERTEX_INPUT In, out float4 Position : POSITION)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Position = mul(In.Position, WorldViewProjection);
	Out.Depth = Position.z;

	return Out;
}

float4 PSShadowMap(VERTEX_OUTPUT In) : COLOR
{
	return In.Depth;
}

technique ShadowMap
{
	pass Pass_0
	{
        VertexShader = compile vs_2_0 VSShadowMap ( );
        PixelShader = compile ps_2_0 PSShadowMap ( );
	}
}
