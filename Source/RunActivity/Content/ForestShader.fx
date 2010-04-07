//--------------------------------------------------------------//
// FOREST SHADER
//
// Principal Author: Rick Grout
//--------------------------------------------------------------//

// Values transferred from the game
float4x4 mView;
float4x4 mWorld;
float4x4 mWorldViewProj;
float3 LightVector;
float overcast;

float3 headlightPosition;
float3 headlightDirection;
float lightStrength = 1.5;
float coneAngle = 0.4;
float coneDecay = 8.0;

// Textures
texture forest_Tex;

// Texture settings
sampler forestMap = sampler_state
{
   Texture = <forest_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = Clamp;
   AddressV = Clamp;
};

// Shader Input and Output Structures
//
// Vertex Shader Input
struct VS_IN
{
	float3 Position		: POSITION;
	float3 Size			: NORMAL;
	float2 TexCoords	: TEXCOORD0;
};

// Vertex Shader Output
struct VS_OUT
{
	float4 Position		: POSITION;
	float2 TexCoords	: TEXCOORD0;
	float3 Normal       : TEXCOORD1;
	// Headlight
	float3 LightDir		: TEXCOORD2;
};

/////////////////////    V E R T E X     S H A D E R S    /////////////////////////////

VS_OUT VSforest( VS_IN In )
{
	VS_OUT Out = ( VS_OUT ) 0;
	
    float3 position = In.Position; 
    float3 eyeVector = normalize(mView._m02_m12_m22);

 	float3 upVector = float3(0, -1, 0);
    float3 sideVector = normalize(cross(eyeVector, upVector));    
    
    position += (In.TexCoords.x-0.5f) * sideVector * In.Size.x;
    position += (In.TexCoords.y-1.0f) * upVector * In.Size.y;
   
    Out.Position = mul( mWorldViewProj, float4(position, 1));
	Out.TexCoords = In.TexCoords;
	Out.Normal = eyeVector;
   
	// Headlight
	float3 final3DPos = mul(position, mWorld);
	Out.LightDir = final3DPos - headlightPosition;

	return Out;
}

/////////////////////    P I X E L     S H A D E R S    /////////////////////////////

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

float4 PSforest( VS_OUT In ) : COLOR
{
	// Get the color information for the current pixel
	float4 surfColor = tex2D(forestMap, In.TexCoords);
	float alpha = surfColor.a;
	
	// Darken at night
	surfColor *= Day2Night();
	// Reduce saturaton when overcast
	float3 color = Overcast(surfColor.xyz, 1-overcast);
	surfColor = float4(color, 1);
/*
	float4 litColor = surfColor;
	
    // Headlight effect
    float3 normal = normalize(In.Normal);
    float3 lightDir = normalize(In.LightDir);
    float coneDot = dot(lightDir, normalize(headlightDirection));
    float shading = 0;
    if (coneDot > coneAngle)
    {
		float coneAtten = pow(coneDot, coneDecay);
		shading = dot(normal, -lightDir);
		shading *= lightStrength;
		shading *= coneAtten;
    }
    surfColor += (litColor + shading) * 0.05;
*/	
	surfColor.a = alpha;
	return surfColor;
}

///////////////////////////    T E C H N I Q U E S    ///////////////////////////////

technique Forest
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSforest ( );
      PixelShader = compile ps_2_0 PSforest ( );
   }
}
