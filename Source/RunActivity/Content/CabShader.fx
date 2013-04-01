// COPYRIGHT 2010, 2013 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                            C A B   S H A D E R                             //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float    NightColorModifier;  // Modifier for nighttime lighting.
bool     LightOn;       // Dashboard light is on
float4   Light1Pos;     // Dashboard Light 1 cone position
float4   Light2Pos;     // Dashboard Light 2 cone position
float3   Light1Col;     // Light 1 color
float3   Light2Col;     // Light 2 color
float2   TexPos;        // Texture bounding rectangle
float2   TexSize;       // Texture bounding rectangle
texture  ImageTexture;

sampler ImageSampler = sampler_state
{
	Texture = (ImageTexture);
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct PIXEL_INPUT
{
	//float2 Position  : VPOS;
	float2 TexCoords : TEXCOORD0;
	float4 Color     : COLOR0;
	float3 Normal    : NORMAL;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSCabShader(PIXEL_INPUT In) : COLOR0
{
	float4 origColor = tex2D(ImageSampler, In.TexCoords) * In.Color;
	float3 shadColor = origColor.rgb * NightColorModifier;

	if (LightOn)
	{
		float2 orig = In.TexCoords * TexSize + TexPos;

		float2 diffvect = Light1Pos.xy - orig;
		diffvect.x /= Light1Pos.w;
		float lum1 = saturate ((Light1Pos.z - length(diffvect)) / 200);

		diffvect = Light2Pos.xy - orig;
		diffvect.x /= Light2Pos.w;
		float lum2 = saturate ((Light2Pos.z - length(diffvect)) / 200);

		float3 lightEffect = saturate (lum1 * Light1Col + lum2 * Light2Col);
		shadColor += origColor.rgb * lightEffect;
	}

	return float4(min(shadColor, origColor.rgb * 1.2), origColor.a);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique CabShader {
	pass Pass_0 {
		PixelShader = compile ps_2_0 PSCabShader();
	}
}
