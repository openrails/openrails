// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                     L I G H T   G L O W   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 WorldViewProjection;  // model -> world -> view -> projection

float2 Fade; // overall fade (0 = off, 1 = on); transition fade (0 = original, 1 = transition)

texture LightGlowTexture;
sampler LightGlow = sampler_state
{
	Texture = (LightGlowTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Wrap;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float3 PositionO        : POSITION0; // original position x, y, z
	float3 PositionT        : POSITION1; // transition position x, y, z
	float3 NormalO          : NORMAL0;   // original normal x, y, z
	float3 NormalT          : NORMAL1;   // transition normal x, y, z
	float4 ColorO           : COLOR0;    // original color r, g, b, a
	float4 ColorT           : COLOR1;    // transition color r, g, b, a
	float4 TexCoords_Radius : TEXCOORD0; // tex coords u, v; original radius; transition radius
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position  : POSITION0;
	float4 Color     : COLOR0;
	float2 TexCoords : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSLightGlow(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
    
    float radius = lerp(In.TexCoords_Radius.z, In.TexCoords_Radius.w, Fade.y);
    float3 position = lerp(In.PositionO, In.PositionT, Fade.y);
    float3 normal = lerp(In.NormalO, In.NormalT, Fade.y);
 	float3 upVector = float3(0, 1, 0);
    float3 sideVector = normalize(cross(upVector, normal));
    upVector = normalize(cross(sideVector, normal));
    position += (In.TexCoords_Radius.x - 0.5f) * sideVector * radius;
    position += (In.TexCoords_Radius.y - 0.5f) * upVector * radius;
    Out.Position = mul(WorldViewProjection, float4(position, 1));
	
    Out.Color = lerp(In.ColorO, In.ColorT, Fade.y);
    Out.Color.a *= Fade.x;
    
    Out.TexCoords = In.TexCoords_Radius.xy;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSLightGlow(in VERTEX_OUTPUT In) : COLOR0
{
	return In.Color * tex2D(LightGlow, In.TexCoords.xy);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique LightGlow {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSLightGlow();
		PixelShader = compile ps_2_0 PSLightGlow();
	}
}
