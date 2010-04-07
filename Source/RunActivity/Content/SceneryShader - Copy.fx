//--------------------------------------------------------------//
// SCENERY OBJECT SHADER 
//--------------------------------------------------------------//
//--------------------------------------------------------------//
// Pass 0
//--------------------------------------------------------------//


float4x4 mModelToProjection : ViewProjection;	// SetValueTranspose((world * view) * projection);  
float4x4 mWorldToView  : ViewInverse;			// SetValue(Matrix.Invert(view));
float4x4 mModelToWorld : WorldMatrix;			// SetValue(world);

float3 LightVector;								// Direction vector to sun
float3 BumpScale = float3( 1.0, -1.0, 1.0 );	// multiply bump map by this  -1 seems to work with Ultimapper sometimes

float Saturation = 0.9;
float Ambient = 0.5;
float Brightness = 0.7;
float ZBias = 0.0;  // TODO TESTING
float overcast;									// Lower saturation & brightness when overcast
float3 viewerPos;								// Viewer's world coordinates.
float specularPower;							// Exponent -- higher number yields greater specularity
bool isNight_Tex;								// Using night texture

float3 headlightPosition;
float3 coneDirection = float3(0, -0.7, 0);
float lightStrength = 1.0;
float coneAngle = 0.5;
float coneDecay = 2.0;

texture imageMap_Tex;
sampler imageMap = sampler_state
{
   Texture = (imageMap_Tex);
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = Linear;
   //AddressU = Wrap;  set in the Materials class
   //AddressV = Wrap;
};

texture normalMap_Tex;
sampler normalMap = sampler_state
{
   Texture = (normalMap_Tex);
   MAGFILTER =  Linear;
   MINFILTER =  Linear;
   MIPFILTER =  Linear;
   MipMapLodBias = 0;
   AddressU = Wrap;
   AddressV = Wrap;
};


/////////////////////    V E R T E X     S H A D E R    /////////////////////////////////

struct VS_OUTPUT
{
   float  light      : TEXCOORD1;
   float  distance   : TEXCOORD2;
   float4 pPositionP : POSITION;    // in projection space
   float2 uvImageT	 : TEXCOORD0;   // in texture space
   float3 vNormalW   : TEXCOORD3;	// in world space
};

VS_OUTPUT VS(   float4 pPositionM : POSITION,	// in model space
				float3 vNormalM   : NORMAL,		// in model space
				float2 uvImageT   : TEXCOORD0	// in texture space
			)	
{
   VS_OUTPUT Out = (VS_OUTPUT) 0; 

   Out.pPositionP = mul( mModelToProjection, pPositionM );		// shift point position from model space to projection space
   // Out.pPositionP.z and .w = 0 - far clip plane , ie 0 - 1000
   Out.pPositionP.z += ZBias; 
   Out.pPositionP.w += ZBias;
   
   Out.vNormalW   = normalize(mul(vNormalM,mModelToWorld).xyz);	   // from model space to world space
   Out.uvImageT = uvImageT;	

   Out.distance = length( Out.pPositionP );

   Out.light = dot( Out.vNormalW, LightVector ) *0.5 + 0.5;									

   return Out;
}


/////////////////////    P I X E L     S H A D E R    /////////////////////////////////

// This function dims the lighting at night, with a transition period as the sun rises/sets
float Day2Night( )
{
	// The following constants define the beginning and the end conditions of the day-night transition
	const float startNightTrans = 0.1; // The "NightTrans" values refer to the Y postion of LightVector
	const float finishNightTrans = -0.1;
	const float minDarknessCoeff = 0.2;
	
	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	// Can't use lerp() here, as overall dimming action is too complex
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

// This function reduces color saturation and brightness as overcast increases
// Adapted from an algorithm by Romain Dura aka Romz
float3 Overcast(float3 color, float sat)
{
	// This value limits desaturation amount:
	const float satLower = 0.8; 
	// Values used to determine equivalent grayscale color:
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float intensityf = dot(color, LumCoeff);
	float3 intensity = float3(intensityf, intensityf, intensityf);
	float3 satColor = lerp(intensity, color, clamp(sat, satLower, 1.0));
	
	// Reduce brightness slightly
	// Default overcast=0.2 and sat=1-0.2, so this equation yields a default brightness of 1.0 
	satColor *= 0.6*(0.867+sat); 
	
	return satColor;
}

float4 PSImage( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW      : TEXCOORD3,
           float4 pPositionP    : TEXCOORD4 )
           : COLOR
{ 

    float4 surfColor = tex2D( imageMap, uvImageT );
    float alpha = surfColor.a;
    surfColor *= light * 0.65 + 0.4; //Brightness + Ambient;
    
	if (specularPower > 0)
	{
		float3 eyeVector = normalize(viewerPos - pPositionP);
		float3 reflectionVector = -reflect(normalize(LightVector), normalize(vNormalW));
		float specularity = dot(normalize(reflectionVector), normalize(eyeVector));
		specularity = saturate(pow(specularity, specularPower)) * length(surfColor) * 0.5;        
		surfColor.rgb += specularity;
	}
	
    if (!isNight_Tex) // Darken at night unless using a night texture
    {
		surfColor *= Day2Night(); 
		// Reduce saturaton when overcast
		float3 color = Overcast(surfColor.xyz, 1-overcast);
		surfColor = float4(color, 1);
    }
    
    surfColor.a = alpha;
    return surfColor;
}

float4 PSVegetation( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	float alpha = surfColor.a;
	surfColor *= 0.8;  
	surfColor += 0.03;
	
	// Darken at night
	surfColor *= Day2Night();
	// Reduce saturaton when overcast
	float3 color = Overcast(surfColor.xyz, 1-overcast);
	surfColor = float4(color, 1);

	surfColor.a = alpha;
	return surfColor;
}

float4 PSDark( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	float alpha = surfColor.a;
	surfColor *= 0.4; 
	surfColor.a = alpha;
	return surfColor;
}

float4 PSTerrain( 
		   float light          : TEXCOORD1,
		   float distance		: TEXCOORD2,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 

    float3 surfColor = tex2D( imageMap, uvImageT );
    
    distance = clamp(distance,100,500);
    float effect = 100/distance;
    float3 bump = tex2D( normalMap, uvImageT * 50 );
    bump -= 0.5;
	surfColor +=  0.5 * bump;
    surfColor *= light * 0.65 + 0.4; //Brightness + Ambient;
    
    // Darken at night
    surfColor *= Day2Night();
    // Reduce saturaton when overcast
    surfColor = Overcast(surfColor, 1-overcast);

    return float4( surfColor,1);
}

technique Image   //0
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSImage ( );
   }

}

technique Vegetation  // 1
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSVegetation ( );
   }

}

technique Terrain   // 2
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSTerrain ( );
   }

}

technique Dark  // 3
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSDark ( );
   }

}
