// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

// Values transferred from the game
float4x4 mModelToProjection; 
float4x4 mView;
float3 LightVector;                             // Direction vector to sun
float time;										// Used for moving textures across the sky
int random;										// Causes mapping to one of the moon phases
float4 sunpeakColor;
float4 sunriseColor;
float4 sunsetColor;
float overcast;
float windSpeed;
float windDirection;
float moonScale;

// "Up" vector for moon billboarding
#define worldUp cross(float3(-1,0,0),float3(0,0,-1))

// Textures
texture skyMap_Tex;
texture starMap_Tex;
texture moonMap_Tex;
texture moonMask_Tex;
texture cloudMap_Tex;

// Texture settings
sampler skyMap = sampler_state
{
   Texture = <skyMap_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = Wrap;
   AddressV = Wrap;
};

sampler starMap = sampler_state
{
   Texture = <starMap_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = wrap;
   AddressV = wrap;
};

sampler moonMap = sampler_state
{
   Texture = <moonMap_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = wrap;
   AddressV = wrap;
};

sampler moonMask = sampler_state
{
   Texture = <moonMask_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = wrap;
   AddressV = wrap;
};

sampler cloudMap = sampler_state
{
   Texture = <cloudMap_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = wrap;
   AddressV = wrap;
};

// Shader Input and Output Structures
//
// Vertex Shader Input
struct VS_IN
{
	float4 Pos			: POSITION;
	float3 Normal       : NORMAL;
	float2 vSky		    : TEXCOORD0;
};

// Vertex Shader Output
struct VS_OUT
{
	float4 Pos			: POSITION;
	float2 vSky		    : TEXCOORD0;
	float3 Normal       : TEXCOORD1;
};

// Pixel Shader Input
struct PS_IN
{
	float2 pSky		    : TEXCOORD0;
	float3 Normal       : TEXCOORD1;
};

// Pixel Shader Output
struct PS_OUT
{
	float4 Color		: COLOR;
};

/////////////////////    V E R T E X     S H A D E R S    /////////////////////////////

VS_OUT VSsky( VS_IN In )
{
	VS_OUT Out = ( VS_OUT ) 0;

	Out.Pos      = mul( mModelToProjection, In.Pos );
	Out.Normal   = In.Normal;
	Out.vSky     = In.vSky;	
		
	return Out;
}

VS_OUT VSmoon( VS_IN In )
{
	VS_OUT Out = ( VS_OUT ) 0;

    float3 position = In.Pos; 
    float3 viewDir = mView._m02_m12_m22;

    float3 rightVector = normalize(cross(viewDir, worldUp));    
    float3 upVector = normalize(cross(rightVector, viewDir));        
    
    int scale;
    if (random == 6) // moon dog
		moonScale *= 2;
    position += (In.vSky.x) * rightVector * moonScale;
    position += (In.vSky.y) * upVector * moonScale;
   
    Out.Pos = mul( mModelToProjection, float4(position, 1));
	Out.Normal   = In.Normal;
	Out.vSky     = In.vSky;	
		
	return Out;
}

/////////////////////    P I X E L     S H A D E R S    ///////////////////////////////

// This function dims the lighting at night, with a transition period as the sun rises or sets
float Day2Night(float startNightTrans, float finishNightTrans, float minDarknessCoeff)
{
	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	float slope = (1.0-minDarknessCoeff)/(startNightTrans-finishNightTrans); // "m"
	float incpt = 1.0 - slope*startNightTrans; // "b"
	// This is the return value used to darken scenery
	float adjustment;
	
    if (LightVector.y < finishNightTrans)
      adjustment = minDarknessCoeff;
    else if (LightVector.y > startNightTrans)
      adjustment = 1.0; // Scenery is fully lit during the day
    else
      adjustment = slope*LightVector.y + incpt;

	return adjustment;
}

// This function adjusts brightness, saturation and contrast
// By Romain Dura aka Romz
float3 ContrastSaturationBrightness(float3 color, float brt, float sat, float con)
{
	// Increase or decrease theese values to adjust r, g and b color channels separately
	const float AvgLumR = 0.5;
	const float AvgLumG = 0.5;
	const float AvgLumB = 0.5;
	
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float3 AvgLumin = float3(AvgLumR, AvgLumG, AvgLumB);
	float3 brtColor = color * brt;
	float intensityf = dot(brtColor, LumCoeff);
	float3 intensity = float3(intensityf, intensityf, intensityf);
	float3 satColor = lerp(intensity, brtColor, sat);
	float3 conColor = lerp(AvgLumin, satColor, con);
	return conColor;
}

PS_OUT PSsky( PS_IN In )
{
	PS_OUT Out = ( PS_OUT ) 0;

	// Get the color information for the current pixel
	float4 skyColor = tex2D( skyMap, In.pSky );
	float2 TexCoord = float2((1.0-In.pSky.x)+time, In.pSky.y );
	float4 starColor = tex2D( starMap, TexCoord );
	
    // Adjust sky color brightness for time of day
    float adjustLight = Day2Night(0.25, -0.25, -0.5);
    skyColor *= adjustLight;

	// Stars
	skyColor = lerp(starColor, skyColor, clamp(adjustLight+0.55, 0, 1));
    
    // Calculate angular difference between LightVector and vertex normal, radians
    float dotproduct = dot( LightVector, In.Normal);
    float angle = acos(dotproduct/(length(LightVector)*length(In.Normal)));
	        
    // Sun glow
	// Coefficients selected by the author to achieve the desired appearance
    skyColor += 0.015/angle;
    // increase orange at sunset
    if (LightVector.x < 0)
    {
		skyColor.r += 0.001/angle/(0.8*abs(LightVector.y-0.1));
		skyColor.g += 0.05*skyColor.r;
	}
    
    // Sun
	if (angle < 0.02)
	{
		skyColor = sunpeakColor;
        // Transition to orange at low angles
        if (LightVector.x < 0)
        {
			// Sunset
			skyColor = lerp(sunsetColor, skyColor, clamp(pow(adjustLight,10), 0, 1));
		}
		else
		{
			// Sunrise
			skyColor = lerp(sunriseColor, skyColor, clamp(pow(adjustLight,10), 0, 1));
		}
	}
	
    // Keep alpha opague
    skyColor.a = 1.0;
	Out.Color = skyColor;
	
	return Out;
}

PS_OUT PSmoon( PS_IN In )
{
	PS_OUT Out = ( PS_OUT ) 0;

	// Get the color information for the current pixel
	float rand = random;
	float2 TexCoord = float2(ceil(frac(rand/2))/2+In.pSky.x*0.5, floor(rand/2)/4+In.pSky.y*0.25 );
	float4 moonColor = tex2D( moonMap, TexCoord );
	float4 moonMask = tex2D( moonMask, In.pSky );
	
	// Fade moon during daylight
	if (LightVector.y > 0.1)
		moonColor.a *= (1-LightVector.y)/1.5;
	// Mask stars behind dark side (mask fades in)
	if (random != 6 && LightVector.y < 0.13)
		moonColor.a += moonMask.r*(-6.25*LightVector.y+0.8125);
		
	Out.Color = moonColor;
	
	return Out;
}

PS_OUT PSclouds( PS_IN In )
{
	PS_OUT Out = ( PS_OUT ) 0;

	// Get the color information for the current pixel
	// Cloud map is tiled. Tiling factor: 4
	// Move cloud map to suit wind conditions
	windSpeed *= 200; // This greatly exaggerates the wind speed, but it looks better!
	float2 TexCoord = float2(In.pSky.x*4-time*sin(windDirection)*windSpeed, In.pSky.y*4+time*cos(windDirection)*windSpeed );
	float4 cloudColor = tex2D( cloudMap, TexCoord );
    float alpha = cloudColor.a;
	
	// Adjust amount of overcast by adjusting alpha
	if (overcast < 0.2)
		cloudColor.a *= 4*overcast+0.2;
	else
	{
		// Adjust alpha
		alpha += saturate(2*overcast - 0.4);
		// Reduce contrast and brightness
		// Coefficients selected by author to achieve the desired appearance
		float contrast = 1.25-1.125*overcast;
		float brightness = 1.15-0.75*overcast;
		float3 color = ContrastSaturationBrightness(cloudColor.xyz, 1.0, brightness, contrast);
		cloudColor = float4(color,alpha);
	
	}

    // Adjust cloud color brightness for time of day
    alpha = cloudColor.a;
    cloudColor *= Day2Night(0.2, -0.2, 0.15);
    cloudColor.a = alpha;
	
	Out.Color = cloudColor;
	
	return Out;
}

///////////////////////////    T E C H N I Q U E S    ///////////////////////////////

// These techniques are all the same, but we'll keep them separate for now.

technique SkyTechnique
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSsky ( );
      PixelShader = compile ps_2_0 PSsky ( );
   }
}

technique MoonTechnique
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSmoon ( );
      PixelShader = compile ps_2_0 PSmoon ( );
   }
}

technique CloudTechnique
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSsky ( );
      PixelShader = compile ps_2_0 PSclouds ( );
   }
}
