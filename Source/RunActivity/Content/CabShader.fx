// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 LightVector;  // Direction vector to sun
bool   isNightTex;   // Using night texture
float4 Light1Pos;     // Dashboard Light 1 cone position
float4 Light2Pos;     // Dashboard Light 2 cone position
float3 Light1Col;     // Light 1 color
float3 Light2Col;     // Light 2 color
//float  LightRange;   // Dashboard light illumination range
bool   isLight;      // Dashboard light is on
float overcast;

float2 TexPos;         // Texture bounding rectangle
float2 TexSize;        // Texture bounding rectangle

uniform extern texture ImageTexture;

sampler ScreenS = sampler_state
{
    Texture = (ImageTexture);
};

struct PIXEL_INPUT
{
	//float2 Position  : VPOS;
	float2 TexCoords : TEXCOORD0;
	float4 Color     : COLOR0;
	float3 Normal    : NORMAL;
};


// Gets the night-time effect.
float _PSGetNightEffect()
{
	// The following constants define the beginning and the end conditions of
	// the day-night transition. Values refer to the Y postion of LightVector.
	const float startNightTrans = 0.1;
	const float finishNightTrans = -0.1;
	return saturate((LightVector.y - finishNightTrans) / (startNightTrans - finishNightTrans)) * saturate(1.5 - overcast);
}

float3 _LightEffect(float2 orig)
{
	const float lightStrength = 1.0;

	float light1Range = Light1Pos.z;
	float2 diffvect = Light1Pos.xy - orig;
	diffvect.x /= Light1Pos.w;
	float dist1 = length(diffvect);
	//float dist1 = distance(Light1Pos.xy, orig);
	float lum1 = saturate ((light1Range - dist1) / 200) * lightStrength;

	float light2Range = Light2Pos.z;
	diffvect = Light2Pos.xy - orig;
	diffvect.x /= Light2Pos.w;
	float dist2 = length(diffvect);
	//float dist2 = distance(Light2Pos.xy, orig);
	float lum2 = saturate ((light2Range - dist2) / 200) * lightStrength;

	return saturate (lum1 * Light1Col + lum2 * Light2Col);
	//return saturate (lum1 + lum2);
	//return lum1;
}

float4 PixelShaderFunction(PIXEL_INPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float NightBrightness = 0.2 + isLight * 0.15;
	const float3 litcolor = { 1, 0.85, 0.7 };

	float4 origColor = tex2D(ScreenS, In.TexCoords);
	float3 shadColor = origColor.rgb;
	shadColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNightTex));
	if (isLight)
	{
		float2 Pos = In.TexCoords * TexSize;
		Pos += TexPos;
		shadColor += origColor * _LightEffect(Pos);
	}
    return float4(min(shadColor, origColor * 1.2), origColor.a);
    //return float4(shadColor, origColor.a);
}

technique CabShading
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
