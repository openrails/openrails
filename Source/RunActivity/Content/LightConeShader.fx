// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                     L I G H T   C O N E   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 WorldViewProjection;  // model -> world -> view -> projection

float2 Fade; // overall fade (0 = off, 1 = on); transition fade (0 = original, 1 = transition)

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float3 PositionO : POSITION0; // original position x, y, z
	float3 PositionT : POSITION1; // transition position x, y, z
	float4 ColorO    : COLOR0;    // original color r, g, b, a
	float4 ColorT    : COLOR1;    // transition color r, g, b, a
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
    Out.Position = mul(float4(lerp(In.PositionO, In.PositionT, Fade.y), 1), WorldViewProjection);
    Out.Color = lerp(In.ColorO, In.ColorT, Fade.y);
    Out.Color.a *= Fade.x * 0.1;
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
