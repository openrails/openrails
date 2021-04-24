// COPYRIGHT 2014 by the Open Rails project.
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
//                   D I A L   G A U G E   S H A D E R                        //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4   LimitAngle;   // radian, x: target speed (colored start), y: permitted speed (pointer end), z: intervention speed, w: release speed

float4   LimitColor;   // dark grey, white or yellow
float4   PointerColor; // medium grey, white, yellow or red
float4	 InterventionColor; // yellow or red

texture  ImageTexture;

// Color RGB values are from ETCS specification
static const float4 DarkGreyColor = float4(0.333333, 0.333333, 0.333333, 1); // Dark grey gauge between -149 and -144 deg.
static const float4 ReleaseColor = float4(0.764706, 0.764706, 0.764706, 1);  // Light grey gauge-part for indicating release speed

// These constants are from ETCS specification
static const float limitPointerThk = 0.05128205; // ETCS: =atan(6/117) : thickness of 6 pixels at radius 137-20
static const float limitStartAngle = -2.6005406; // ETCS: -149 deg
static const float radiusOutside = 137 * 137;
static const float radiusRelease = 131 * 131;
static const float radiusRelease1 = 132 * 132;
static const float radiusInside = 128 * 128;
static const float radiusLimitPointer = 117 * 117;
static const float radiusNeedleCenter = 26 * 26; // Radius in ETCS is 25
static const float2 center = float2(140, 150); // ETCS display size is 280x300

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

////////////////////     P I X E L   S H A D E R S    //////////////////////////

float4 PSCircularSpeedGauge(PIXEL_INPUT In) : COLOR0
{
	float4 returnColor;
	float4 origColor = tex2D(ImageSampler, In.TexCoords) * In.Color;

	float2 dist = center - In.TexCoords;
	float radius = dist.x * dist.x + dist.y * dist.y;

	if (radius < radiusNeedleCenter)
		returnColor = PointerColor;
	else if (radius > radiusOutside)
		returnColor = origColor;
	else if (radius < radiusLimitPointer)
		returnColor = origColor;
	else
	{
		float angle = atan(-dist.x / dist.y);
		if (dist.y < 0)
		{
			if (dist.x > 0)
				angle -= 3.141592654;
			else
				angle += 3.141592654;
		}

		if (angle < limitStartAngle
			|| angle > LimitAngle.z
			|| angle < LimitAngle.y - limitPointerThk && radius < radiusInside)
		{
			returnColor = origColor;
		}
        else if (LimitAngle.x < limitStartAngle) // Do not display gauge
        {
            returnColor = origColor;
        }
		else if (angle > LimitAngle.y)
        {
            if (angle > LimitAngle.w) // Exceeded limit pointer at overspeed
                returnColor = InterventionColor;
            else if (radius > radiusInside)
                returnColor = ReleaseColor;
			else
				returnColor = origColor;
        }
		else if (angle > LimitAngle.x)
		{
			if (angle > LimitAngle.w)
				returnColor = LimitColor;
			else if (radius < radiusRelease)
				returnColor = LimitColor;
			else if (radius > radiusRelease1)
				returnColor = ReleaseColor;
			else
				returnColor = origColor;
		}
        else
            returnColor = DarkGreyColor;
	}

	return returnColor;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique CircularSpeedGauge {
	pass Pass_0 {
		PixelShader = compile ps_4_0_level_9_3 PSCircularSpeedGauge();
	}
}
