////////////////////////////////////////////////////////////////////////////////
//                     L I G H T   C O N E   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 WorldViewProjection;  // model -> world -> view -> projection

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float3 Position : POSITION0; // position x, y, z
	float4 Color    : COLOR0;    // color r, g, b, a
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position  : POSITION0;
	float4 Color     : COLOR0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSLightCone(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
    Out.Position = mul(float4(In.Position, 1), WorldViewProjection);
    Out.Color = In.Color;
	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSLightCone(in VERTEX_OUTPUT In) : COLOR0
{
	return In.Color;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique LightCone {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSLightCone();
		PixelShader = compile ps_2_0 PSLightCone();
	}
}
