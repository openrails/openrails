//--------------------------------------------------------------//
// PRECIPITATION SHADER 
//--------------------------------------------------------------//

// Values transferred from the game
float4x4 mProjection;
float4x4 mWorld;
float4x4 mView;
float3 LightVector; // Direction vector to sun, used for day-night darkening
int viewportHeight;
double currentTime;
int weatherType;

// Textures
texture precip_Tex;

// Texture settings
sampler PrecipSamp = sampler_state
{
	texture = <precip_Tex>;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
	MIPFILTER = LINEAR;
	MIPMAPLODBIAS = 0.000000;
	AddressU = Wrap;
	AddressV = Wrap;

};

// Shader Input and Output Structures
//
// Vertex Shader Input
struct VS_IN
{
	float4 Pos          : POSITION;
	float1 Size         : PSIZE;
	float1 Time         : TEXCOORD0;
	float2 Wind         : TEXCOORD1;
};

// Vertex Shader Output
struct VS_OUT
{
	float4 Pos			: POSITION;
	float1 Size			: PSIZE;
};

// Pixel Shader Input
struct PS_IN
{
	float2 pPrecip		: TEXCOORD0;
};

/////////////////////    V E R T E X     S H A D E R S    /////////////////////////////

// Precipitation vertex shader - calculates raindrop/snowflake positions.
VS_OUT VSprecip( VS_IN In )
{
	VS_OUT Out = ( VS_OUT ) 0;
    float4 position = mul(In.Pos, mWorld);
     
    // Particle age determines Y position
    float age = currentTime - In.Time;
    // Wind effect
    position.xy += age * In.Wind;
    // Vertical velocity
    float velocity;
    if (weatherType == 1) // snow
		velocity = 8.0f;
    if (weatherType == 2) // rain
    {
		velocity = 20.0f;
		In.Size *= 1.4;
	}
    position.y = position.y - age * velocity;
    
    float4 ProjectedPosition = mul(mul(position, mView), mProjection);
    Out.Pos = ProjectedPosition;
    
    // Calculate size based on distance from viewer
    Out.Size = In.Size * mProjection._m11 / ProjectedPosition.w * viewportHeight / 4;
	
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

float4 PSprecip(PS_IN In) : COLOR0
{
    float4 color = tex2D(PrecipSamp, In.pPrecip);
    
    // Adjust raindrop/snowflake brightness for time of day
    color *= Day2Night(0.2, -0.2, 0.6);
    
	return color;
}

///////////////////////////    T E C H N I Q U E S    ///////////////////////////////

technique RainTechnique
{
    pass Pass_0
    {
        VertexShader = compile vs_2_0 VSprecip();
        PixelShader = compile ps_2_0 PSprecip();
    }
}
