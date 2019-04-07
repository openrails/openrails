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
	float4 Position  : SV_POSITION;
	float4 Color     : COLOR0;
	float2 TexCoords : TEXCOORD0;
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
		PixelShader = compile ps_4_0_level_9_1 PSCabShader();
	}
}
